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

namespace GatewayWatchdog
{
    public partial class WatchGateway : ServiceBase
    {
        Thread checkGateway;
        bool shouldStop = false;
        const int nbCPUAvg = 120;

        public WatchGateway()
        {
            InitializeComponent();

        }

        public void Start()
        {
            checkGateway = new Thread(CheckGateway);
            checkGateway.Start();
        }

        protected override void OnStart(string[] args)
        {
            checkGateway = new Thread(CheckGateway);
            checkGateway.Start();
        }

        protected override void OnStop()
        {
            shouldStop = true;
        }

        void CheckGateway()
        {
            if (!Environment.UserInteractive)
                Thread.Sleep(40000);
            List<double> lastCPUVals = new List<double>();

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


                            if (lastCPUVals.Count < nbCPUAvg * 0.8 || lastCPUVals.Average() < 80.0)
                            {
                                isOk = true;
                            }
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
                                }
                                catch
                                {
                                    isOk = false;
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
                    StopGateway();
                    StartGateway();
                }
                else
                {
                    if (Environment.UserInteractive)
                        Console.WriteLine("All ok");
                }
                Thread.Sleep(10000);
            }
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
            Thread.Sleep(20000);
        }
    }
}
