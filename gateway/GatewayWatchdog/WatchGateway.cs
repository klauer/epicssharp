using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using PSI.EpicsClient2;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace GatewayWatchdog
{
    enum GWStatus
    {
        STARTING,
        ALL_OK,
        HIGH_CPU,
        NOT_ANSWERING,
        RESTARTING
    }

    public partial class WatchGateway : ServiceBase
    {
        Thread checkGateway;
        bool shouldStop = false;
        const int nbCPUAvg = 120;
        TcpListener tcpListener = null;
        GWStatus status = GWStatus.STARTING;
        double currentAVG = 0;
        List<double> lastCPUVals = new List<double>();

        public WatchGateway()
        {
            InitializeComponent();

        }

        public void Start()
        {
            checkGateway = new Thread(CheckGateway);
            checkGateway.Start();

            if (ConfigurationManager.AppSettings["WebInterface"] != null)
            {
                string s = ConfigurationManager.AppSettings["WebInterface"];
                IPEndPoint ipSource = new IPEndPoint(IPAddress.Parse(s.Split(':')[0]), int.Parse(s.Split(':')[1]));
                if (Environment.UserInteractive)
                    Console.WriteLine("Start receiving HTTP on " + ipSource);
                tcpListener = new TcpListener(ipSource);
                tcpListener.Start(10);
                tcpListener.BeginAcceptSocket(ReceiveConn, tcpListener);
            }
        }

        void ReceiveConn(IAsyncResult result)
        {
            TcpListener listener = null;
            Socket client = null;

            try
            {
                listener = (TcpListener)result.AsyncState;
                client = listener.EndAcceptSocket(result);

                client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                //Thread.Sleep(100);
                using (var reader = new StreamReader(new NetworkStream(client)))
                {
                    var cmd = reader.ReadLine();
                    using (var writer = new StreamWriter(new NetworkStream(client)))
                    {
                        Console.WriteLine(cmd);

                        if(cmd.StartsWith("POST /restart"))
                        {
                            RestartGW();
                        }

                        writer.WriteLine("HTTP/1.0 OK");
                        writer.WriteLine("Content-Type: text/html");
                        writer.WriteLine("Expires: now");
                        writer.WriteLine();
                        writer.WriteLine("<html>");
                        writer.WriteLine("<head><title>Gateway Watchdog - " + ConfigurationManager.AppSettings["GatewayName"] + "</title></head>");
                        writer.WriteLine("<body>");
                        writer.WriteLine("Status: " + status + "<br>");
                        writer.WriteLine("CPU Average: " + currentAVG.ToString("0.00") + "%<br>");
                        writer.WriteLine("<form method='post' action='/restart'>");
                        writer.WriteLine("<input type='submit' value='Restart Gateway' onclick='return confirm(\"Are you sure you want to restart the gateway?\");'>");
                        writer.WriteLine("</form>");
                        writer.WriteLine("Updated on " + DateTime.Now + "<br>");
                        writer.WriteLine("<script>setTimeout(\"document.location='/';\",1000);</script>");
                        writer.WriteLine("</body>");
                        writer.WriteLine("</html>");
                        writer.Close();
                    }

                    reader.Close();
                }
                client.Close();
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                    Console.WriteLine(ex);
            }


            try
            {
                listener.BeginAcceptSocket(new AsyncCallback(ReceiveConn), listener);
            }
            catch
            {
            }
        }

        protected override void OnStart(string[] args)
        {
            this.Start();
            /*checkGateway = new Thread(CheckGateway);
            checkGateway.Start();

            if (ConfigurationManager.AppSettings["WebInterface"] != null)
            {
            }*/
        }

        protected override void OnStop()
        {
            shouldStop = true;

            if (ConfigurationManager.AppSettings["WebInterface"] != null)
            {
            }
        }

        void CheckGateway()
        {
            if (!Environment.UserInteractive)
                Thread.Sleep(40000);

            NameValueCollection additionalChannels = (NameValueCollection)ConfigurationManager.GetSection("AdditionalChannels");

            while (!shouldStop)
            {
                if (Environment.UserInteractive)
                    Console.WriteLine("Checking...");
                bool isOk = false;
                for (int i = 0; i < 5; i++)
                {
                    if (Environment.UserInteractive)
                        Console.WriteLine("Trial " + i);
                    isOk = false;
                    using (EpicsClient client = new EpicsClient())
                    {
                        client.Configuration.WaitTimeout = 15000;
                        EpicsChannel<double> cpuInfo = client.CreateChannel<double>(ConfigurationManager.AppSettings["GatewayName"] + ":CPU");
                        try
                        {
                            double v = cpuInfo.Get();
                            lastCPUVals.Add(v);
                            while (lastCPUVals.Count > nbCPUAvg)
                                lastCPUVals.RemoveAt(0);

                            currentAVG = lastCPUVals.Average();
                            if (lastCPUVals.Count < nbCPUAvg * 0.8 || currentAVG < 90.0)
                            {
                                isOk = true;
                                status = GWStatus.ALL_OK;
                            }
                            else
                                status = GWStatus.HIGH_CPU;
                        }
                        catch
                        {
                        }
                    }

                    if (isOk && additionalChannels != null)
                    {
                        foreach (string gw in additionalChannels.AllKeys)
                        {
                            using (EpicsClient client = new EpicsClient())
                            {
                                client.Configuration.SearchAddress = gw;
                                client.Configuration.WaitTimeout = 2000;
                                EpicsChannel<string> channel = client.CreateChannel<string>(additionalChannels[gw]);
                                try
                                {
                                    string s = channel.Get();
                                    if (Environment.UserInteractive)
                                        Console.WriteLine("Read " + s);
                                    isOk = true;
                                    status = GWStatus.ALL_OK;
                                }
                                catch
                                {
                                    isOk = false;
                                    status = GWStatus.NOT_ANSWERING;
                                }
                            }
                        }
                    }

                    if (isOk == true)
                        break;

                    Thread.Sleep(1000);
                }

                if (!isOk)
                {
                    if (Environment.UserInteractive)
                        Console.WriteLine("Not ok!!!");
                    RestartGW();
                    Thread.Sleep(40000);
                }
                else
                {
                    if (Environment.UserInteractive)
                        Console.WriteLine("All ok");
                }
                Thread.Sleep(10000);
            }
        }

        void RestartGW()
        {
            status = GWStatus.RESTARTING;
            StopGateway();
            lastCPUVals.Clear();
            StartGateway();
        }

        void StopGateway()
        {
            try
            {
                ServiceController service = new ServiceController(ConfigurationManager.AppSettings["ServiceName"]);
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(5000));
            }
            catch
            {
            }

            Thread.Sleep(2000);

            // Kill the remaining processes
            try
            {
                var processes = Process.GetProcesses()
                    .Where(row => row.ProcessName.ToLower() == "gwservice" || row.ProcessName.ToLower() == "epics gateway");
                foreach (var i in processes)
                    i.Kill();
            }
            catch
            {
            }
        }

        void StartGateway()
        {
            try
            {
                if (Environment.UserInteractive)
                    Console.WriteLine("Starting gw");
                ServiceController service = new ServiceController(ConfigurationManager.AppSettings["ServiceName"]);
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(5000));
            }
            catch
            {
            }
        }
    }
}
