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

namespace GatewayWatchdog
{
    public partial class WatchGateway : ServiceBase
    {
        Thread checkGateway;
        bool shouldStop = false;

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
            Thread.Sleep(40000);

            while (!shouldStop)
            {
                bool isOk=false;
                for (int i = 0; i < 5; i++)
                {
                    using (EpicsClient client = new EpicsClient())
                    {
                        //Console.WriteLine("Checking...");
                        client.Configuration.WaitTimeout = 15000;
                        EpicsChannel<double> cpuInfo = client.CreateChannel<double>(ConfigurationManager.AppSettings["GatewayName"] + ":CPU");
                        try
                        {
                            double v = cpuInfo.Get();
                            if (v < 80.0)
                            {
                                isOk = true;
                                break;
                            }
                            /*else
                                Console.WriteLine("All ok");*/
                        }
                        catch
                        {
                        }
                    }
                    Thread.Sleep(1000);
                }

                if (!isOk)
                {
                    StopGateway();
                    StartGateway();
                }
                else
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
                foreach(var i in processes)
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
                //Console.WriteLine("Starting gw");
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
