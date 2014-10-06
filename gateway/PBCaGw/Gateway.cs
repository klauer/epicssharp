using System;
using System.Collections.Concurrent;
using PBCaGw.Services;
using System.Net;
using PBCaGw.Workers;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using PBCaGw.Configurations;
using System.Linq;
using System.Collections.Generic;

namespace PBCaGw
{
    public delegate void NewIocChannelDelegate(string ioc, string channel);
    public delegate void NewClientChannelDelegate(string client, string channel);
    public delegate void DropIocDelegate(string ioc);
    public delegate void DropClientDelegate(string client);

    /// <summary>
    /// Base class for the gateway.
    /// This is the class to instantiate to create a gateway.
    /// </summary>
    public class Gateway : IDisposable
    {
        /// <summary>
        /// Gateway CA protocol version
        /// </summary>
        public const UInt16 CA_PROTO_VERSION = 11;
        /// <summary>
        /// Min. time (in sec) to keep the IOC connection before killing it when there is no more client connections to it.
        /// </summary>
        public const int IOC_KEEP_ALIVE_CONNECTION = 600;
        //public const int IOC_KEEP_ALIVE_CONNECTION = 120;

        /// <summary>
        /// Time without any communication before sending an echo.
        /// </summary>
        //public const int ECHO_INTERVAL = 30;
        public const int ECHO_INTERVAL = 30;

        /// <summary>
        /// Max size of UDP packet sent.
        /// </summary>
        public const int MAX_UDP_SEND_PACKET = 1000;

        /// <summary>
        ///  Max UDP packet size (3x 8192... don't ask me from where it comes, found while looking on the net)
        /// </summary>
        public const int BUFFER_SIZE = 8192 * 30;

        public const int TCP_FLUSH_TIME = 10;
        //public const int TCP_FLUSH_TIME = 200;

        Configuration configuration = new Configuration();
        public Configuration Configuration { get { return configuration; } }

        WorkerChain udpChainA;
        WorkerChain udpResponseChainA;
        GwTcpListener tcpListenerA;
        // ReSharper disable InconsistentNaming
        internal IBeaconResetter beaconA;
        // ReSharper restore InconsistentNaming

        WorkerChain udpChainB;
        GwTcpListener tcpListenerB;
        // ReSharper disable InconsistentNaming
        internal IBeaconResetter beaconB;
        // ReSharper restore InconsistentNaming

        bool disposed = false;

        /// <summary>
        /// Background thread for the periodic jobs
        /// </summary>
        static readonly Thread bgJobs;
        /// <summary>
        /// Wakes up every 10 sec
        /// </summary>
        public static event EventHandler TenSecJobs;
        /// <summary>
        /// Wakes up every 5 sec
        /// </summary>
        public static event EventHandler FiveSecJobs;
        /// <summary>
        /// Wakes up every sec
        /// </summary>
        public static event EventHandler OneSecJobs;

        public event EventHandler UpdateSearch;

        static DateTime now;
        public static DateTime Now { get { return now; } }

        private DiagnosticServer diagnostic;

        internal object searchLock = new object();
        internal Dictionary<string, SearchStat> searchStats = new Dictionary<string, SearchStat>();
        internal Dictionary<string, SearchStat> searchersStats = new Dictionary<string, SearchStat>();

        /// <summary>
        /// Starts the scheduler
        /// </summary>
        static Gateway()
        {
            now = DateTime.Now;
            //GCSettings.LatencyMode = GCLatencyMode.Interactive;

            bgJobs = new Thread(RunBgJobs);
            bgJobs.IsBackground = true;
            bgJobs.Start();


            //BufferedSockets = true;
            BufferedSockets = false;
            AutoCreateChannel = true;
            //AutoCreateChannel = false;
            RestoreCache = true;
            //RestoreCache = false;
        }

        void UpdateSearchList(object sender, EventArgs e)
        {
            if (UpdateSearch != null)
            {
                try
                {
                    UpdateSearch(this, null);
                }
                catch
                {
                }
            }

            lock (searchLock)
            {
                // Cleanup all old
                foreach (var i in searchStats.Where(row => row.Value.CurrentSearch <= 0).Select(row => row.Key).ToList())
                {
                    searchStats.Remove(i);
                }

                foreach (var i in searchStats)
                {
                    i.Value.PreviousSearch = i.Value.CurrentSearch;
                    i.Value.CurrentSearch = 0;
                }

                // Cleanup all old
                foreach (var i in searchersStats.Where(row => row.Value.CurrentSearch <= 0).Select(row => row.Key).ToList())
                {
                    searchersStats.Remove(i);
                }

                foreach (var i in searchersStats)
                {
                    i.Value.PreviousSearch = i.Value.CurrentSearch;
                    i.Value.CurrentSearch = 0;
                }
            }
        }

