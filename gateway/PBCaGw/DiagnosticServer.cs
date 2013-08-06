using System;
using System.Globalization;
using CaSharpServer;
using System.Diagnostics;
using System.Net;
using PBCaGw.Services;
using PBCaGw.Workers;
using System.Threading;

namespace PBCaGw
{
    /// <summary>
    /// Handles all the diagnostic channels
    /// </summary>
    public class DiagnosticServer : IDisposable
    {
        readonly CADoubleRecord channelCpu;
        readonly PerformanceCounter cpuCounter;
        readonly CADoubleRecord channelMem;
        readonly PerformanceCounter ramCounter;
        readonly CAIntRecord channelNbClientConn;
        readonly CAIntRecord channelNbServerConn;
        readonly CAIntRecord channelKnownChannels;
        readonly CAIntRecord channelOpenMonitor;
        readonly CAIntRecord channelNbSearchPerSec;
        readonly CAIntRecord channelNbMessagesPerSec;
        readonly CAIntRecord channelNbCreatedPacketPerSec;
        readonly CAIntRecord channelNbPooledPacket;
        readonly CAIntRecord channelNbTcpCreated;
        readonly CAIntRecord channelRestartGateway;
        readonly CAIntRecord channelMaxCid;
        readonly CAStringRecord channelVersion;
        readonly CAStringRecord channelBuild;
        readonly CADoubleRecord channelAverageCpu;
        readonly DateTime startTime = Gateway.Now;
        static public int NbSearches = 0;
        static public int NbMessages = 0;
        static public int NbNewData = 0;
        static public int NbPooledPacket = 0;
        static public int NbTcpCreated = 0;
        bool disposed = false;

        readonly CAServer diagServer;
        readonly Gateway gateway;

