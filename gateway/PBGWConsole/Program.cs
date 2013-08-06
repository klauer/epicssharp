using System;
using System.Globalization;
using PBCaGw;
using PBCaGw.Services;
using System.Threading;

namespace PBGWConsole
{
    class Program
    {
        static Gateway gateway;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Console.WriteLine("PBCaGW " + Gateway.Version);
            Console.WriteLine("Build on " + DiagnosticServer.BuildTime.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("(c) Paul Scherrer Institute - GFA IT - 2012");

            // Setup the console look
            try
            {
                Console.Title = "PBCaGW - " + System.Configuration.ConfigurationManager.AppSettings["gatewayName"];
                Console.WindowWidth = 120;
                Console.BufferWidth = 120;
                Console.WindowHeight = 60;
                Console.BufferHeight = 3000;
            }
            catch
            {
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Press return to stop the gateway...");
            }
            else
            {
                Console.WriteLine("Press Ctrl+C to stop the gateway...");
                Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            }
            Console.WriteLine("");

            /*Gateway.AutoCreateChannel = false;
            Gateway.RestoreCache = false;*/

            /*Gateway.AutoCreateChannel = false;
            Gateway.RestoreCache = false;*/
            //Gateway.AutoCreateChannel = false;
            //Gateway.BufferedSockets = false;

            gateway = new Gateway();
            gateway.LoadConfig();
            gateway.Start();

            while (true)
            {
                ConsoleKeyInfo k = Console.ReadKey();
                switch (k.Key)
                {
                    case ConsoleKey.Spacebar:
                        DataPacket.ShowPools();
                        break;
                    case ConsoleKey.L:
                        Log.ShowAll = !Log.ShowAll;
                        break;
                    default:
                        if (System.Diagnostics.Debugger.IsAttached)
                        {
                            gateway.Dispose();
                            return;
                        }
                        break;
                }
            }

            /*if (System.Diagnostics.Debugger.IsAttached)
            {
                while (true)
                {
                    ConsoleKeyInfo k = Console.ReadKey();
                    switch (k.Key)
                    {
                        case ConsoleKey.Spacebar:
                            DataPacket.ShowPools();
                            break;
                        default:
                            gateway.Dispose();
                            return;
                    }
                }
            }
            else
            {
                while (true)
                    Console.ReadKey();
            }*/
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ToString() + ((Exception)e.ExceptionObject).StackTrace.ToString());
            Console.WriteLine("Major error. Press a key to terminate");
            Console.ReadKey();
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Log.TraceEvent(System.Diagnostics.TraceEventType.Stop, -1, "Ctrl+C received, stopping the gateway");
            e.Cancel = true;
            gateway.Dispose();
            Environment.Exit(0);
        }
    }
}