        internal void GotSearch(string channelname, string searcher)
        {
            lock (searchLock)
            {
                if (!searchStats.ContainsKey(channelname))
                    searchStats.Add(channelname, new SearchStat());
                searchStats[channelname].CurrentSearch++;

                if (!searchersStats.ContainsKey(searcher))
                    searchersStats.Add(searcher, new SearchStat());
                searchersStats[searcher].CurrentSearch++;
            }
        }

        /// <summary>
        /// Fires the events at the right frequency. The timings are not precises.
        /// </summary>
        static void RunBgJobs()
        {
            int jobCounter = 0;
            while (true)
            {
                now = DateTime.Now;
                Thread.Sleep(1000);
                if (jobCounter == 10)
                {
                    ThreadPool.QueueUserWorkItem(RunTenSecJob);
                    jobCounter = 0;
                }
                if (jobCounter % 5 == 0 && FiveSecJobs != null)
                    ThreadPool.QueueUserWorkItem(RunFiveSecJob);

                ThreadPool.QueueUserWorkItem(RunOneSecJob);
                jobCounter++;
            }
            // ReSharper disable FunctionNeverReturns
        }
        // ReSharper restore FunctionNeverReturns

        static void RunTenSecJob(object state)
        {
            /*if(Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "10 sec job running");*/
            if (TenSecJobs != null)
            {
                foreach (var i in TenSecJobs.GetInvocationList())
                {
                    bool faulty = false;
                    for (var n = 0; n < 5; n++)
                    {
                        try
                        {
                            i.Method.Invoke(i.Target, new object[] { null, null });
                            faulty = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                                Log.TraceEvent(TraceEventType.Critical, -1, ex.Message + "\r\n" + ex.StackTrace);
                            faulty = true;
                            Thread.Sleep(500);
                        }
                    }
                    if (faulty)
                        Environment.Exit(1);
                }
                //TenSecJobs(null, null);
            }
            /*if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "10 sec job done");*/
        }

        static void RunFiveSecJob(object state)
        {
            /*if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "5 sec job running");*/
            if (FiveSecJobs != null)
            {
                foreach (var i in FiveSecJobs.GetInvocationList())
                {
                    bool faulty = false;
                    for (var n = 0; n < 5; n++)
                    {
                        try
                        {

                            i.Method.Invoke(i.Target, new object[] { null, null });
                            faulty = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                                Log.TraceEvent(TraceEventType.Critical, -1, ex.Message + "\r\n" + ex.StackTrace);
                            faulty = true;
                            Thread.Sleep(500);
                        }
                    }
                    if (faulty)
                        Environment.Exit(1);
                }
            }
            /*if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "5 sec job done");*/
        }

        static void RunOneSecJob(object state)
        {
            /*if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "1 sec job running");*/
            if (OneSecJobs != null)
            {
                foreach (var i in OneSecJobs.GetInvocationList())
                {
                    bool faulty = false;
                    for (var n = 0; n < 5; n++)
                    {
                        try
                        {
                            i.Method.Invoke(i.Target, new object[] { null, null });
                            faulty = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                                Log.TraceEvent(TraceEventType.Critical, -1, ex.Message + "\r\n" + ex.StackTrace);
                            faulty = true;
                            Thread.Sleep(500);
                        }
                    }
                    if (faulty)
                        Environment.Exit(1);
                }
                //OneSecJobs(null, null);
            }
            /*if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "1 sec job done");*/
        }

        /// <summary>
        /// Starts the gateway
        /// </summary>
        public void Start()
        {
            if (Log.WillDisplay(System.Diagnostics.TraceEventType.Start))
            {
                Log.TraceEvent(TraceEventType.Start, -1, "Starting gateway " + configuration.GatewayName);
                Log.TraceEvent(TraceEventType.Start, -1, "Starting diagnostic");
            }
            diagnostic = new DiagnosticServer(this, configuration.LocalSideB.Address);

            // Starting the gateway chains
            StartGateway();
        }

