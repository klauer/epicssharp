using CaSharpServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NameServer
{
    class DebugServer : IDisposable
    {
        CAServer server;
        readonly CADoubleRecord channelCpu;
        readonly PerformanceCounter cpuCounter;
        readonly CADoubleRecord channelMem;
        readonly PerformanceCounter ramCounter;
        readonly CAIntRecord channelNbSearchPerSec;
        readonly CAStringRecord runningTime;
        readonly CAStringRecord channelVersion;
        readonly CAStringRecord channelBuild;
        readonly CAIntRecord channelHeartBeat;

        DateTime startTime = DateTime.Now;

        static public int NbSearches = 0;

        bool disposed = false;

        public DebugServer(IPAddress address)
        {
            server = new CAServer(address, 7654, 7654, 7655);
            // CPU usage
            channelCpu = server.CreateRecord<CADoubleRecord>(System.Environment.MachineName + ":CPU");
            channelCpu.EngineeringUnits = "%";
            channelCpu.CanBeRemotlySet = false;
            channelCpu.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelCpu.PrepareRecord += channelCPU_PrepareRecord;
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            // Mem free
            channelMem = server.CreateRecord<CADoubleRecord>(System.Environment.MachineName + ":MEM-FREE");
            channelMem.CanBeRemotlySet = false;
            channelMem.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelMem.EngineeringUnits = "Mb";
            channelMem.PrepareRecord += channelMEM_PrepareRecord;
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // Searches per sec
            channelNbSearchPerSec = server.CreateRecord<CAIntRecord>(System.Environment.MachineName + ":SEARCH-SEC");
            channelNbSearchPerSec.CanBeRemotlySet = false;
            channelNbSearchPerSec.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelNbSearchPerSec.PrepareRecord += channelNbSearchPerSec_PrepareRecord;

            runningTime = server.CreateRecord<CAStringRecord>(System.Environment.MachineName + ":RUNNING-TIME");
            runningTime.CanBeRemotlySet = false;
            runningTime.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC1;
            runningTime.PrepareRecord += runningTime_PrepareRecord;

            // Gateway Version channel
            channelVersion = server.CreateRecord<CAStringRecord>(System.Environment.MachineName + ":VERSION");
            channelVersion.CanBeRemotlySet = false;
            channelVersion.Value = Version; 
            // Gateway build date channel
            channelBuild = server.CreateRecord<CAStringRecord>(System.Environment.MachineName + ":BUILD");
            channelBuild.CanBeRemotlySet = false;
            channelBuild.Value = BuildTime.ToString(System.Globalization.CultureInfo.InvariantCulture);

            channelHeartBeat = server.CreateRecord<CAIntRecord>(System.Environment.MachineName + ":BEAT");
            channelHeartBeat.Value = 0;
            channelHeartBeat.PrepareRecord += channelHeartBeat_PrepareRecord;
            channelHeartBeat.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC1;

        }

        void channelCPU_PrepareRecord(object sender, EventArgs e)
        {
            channelCpu.Value = cpuCounter.NextValue();
        }

        void channelMEM_PrepareRecord(object sender, EventArgs e)
        {
            channelMem.Value = ramCounter.NextValue();
        }

        void channelNbSearchPerSec_PrepareRecord(object sender, EventArgs e)
        {
            channelNbSearchPerSec.Value = NbSearches / 5;
            NbSearches = 0;
        }

        void runningTime_PrepareRecord(object sender, EventArgs e)
        {
            runningTime.Value = (DateTime.Now - startTime).ToString();
        }

        void channelHeartBeat_PrepareRecord(object sender, EventArgs e)
        {
            channelHeartBeat.Value = 1 - channelHeartBeat.Value;
        }

        public static string Version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public static DateTime BuildTime
        {
            get
            {
                string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
                const int cPeHeaderOffset = 60;
                const int cLinkerTimestampOffset = 8;
                byte[] b = new byte[2048];
                using (System.IO.Stream s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    s.Read(b, 0, 2048);
                }

                int i = System.BitConverter.ToInt32(b, cPeHeaderOffset);
                int secondsSince1970 = System.BitConverter.ToInt32(b, i + cLinkerTimestampOffset);
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
                dt = dt.AddSeconds(secondsSince1970);
                dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
                return dt;
            }
        }


        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            server.Dispose();
        }
    }
}
