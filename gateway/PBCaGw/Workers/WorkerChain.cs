using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using PBCaGw.Services;
using System.Collections.Concurrent;
using System.Threading;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Stores a processing chain composed of workers
    /// </summary>
    public class WorkerChain : IDisposable
    {
        static int nextChainId = 1;
        private readonly List<Worker> workers = new List<Worker>();
        private Worker firstWorker = null;
        private Worker lastWorker = null;
        public IPEndPoint ClientEndPoint = null;
        public IPEndPoint ServerEndPoint = null;
        public Gateway Gateway;
        public ChainSide Side { get; set; }
        private readonly ConcurrentDictionary<WorkerChain, WorkerChain> usedBy = new ConcurrentDictionary<WorkerChain, WorkerChain>();
        public readonly ObservableConcurrentBag<string> Channels = new ObservableConcurrentBag<string>();
        public ConcurrentDictionary<string, uint> ChannelCid = new ConcurrentDictionary<string, uint>();
        public ConcurrentDictionary<uint, uint> Subscriptions = new ConcurrentDictionary<uint, uint>();
        // Stores IOC open monitors
        public ConcurrentDictionary<string, uint> ChannelSubscriptions = new ConcurrentDictionary<string, uint>();
        public DateTime LastMessage = Gateway.Now;
        /// <summary>
        /// Used for the search command
        /// </summary>
        public List<IPEndPoint> Destinations { get; set; }

        //static readonly object lockChainManagement = new object();
        //static readonly List<WorkerChain> knownChains = new List<WorkerChain>();
        static ConcurrentBag<WorkerChain> knownChains = new ConcurrentBag<WorkerChain>();


        public bool IsDisposed = false;
        public bool IsDisposing = false;

        /// <summary>
        /// For a client connection, stores the passed username.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// For a client connection, stores the passed hostname.
        /// </summary>
        public string Hostname { get; set; }

        readonly int chainId = nextChainId++;

        /// <summary>
        /// The chain ID (unique chain identifier).
        /// </summary>
        public int ChainId { get { return chainId; } }

        /// <summary>
        /// For an IOC connection, marks when not anymore linked to any client connection.
        /// </summary>
        DateTime? lastNonUsed = null;

        static WorkerChain()
        {
            Gateway.TenSecJobs += new EventHandler(GatewayTenSecJobs);
        }

        public void DisposeChannel(string channelName, uint GWCID)
        {
            if (channelName == null)
            {
                Log.TraceEvent(TraceEventType.Critical, this.chainId, "Dispose channel null? => lost info, dropping chain.");
                this.Dispose();
                return;
            }
            if (this.ChannelCid.ContainsKey(channelName))
            {
                DataPacket packet = DataPacket.Create(16);
                packet.Destination = this.ClientEndPoint;
                packet.Command = 27;
                packet.PayloadSize = 0;
                packet.DataCount = 0;
                packet.DataType = 0;
                packet.Parameter1 = this.ChannelCid[channelName];
                packet.Parameter2 = 0;
                ((TcpReceiver)this[0]).Send(packet);
            }

            //PBCaGw.Handlers.EventAdd.Unsubscribe()

            uint u;
            if (Subscriptions.Any(row => row.Value == GWCID))
            {
                Handlers.EventAdd.Unsubscribe(GWCID);
                var v = Subscriptions.FirstOrDefault(row => row.Value == GWCID);
                Subscriptions.TryRemove(v.Key, out u);
            }

            this.ChannelCid.TryRemove(channelName, out u);
            this.Channels.TryTake(channelName);
        }

        static int nbWaitToExecute = 0;
        /// <summary>
        /// Cleanup server chains (IOC) which are not used since more than Gateway.IOC_KEEP_ALIVE_CONNECTION
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void GatewayTenSecJobs(object sender, EventArgs e)
        {
            nbWaitToExecute--;
            if (nbWaitToExecute > 0)
                return;
            nbWaitToExecute = 3;

            List<WorkerChain> toWakeUp;
            /*lock (lockChainManagement)
            {
                toWakeUp = knownChains.Where(row =>
                    row.Side != ChainSide.DEBUG_PORT &&
                    row[0] is TcpReceiver &&
                    (Gateway.Now - row.LastMessage).TotalSeconds > Gateway.ECHO_INTERVAL).ToList();
            }*/
            toWakeUp = knownChains.Where(row =>
    row.Side != ChainSide.DEBUG_PORT &&
    row[0] is TcpReceiver &&
    (Gateway.Now - row.LastMessage).TotalSeconds > Gateway.ECHO_INTERVAL).ToList();
            foreach (WorkerChain chain in toWakeUp)
            {
                DataPacket newPacket = DataPacket.Create(0, null);
                newPacket.Command = 23;
                if (chain.Side == ChainSide.SERVER_CONN)
                {
                    newPacket.Destination = chain.ServerEndPoint;
                    InfoService.EchoSent[newPacket.Destination] = new Record();
                    if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, chain.ChainId, "Sending echo to server " + (newPacket.Destination == null ? "" : newPacket.Destination.ToString()));
                    TcpManager.SendIocPacket(null, newPacket);
                }
                else
                {
                    newPacket.Destination = chain.ClientEndPoint;
                    InfoService.EchoSent[newPacket.Destination] = new Record();
                    if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, chain.ChainId, "Sending echo to client " + (newPacket.Destination == null ? "" : newPacket.Destination.ToString()));
                    TcpManager.SendClientPacket(newPacket);
                }
            }

            toWakeUp = knownChains.Where(row =>
                row.Side != ChainSide.DEBUG_PORT &&
                row[0] is TcpReceiver &&
                (Gateway.Now - row.LastMessage).TotalSeconds > Gateway.ECHO_INTERVAL * 2).ToList();
            foreach (WorkerChain chain in toWakeUp)
            {
                chain.Dispose();
            }

            // Cleanup server chains (IOC) which are not used since more than Gateway.IOC_KEEP_ALIVE_CONNECTION
            List<WorkerChain> toDrop;
            /*lock (lockChainManagement)
            {
                toDrop = knownChains
                    .Where(row => row.Side == ChainSide.SERVER_CONN
                        && row.usedBy.Count == 0
                        && row.lastNonUsed != null
                        && (Gateway.Now - row.lastNonUsed.Value).TotalSeconds > Gateway.IOC_KEEP_ALIVE_CONNECTION).ToList();
            }*/
            toDrop = knownChains
    .Where(row => row.Side == ChainSide.SERVER_CONN
        && row.usedBy.Count == 0
        && row.lastNonUsed != null
        && (Gateway.Now - row.lastNonUsed.Value).TotalSeconds > Gateway.IOC_KEEP_ALIVE_CONNECTION).ToList();

            List<string> chainRemoved = new List<string>();
            foreach (WorkerChain chain in toDrop)
            {
                if (chainRemoved.Contains(chain.ServerEndPoint.ToString()))
                    continue;
                chainRemoved.Add(chain.ServerEndPoint.ToString());

                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Stop))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Stop, chain.chainId, "IOC Chain too old: " + chain.ServerEndPoint);
                TcpManager.DropServerConnection(chain);
                chain.Dispose();
            }

            // Cleanup client chains (MEDM) which didn't send any message for at least 2x KEEP_ALIVE
            /*lock (lockChainManagement)
            {
                toDrop = knownChains
                    .Where(row => row.Side != ChainSide.DEBUG_PORT
                        && row.Side != ChainSide.SERVER_CONN
                        && row[0] is TcpReceiver
                        && (Gateway.Now - row.LastMessage).TotalSeconds > Gateway.ECHO_INTERVAL * 2).ToList();
            }*/
            toDrop = knownChains
    .Where(row => row.Side != ChainSide.DEBUG_PORT
        && row.Side != ChainSide.SERVER_CONN
        && row[0] is TcpReceiver
        && (Gateway.Now - row.LastMessage).TotalSeconds > Gateway.ECHO_INTERVAL * 2).ToList();
            foreach (WorkerChain chain in toDrop)
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Stop))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Stop, chain.chainId, "Chain is not talking anymore: " + chain.ClientEndPoint);
                TcpManager.DropClientConnection(chain.ClientEndPoint);
            }
        }

        public static int NbClientConn()
        {
            return knownChains.Count(row => row.Side != ChainSide.SERVER_CONN && row[0] is TcpReceiver);
            /*lock (lockChainManagement)
            {
                return knownChains.Count(row => row.Side != ChainSide.SERVER_CONN && row[0] is TcpReceiver);
            }*/
        }

        public static int NbServerConn()
        {
            return knownChains.Count(row => row.Side == ChainSide.SERVER_CONN && row[0] is TcpReceiver);
            /*lock (lockChainManagement)
            {
                return knownChains.Count(row => row.Side == ChainSide.SERVER_CONN && row[0] is TcpReceiver);
            }*/
        }

        /// <summary>
        /// Default constructor.
        ///  Stores the chain in the knownChains list to be able to know all the current chains.
        /// </summary>
        private WorkerChain()
        {
        }

        static bool isCleaning = false;
        static object cleaningLock = new object();

        /// <summary>
        /// Dispose the current chain.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed || IsDisposing)
                return;

            IsDisposing = true;
            knownChains = new ConcurrentBag<WorkerChain>(knownChains.Where(row => row != this));

            if (workers[0] is TcpReceiver)
            {
                if (Side == ChainSide.SERVER_CONN)
                    TcpManager.DropServerConnection(this);
                else
                {
                    try
                    {
                        IPEndPoint addr = ((TcpReceiver)workers[0]).RemoteEndPoint;
                        TcpManager.DropClientConnection(addr);
                    }
                    catch
                    {
                    }
                }
            }

            foreach (var i in workers)
                i.Dispose();

            // Unsubscribe all monitors used by a given client (used only by client chains)
            foreach (var monitor in Subscriptions)
                Handlers.EventAdd.Unsubscribe(monitor.Value);

            var q = InfoService.SubscribedChannel.Where(row => row.Value.Server == this.ServerEndPoint).ToList();
            foreach (var i in q)
            {
                Record r = i.Value;
                if (r == null)
                    continue;
                //Console.WriteLine("Removing "+i.Key);
                foreach (var j in r.SubscriptionList)
                {
                    if(InfoService.ChannelSubscription.Remove(j))
                        CidGenerator.ReleaseCid(j);
                }
                Debug.Assert(r.GWCID != null, "r.GWCID != null");
                if(InfoService.ChannelSubscription.Remove(r.GWCID.Value))
                    CidGenerator.ReleaseCid(r.GWCID.Value);
                InfoService.SubscribedChannel.Remove(i.Key);
            }

            // Remove channel subscriptions
            foreach (var i in ChannelSubscriptions)
            {
                Record r = InfoService.SubscribedChannel[i.Key];
                if (r == null)
                    continue;
                foreach (var j in r.SubscriptionList)
                {
                    if(InfoService.ChannelSubscription.Remove(j))
                        CidGenerator.ReleaseCid(j);
                }
                if(r.GWCID != null)
                    if(InfoService.ChannelSubscription.Remove(r.GWCID.Value))
                        CidGenerator.ReleaseCid(r.GWCID.Value);
                InfoService.SubscribedChannel.Remove(i.Key);
            }

            // Remove all channels linked to this chain (used only by IOC chains)
            foreach (string channel in Channels)
            {
                InfoService.SearchChannelEndPointA.Remove(channel);
                InfoService.SearchChannelEndPointB.Remove(channel);

                q = InfoService.SubscribedChannel.Where(row => row.Value.Channel == channel).ToList();
                foreach(var i in q)
                {
                    Record r = i.Value;
                    if (r == null)
                        continue;
                    foreach (var j in r.SubscriptionList)
                    {
                        if(InfoService.ChannelSubscription.Remove(j))
                            CidGenerator.ReleaseCid(j);
                    }
                    if (r.GWCID != null)
                        if (InfoService.ChannelSubscription.Remove(r.GWCID.Value))
                            CidGenerator.ReleaseCid(r.GWCID.Value);
                    InfoService.SubscribedChannel.Remove(i.Key);
                }

                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, ChainId, "Dropping channel " + channel);
                List<WorkerChain> channelsToDrop;
                channelsToDrop = knownChains
                        .Where(row => row.Side != ChainSide.SERVER_CONN && row.ChannelCid.ContainsKey(channel)).ToList();
                // Disonnect all clients which use a channel
                TcpManager.DisposeGlobalChannel(channel);
                /*foreach (WorkerChain chain in channelsToDrop)
                    chain.Dispose();*/

                try
                {
                    Record record = InfoService.ChannelEndPoint[channel];
                    if (record != null && record.GWCID != null)
                    {
                        if(InfoService.ChannelCid.Remove(record.GWCID.Value))
                            CidGenerator.ReleaseCid(record.GWCID.Value);
                    }
                    /*else
                    {
                        if (Log.WillDisplay(System.Diagnostics.TraceEventType.Error))
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Error, ChainId, "Channel GWCID unkown...");
                    }*/
                    InfoService.ChannelEndPoint.Remove(channel);
                }
                catch
                {
                }
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, ChainId, "NB known channels: " + InfoService.ChannelEndPoint.Count);
            }


            var q2=InfoService.ChannelSubscription.Where(row => row.Value.Destination == this.ServerEndPoint).ToList();
            foreach(var i in q2)
            {
                if(InfoService.ChannelSubscription.Remove(i.Key))
                    CidGenerator.ReleaseCid(i.Key);
            }

            q2 = InfoService.ChannelCid.Where(row => row.Value.Destination == this.ServerEndPoint).ToList();
            foreach (var i in q2)
            {
                if (InfoService.ChannelCid.Remove(i.Key))
                    CidGenerator.ReleaseCid(i.Key);
            }

            // Remove the chain from the other chains
            WorkerChain outValue;
            foreach (WorkerChain chain in knownChains)
                chain.usedBy.TryRemove(this, out outValue);

            // Mark all chains which may need to be disconnected
            foreach (var i in knownChains.Where(row => row.Side == ChainSide.SERVER_CONN
                        && row.usedBy.Count == 0 && row.lastNonUsed == null))
                i.lastNonUsed = Gateway.Now;

            IsDisposed = true;
        }

        /// <summary>
        /// Link 2 chains together (a client which is connected to an IOC)
        /// </summary>
        /// <param name="chain"></param>
        public void UseChain(WorkerChain chain)
        {
            if (chain == null)
                return;
            chain.lastNonUsed = null;
            if (!chain.usedBy.ContainsKey(this))
                chain.usedBy.TryAdd(this, this);
        }

        public Worker this[int key]
        {
            get
            {
                if (key < 0 || key >= workers.Count)
                    return null;
                return workers[key];
            }
        }

        /// <summary>
        /// Adds the next Worker to the chain and register it to the previous Worker to the ReceiveData event.
        /// </summary>
        /// <param name="worker"></param>
        public void Add(Worker worker)
        {
            workers.Add(worker);
            if (firstWorker == null)
                firstWorker = worker;
            else
                lastWorker.ReceiveData += new ReceiveDataDelegate(worker.ProcessData);
            lastWorker = worker;
        }

        public void RemoveLast()
        {
            if (workers.Count > 1)
                workers[workers.Count - 2].ReceiveData -= new ReceiveDataDelegate(workers[workers.Count - 1].ProcessData);
            if (workers.Count > 0)
            {
                workers.RemoveAt(workers.Count - 1);
                lastWorker = workers.Last();
            }
        }

        public int Count
        {
            get
            {
                return workers.Count;
            }
        }

        static readonly Type[] udpChainList = new Type[] { typeof(UdpReceiver), typeof(PacketSplitter), typeof(RequestCommand), typeof(PacketPacker), typeof(UdpSender) };
        //static readonly Type[] udpChainList = new Type[] { typeof(UdpReceiver), typeof(PacketSplitter), typeof(RequestCommand), typeof(UdpSender) };

        //static Type[] BeaconReceiverChainList = new Type[] { typeof(UdpReceiver), typeof(PacketSplitter), typeof(RequestCommand) };
        static readonly Type[] beaconReceiverChainList = new Type[] { typeof(UdpReceiver), typeof(PacketSplitter), typeof(BeaconCommand) };

        static readonly Type[] udpResponseChainList = new Type[] { typeof(UdpReceiver), typeof(PacketSplitter), typeof(ResponseCommand), typeof(UdpSender) };

        static readonly Type[] tcpChainList = new Type[] { typeof(TcpReceiver), typeof(PacketSplitter), typeof(RequestCommand), typeof(TcpIocSender) };

        static readonly Type[] tcpResponseChainList = new Type[] { typeof(TcpReceiver), typeof(PacketSplitter), typeof(ResponseCommand), typeof(TcpClientSender) };

        /// <summary>
        /// Creates a request UDP chain
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="side"> </param>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        public static WorkerChain UdpChain(Gateway gateway, ChainSide side, IPEndPoint client, List<IPEndPoint> server)
        {
            WorkerChain res = PopulateChain(gateway, side, udpChainList, client, server[0]);
            res.Destinations = server;
            return res;
        }

        /// <summary>
        /// Creates a request UDP chain
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="side"> </param>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        public static WorkerChain UdpResponseChain(Gateway gateway, ChainSide side, IPEndPoint client, List<IPEndPoint> server)
        {
            return PopulateChain(gateway, side, udpResponseChainList, client, server[0]);
        }

        /// <summary>
        /// Creates a request TCP chain
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="side"> </param>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        public static WorkerChain TcpChain(Gateway gateway, ChainSide side, IPEndPoint client, IPEndPoint server)
        {
            return PopulateChain(gateway, side, tcpChainList, client, server);
        }

        /// <summary>
        /// Creates a response TCP chains
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="side"> </param>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        public static WorkerChain TcpResponseChain(Gateway gateway, ChainSide side, IPEndPoint client, IPEndPoint server)
        {
            return PopulateChain(gateway, side, tcpResponseChainList, client, server);
        }

        public static WorkerChain UdpBeaconReceiver(Gateway gateway, ChainSide side, IPEndPoint client, List<IPEndPoint> server)
        {
            return PopulateChain(gateway, side, beaconReceiverChainList, client, server[0]);
        }

        /// <summary>
        /// Used to populate the chain based on a list of types
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="side"> </param>
        /// <param name="workersNeeded"></param>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        static WorkerChain PopulateChain(Gateway gateway, ChainSide side, IEnumerable<Type> workersNeeded, IPEndPoint client, IPEndPoint server)
        {
            WorkerChain chain = new WorkerChain { ClientEndPoint = client, ServerEndPoint = server, Gateway = gateway, Side = side };

            foreach (Type t in workersNeeded)
            {
                // ReSharper disable PossibleNullReferenceException
                Worker w = (Worker)t.GetConstructor(new Type[] { }).Invoke(new object[] { });
                // ReSharper restore PossibleNullReferenceException
                w.Chain = chain;
                w.ClientEndPoint = client;
                w.ServerEndPoint = server;
                chain.Add(w);
            }

            /*lock (lockChainManagement)
            {
                knownChains.Add(chain);
            }*/
            knownChains.Add(chain);

            return chain;
        }
    }
}