        internal void StartGateway()
        {
            Configuration.Security.Init();

            if (Log.WillDisplay(System.Diagnostics.TraceEventType.Start))
                Log.TraceEvent(TraceEventType.Start, -1, "Starting gateway chains");
            udpChainA = WorkerChain.UdpChain(this, ChainSide.SIDE_A, configuration.LocalSideA, configuration.RemoteSideB);
            tcpListenerA = new GwTcpListener(this, ChainSide.SIDE_A, configuration.LocalSideA);
            beaconA = new BeaconSender(configuration.UdpReceiverA, configuration.LocalSideA, configuration.RemoteSideA);

            if (configuration.ConfigurationType == ConfigurationType.BIDIRECTIONAL)
            {
                udpChainB = WorkerChain.UdpChain(this, ChainSide.SIDE_B, configuration.LocalSideB, configuration.RemoteSideA);
                tcpListenerB = new GwTcpListener(this, ChainSide.SIDE_B, configuration.LocalSideB);
                beaconB = new BeaconSender(configuration.UdpReceiverB, configuration.LocalSideB, configuration.RemoteSideB);
            }
            else
            {
                udpResponseChainA = WorkerChain.UdpResponseChain(this, ChainSide.UDP_RESP_SIDE_A, configuration.LocalSideB, configuration.RemoteSideA);
            }
            if (Log.WillDisplay(System.Diagnostics.TraceEventType.Start))
                Log.TraceEvent(TraceEventType.Start, -1, "Gateway is ready");

            // Recover channels created on a previous session of the gateway
            if (File.Exists("knownChannels.txt") && RestoreCache)
            {
                string[] channelsToRecover;
                lock (lockChannelList)
                    channelsToRecover = File.ReadAllLines("knownChannels.txt");

                foreach (var channelName in channelsToRecover)
                {
                    DataPacket searchPacket = DataPacket.Create(16 + channelName.Length + DataPacket.Padding(channelName.Length));
                    searchPacket.PayloadSize = (ushort)(channelName.Length + DataPacket.Padding(channelName.Length));
                    searchPacket.Kind = DataPacketKind.COMPLETE;
                    searchPacket.DataType = 0;
                    searchPacket.DataCount = Gateway.CA_PROTO_VERSION;
                    searchPacket.Command = 6;
                    searchPacket.Parameter1 = 0;
                    // Version
                    searchPacket.Parameter2 = 0;
                    searchPacket.Sender = udpChainA.ClientEndPoint;
                    searchPacket.SetDataAsString(channelName);

                    udpChainA[2].ProcessData(searchPacket);
                    if (configuration.ConfigurationType == ConfigurationType.BIDIRECTIONAL)
                    {
                        searchPacket = DataPacket.Create(16 + channelName.Length + DataPacket.Padding(channelName.Length));
                        searchPacket.PayloadSize = (ushort)(channelName.Length + DataPacket.Padding(channelName.Length));
                        searchPacket.Kind = DataPacketKind.COMPLETE;
                        searchPacket.DataType = 0;
                        searchPacket.DataCount = Gateway.CA_PROTO_VERSION;
                        searchPacket.Command = 6;
                        searchPacket.Parameter1 = 0;
                        // Version
                        searchPacket.Parameter2 = 0;
                        searchPacket.Sender = udpChainB.ClientEndPoint;
                        searchPacket.SetDataAsString(channelName);
                        udpChainB[2].ProcessData(searchPacket);
                    }
                }
            }
            TenSecJobs += StoreKnownChannels;
            TenSecJobs += UpdateSearchList;
            //TenSecJobs += new EventHandler(MemoryClean);
        }

        void MemoryClean(object sender, EventArgs e)
        {
            GC.Collect();
        }

        internal void StopGateway()
        {
            if (Log.WillDisplay(System.Diagnostics.TraceEventType.Stop))
                Log.TraceEvent(TraceEventType.Stop, -1, "Stopping gatway chains");

            udpChainA.Dispose();
            if (configuration.ConfigurationType == ConfigurationType.UNIDIRECTIONAL)
                udpResponseChainA.Dispose();
            tcpListenerA.Dispose();
            beaconA.Dispose();

            if (configuration.ConfigurationType == ConfigurationType.BIDIRECTIONAL)
            {
                udpChainB.Dispose();
                //udpResponseChainB.Dispose();
                tcpListenerB.Dispose();
                beaconB.Dispose();
            }

            TenSecJobs -= StoreKnownChannels;
            TenSecJobs -= UpdateSearchList;
            StoreKnownChannels(this, null);

            TcpManager.DisposeAll();
        }

