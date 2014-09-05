using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

namespace GatewayWatchdog
{
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                AllocConsole();
                WatchGateway w = new WatchGateway();
                w.Start();
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { new WatchGateway() });
            }
        }
    }
}