        public DiagnosticServer(Gateway gateway, IPAddress address)
        {
            this.gateway = gateway;
            // Starts the diagnostic server
            // using the CAServer library
            diagServer = new CAServer(address, 7890, 7890);
            if (Log.WillDisplay(TraceEventType.Start))
                Log.TraceEvent(TraceEventType.Start, 0, "Starting debug server on " + 7890);
            // CPU usage
            channelCpu = diagServer.CreateRecord<CADoubleRecord>(gateway.Configuration.GatewayName + ":CPU");
            channelCpu.EngineeringUnits = "%";
            channelCpu.CanBeRemotlySet = false;
            channelCpu.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelCpu.PrepareRecord += new EventHandler(channelCPU_PrepareRecord);
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            // Mem free
            channelMem = diagServer.CreateRecord<CADoubleRecord>(gateway.Configuration.GatewayName + ":MEM-FREE");
            channelMem.CanBeRemotlySet = false;
            channelMem.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelMem.EngineeringUnits = "Mb";
            channelMem.PrepareRecord += new EventHandler(channelMEM_PrepareRecord);
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            // NB Client connections
            channelNbClientConn = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":NBCLIENTS");
            channelNbClientConn.CanBeRemotlySet = false;
            channelNbClientConn.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelNbClientConn.PrepareRecord += new EventHandler(channelNbClientConn_PrepareRecord);
            // NB Server connections
            channelNbServerConn = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":NBSERVERS");
            channelNbServerConn.CanBeRemotlySet = false;
            channelNbServerConn.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelNbServerConn.PrepareRecord += new EventHandler(channelNbServerConn_PrepareRecord);
            // Known channels (PV keept)
            channelKnownChannels = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":PVTOTAL");
            channelKnownChannels.CanBeRemotlySet = false;
            channelKnownChannels.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelKnownChannels.PrepareRecord += new EventHandler(channelKnownChannels_PrepareRecord);
            // Open monitors
            channelOpenMonitor = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":MONITORS");
            channelOpenMonitor.CanBeRemotlySet = false;
            channelOpenMonitor.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelOpenMonitor.PrepareRecord += new EventHandler(channelOpenMonitor_PrepareRecord);
            // Searches per sec
            channelNbSearchPerSec = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":SEARCH-SEC");
            channelNbSearchPerSec.CanBeRemotlySet = false;
            channelNbSearchPerSec.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelNbSearchPerSec.PrepareRecord += new EventHandler(channelNbSearchPerSec_PrepareRecord);
            // Messages per sec
            channelNbMessagesPerSec = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":MESSAGES-SEC");
            channelNbMessagesPerSec.CanBeRemotlySet = false;
            channelNbMessagesPerSec.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelNbMessagesPerSec.PrepareRecord += new EventHandler(channelNbMessagesPerSec_PrepareRecord);
            // DataPacket created per sec
            channelNbCreatedPacketPerSec = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":NEWDATA-SEC");
            channelNbCreatedPacketPerSec.CanBeRemotlySet = false;
            channelNbCreatedPacketPerSec.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelNbCreatedPacketPerSec.PrepareRecord += new EventHandler(channelNbCreatedPacketPerSec_PrepareRecord);
            // DataPacket created per sec
            channelNbPooledPacket = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":POOLED-DATA");
            channelNbPooledPacket.CanBeRemotlySet = false;
            channelNbPooledPacket.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelNbPooledPacket.PrepareRecord += new EventHandler(channelNbPooledPacket_PrepareRecord);
            // TCP Created from startup
            channelNbTcpCreated = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":COUNT-TCP");
            channelNbTcpCreated.CanBeRemotlySet = false;
            channelNbTcpCreated.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelNbTcpCreated.PrepareRecord += new EventHandler(channelNbTcpCreated_PrepareRecord);
            // MAX CID (not reused)
            channelMaxCid = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":MAX-CID");
            channelMaxCid.CanBeRemotlySet = false;
            channelMaxCid.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelMaxCid.PrepareRecord += new EventHandler(channelMaxCID_PrepareRecord);
            // Average CPU usage
            channelAverageCpu = diagServer.CreateRecord<CADoubleRecord>(gateway.Configuration.GatewayName + ":AVG-CPU");
            channelAverageCpu.CanBeRemotlySet = false;
            channelAverageCpu.EngineeringUnits = "%";
            channelAverageCpu.Scan = CaSharpServer.Constants.ScanAlgorithm.SEC5;
            channelAverageCpu.PrepareRecord += new EventHandler(channelAverageCpu_PrepareRecord);

            // Restart channel
            channelRestartGateway = diagServer.CreateRecord<CAIntRecord>(gateway.Configuration.GatewayName + ":RESTART");
            channelRestartGateway.Value = 0;
            channelRestartGateway.PropertySet += new EventHandler<PropertyDelegateEventArgs>(channelRestartGateway_PropertySet);
            // Gateway Version channel
            channelVersion = diagServer.CreateRecord<CAStringRecord>(gateway.Configuration.GatewayName + ":VERSION");
            channelVersion.CanBeRemotlySet = false;
            channelVersion.Value = Gateway.Version;
            // Gateway build date channel
            channelBuild = diagServer.CreateRecord<CAStringRecord>(gateway.Configuration.GatewayName + ":BUILD");
            channelBuild.CanBeRemotlySet = false;
            channelBuild.Value = BuildTime.ToString(CultureInfo.InvariantCulture);
        }

        // ReSharper disable InconsistentNaming
        void channelAverageCpu_PrepareRecord(object sender, EventArgs e)
        {
            TimeSpan diff = (Gateway.Now - startTime);
            channelAverageCpu.Value = (Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds / diff.TotalSeconds) * 100 / Environment.ProcessorCount;
        }

        void channelMaxCID_PrepareRecord(object sender, EventArgs e)
        {
            channelMaxCid.Value = (int)CidGenerator.Peek();
        }

        void channelNbTcpCreated_PrepareRecord(object sender, EventArgs e)
        {
            channelNbTcpCreated.Value = NbTcpCreated;
        }

        void channelNbPooledPacket_PrepareRecord(object sender, EventArgs e)
        {
            channelNbPooledPacket.Value = NbPooledPacket;
        }

        void channelNbCreatedPacketPerSec_PrepareRecord(object sender, EventArgs e)
        {
            channelNbCreatedPacketPerSec.Value = NbNewData / 5;
            NbNewData = 0;
        }

        void channelNbMessagesPerSec_PrepareRecord(object sender, EventArgs e)
        {
            channelNbMessagesPerSec.Value = NbMessages / 5;
            NbMessages = 0;
        }

        void channelRestartGateway_PropertySet(object sender, PropertyDelegateEventArgs e)
        {
            if ((int)e.NewValue == 1)
            {
                e.CancelEvent = true;
                gateway.StopGateway();
                Thread.Sleep(2000);
                gateway.LoadConfig();
                gateway.StartGateway();
            }
            else if ((int)e.NewValue == 2)
            {
                e.CancelEvent = true;
                gateway.StopGateway();
                TcpManager.DisposeAll();
                Thread.Sleep(2000);
                gateway.LoadConfig();
                gateway.StartGateway();
            }
        }

        void channelNbSearchPerSec_PrepareRecord(object sender, EventArgs e)
        {
            channelNbSearchPerSec.Value = NbSearches / 5;
            NbSearches = 0;
        }

        void channelOpenMonitor_PrepareRecord(object sender, EventArgs e)
        {
            channelOpenMonitor.Value = InfoService.ChannelSubscription.Count;
        }

        void channelKnownChannels_PrepareRecord(object sender, EventArgs e)
        {
            channelKnownChannels.Value = InfoService.ChannelEndPoint.Count;
        }

        void channelNbServerConn_PrepareRecord(object sender, EventArgs e)
        {
            channelNbServerConn.Value = WorkerChain.NbServerConn();
        }

        void channelNbClientConn_PrepareRecord(object sender, EventArgs e)
        {
            channelNbClientConn.Value = WorkerChain.NbClientConn();
        }

        void channelMEM_PrepareRecord(object sender, EventArgs e)
        {
            channelMem.Value = ramCounter.NextValue();
        }

        void channelCPU_PrepareRecord(object sender, EventArgs e)
        {
            channelCpu.Value = cpuCounter.NextValue();
        }
        // ReSharper restore InconsistentNaming

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            diagServer.Dispose();
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

    }
}