        /// <summary>
        /// Loads the configuration either from an URL or from the gateway.xml file
        /// The URL is the mix of configuration keys configURL and gatewayName
        /// </summary>
        public void LoadConfig()
        {
            bool freshConfig = false;
            try
            {
                if (System.Configuration.ConfigurationManager.AppSettings["configURL"] == null || System.Configuration.ConfigurationManager.AppSettings["gatewayName"] == null)
                    throw new Exception("Direct config");
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                {
                    Log.TraceEvent(TraceEventType.Verbose, -1, "Loading configuration from");
                    Log.TraceEvent(TraceEventType.Verbose, -1, System.Configuration.ConfigurationManager.AppSettings["configURL"] + System.Configuration.ConfigurationManager.AppSettings["gatewayName"]);
                }
                WebClient client = new WebClient();
                string config = client.DownloadString(System.Configuration.ConfigurationManager.AppSettings["configURL"] + System.Configuration.ConfigurationManager.AppSettings["gatewayName"]);
                using (StringReader txtReader = new StringReader(config))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                    configuration = (Configuration)serializer.Deserialize(txtReader);
                    txtReader.Close();
                }
                freshConfig = true;
            }
            catch
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                    Log.TraceEvent(TraceEventType.Verbose, -1, "Loading configuration from gateway.xml");
                using (StreamReader txtReader = new StreamReader("gateway.xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                    configuration = (Configuration)serializer.Deserialize(txtReader);
                    txtReader.Close();
                }
            }

            if (freshConfig)
                SaveConfig();
        }

        /// <summary>
        /// Saves teh configuration in the gateway.xml file
        /// </summary>
        public void SaveConfig()
        {
            using (StreamWriter txtWriter = new StreamWriter("gateway.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                serializer.Serialize(txtWriter, configuration);
                txtWriter.Close();
            }
        }

        public static string Version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }


        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            StopGateway();
            diagnostic.Dispose();
        }

        internal bool IsDisposed
        {
            get
            {
                return disposed;
            }
        }

        public static bool BufferedSockets { get; set; }

        public static bool AutoCreateChannel { get; set; }

        public static bool RestoreCache { get; set; }

        internal readonly ConcurrentDictionary<string, ConcurrentBag<string>> KnownIocs = new ConcurrentDictionary<string, ConcurrentBag<string>>();
        internal readonly ConcurrentDictionary<string, ConcurrentBag<string>> KnownClients = new ConcurrentDictionary<string, ConcurrentBag<string>>();

        public event NewIocChannelDelegate NewIocChannel;
        public event NewClientChannelDelegate NewClientChannel;
        public event DropIocDelegate DropIoc;
        public event DropClientDelegate DropClient;

        readonly object lockChannelList = new object();
        bool channelListIsDirty = false;

        void StoreKnownChannels(object sender, EventArgs e)
        {
            if (!channelListIsDirty)
                return;
            channelListIsDirty = false;
            lock (lockChannelList)
            {
                File.WriteAllLines("knownChannels.txt", KnownIocs.Values.SelectMany(row => row).ToArray());
            }
        }

        internal void DoIocConnectedChannels(string server, string newItem, string oldItem)
        {
            if (!KnownIocs.ContainsKey(server))
                KnownIocs.TryAdd(server, new ConcurrentBag<string>());
            if (newItem != null)
            {
                try
                {
                    KnownIocs[server].Add(newItem);

                    if (NewIocChannel != null)
                        NewIocChannel(server, newItem);
                }
                catch
                {
                }
            }
            else
            {
                string o = oldItem;
                KnownIocs[server].TryTake(out o);

            }

            channelListIsDirty = true;
        }

        internal void DoDropIoc(string server)
        {
            try
            {
                ConcurrentBag<string> value;
                KnownIocs.TryRemove(server, out value);

                if (DropIoc != null)
                    DropIoc(server);
            }
            catch
            {
            }

            channelListIsDirty = true;
        }

        internal void DoClientConnectedChannels(string client, string newItem)
        {
            try
            {
                if (!KnownClients.ContainsKey(client))
                    KnownClients.TryAdd(client, new ConcurrentBag<string>());
                KnownClients[client].Add(newItem);

                if (NewClientChannel != null)
                    NewClientChannel(client, newItem);
            }
            catch
            {
            }
        }

        internal void DoDropClient(string client)
        {
            try
            {
                ConcurrentBag<string> value;
                KnownClients.TryRemove(client, out value);

                if (DropClient != null)
                    DropClient(client);
            }
            catch
            {
            }
        }
    }
}
