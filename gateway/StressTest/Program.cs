using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace StressTest
{
    class Program
    {
        const int NbThreadsToRun = 10;
        //const int NbThreadsToRun = 2;
        static CaSharpServer.CAIntRecord[] intRecs = new CaSharpServer.CAIntRecord[300];
        static bool shouldRun = true;

        static void Main(string[] args)
        {
            var ps = Process.GetProcesses().Where(row => row.ProcessName.Contains("StressClient"));
            foreach (var i in ps)
                i.Kill();
            Thread.Sleep(100);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // Setup the console look
            Console.WindowWidth = 120;
            Console.BufferWidth = 120;
            Console.WindowHeight = 60;
            Console.BufferHeight = 3000;

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


            CaSharpServer.CAServer server = new CaSharpServer.CAServer(IPAddress.Parse("127.0.0.1"), 9876, 9876);

            CaSharpServer.CAIntRecord singleInt = server.CreateRecord<CaSharpServer.CAIntRecord>("STRESS:INT");
            singleInt.Scan = CaSharpServer.Constants.ScanAlgorithm.ON_CHANGE;
            singleInt.Value = 1234;

            for (int i = 0; i < intRecs.Length; i++)
            {
                intRecs[i] = server.CreateRecord<CaSharpServer.CAIntRecord>("STRESS:INT:" + i);
                intRecs[i].Scan = CaSharpServer.Constants.ScanAlgorithm.ON_CHANGE;
                intRecs[i].Value = 1234 - i;
            }
            //intRec.PrepareRecord += new EventHandler(intRec_PrepareRecord);

            PBCaGw.Gateway gateway = new PBCaGw.Gateway();
            gateway.Configuration.LocalAddressSideA = "129.129.130.87:6789";
            gateway.Configuration.RemoteAddressSideA = "129.129.130.87:6789";
            gateway.Configuration.LocalAddressSideB = "127.0.0.1:9877";
            gateway.Configuration.RemoteAddressSideB = "127.0.0.1:9876";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gateway.Start();

            Thread[] threads = new Thread[NbThreadsToRun];
            for (int i = 0; i < threads.Length; i++)
            {

                threads[i] = new Thread(new ThreadStart(ClientStress));
                threads[i].IsBackground = true;
                threads[i].Start();
            }

            Thread produceThread = new Thread(new ThreadStart(ProduceData));
            produceThread.IsBackground = true;
            produceThread.Start();

            if (JetBrains.Profiler.Core.Api.PerformanceProfiler.IsActive)
            {
                Console.WriteLine("Running profiler test...");
                Thread.Sleep(3000);
                JetBrains.Profiler.Core.Api.PerformanceProfiler.Begin();
                JetBrains.Profiler.Core.Api.PerformanceProfiler.Start();
                Thread.Sleep(10000);
                JetBrains.Profiler.Core.Api.PerformanceProfiler.Stop();
                JetBrains.Profiler.Core.Api.PerformanceProfiler.EndSave();

                shouldRun = false;
                ps = Process.GetProcesses().Where(row => row.ProcessName.Contains("StressClient"));
                foreach (var i in ps)
                    i.Kill();
            }
            else if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.ReadLine();

                shouldRun = false;
                ps = Process.GetProcesses().Where(row => row.ProcessName.Contains("StressClient"));
                foreach (var i in ps)
                    i.Kill();
            }
            else
            {
                /*while (true)
                    Console.ReadKey();*/

                Thread.Sleep(33000);
                Console.WriteLine("CPU Time: " + Process.GetCurrentProcess().TotalProcessorTime.ToString());

                shouldRun = false;
                ps = Process.GetProcesses().Where(row => row.ProcessName.Contains("StressClient"));
                foreach (var i in ps)
                    i.Kill();

                //Console.ReadKey();

            }
        }

        static void ProduceData()
        {
            int loop = 0;
            while (true)
            {
                for (int i = 0; i < intRecs.Length; i++)
                    intRecs[i].Value = loop % intRecs.Length;
                loop++;
                Thread.Sleep(10);
            }
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            PBCaGw.Services.Log.TraceEvent(System.Diagnostics.TraceEventType.Stop, -1, "Ctrl+C received, stopping the gateway");
            e.Cancel = true;

            shouldRun = false;

            var ps = Process.GetProcesses().Where(row => row.ProcessName.Contains("StressClient"));
            foreach (var i in ps)
                i.Kill();

            Console.ReadKey();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            PBCaGw.Services.Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, -1, e.ToString());
        }

        static void ClientStress()
        {
            Thread.Sleep(3000);
            Console.WriteLine("Start consuming");
            while (shouldRun)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = "StressClient.exe";
                startInfo.RedirectStandardOutput = true;
                startInfo.Arguments = "-m";

                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using statement will close.
                    using (Process exeProcess = new Process())
                    {
                        exeProcess.StartInfo = startInfo;
                        exeProcess.OutputDataReceived += new DataReceivedEventHandler(exeProcess_OutputDataReceived);
                        exeProcess.Start();
                        exeProcess.BeginOutputReadLine();
                        exeProcess.WaitForExit();
                    }
                }
                catch
                {
                    // Log error.
                }
            }
        }

        static void exeProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e == null || e.Data == null)
                return;
            if (e.Data.Contains("Didn't got back!"))
            {
            }
            Console.WriteLine(e.Data);
        }
    }
}
