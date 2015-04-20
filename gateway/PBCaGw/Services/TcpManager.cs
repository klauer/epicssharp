using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using PBCaGw.Workers;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

namespace PBCaGw.Services
{
    /// <summary>
    /// Handles all the TCP/IP connections and stores the chains corresponding to it.
    /// </summary>
    static class TcpManager
    {

        static readonly Dictionary<IPEndPoint, Socket> iocConnections = new Dictionary<IPEndPoint, Socket>();
        static readonly Dictionary<IPEndPoint, WorkerChain> iocChains = new Dictionary<IPEndPoint, WorkerChain>();

        static readonly Dictionary<IPEndPoint, Socket> clientConnections = new Dictionary<IPEndPoint, Socket>();
        static readonly Dictionary<IPEndPoint, WorkerChain> clientChains = new Dictionary<IPEndPoint, WorkerChain>();

        static readonly Thread bufferFlusher;

        /// <summary>
        /// Creates the buffer dictionary
        /// </summary>
        static TcpManager()
        {
            if (Gateway.BufferedSockets)
            {
                bufferFlusher = new Thread(BufferFlusher);
                bufferFlusher.IsBackground = true;
                bufferFlusher.Start();
            }
        }


        /// <summary>
        /// Flushes the buffers every now and then
        /// </summary>
        static void BufferFlusher()
        {
            Stopwatch sw = new Stopwatch();
            int diff = 0;
            while (Gateway.BufferedSockets)
            {
                if (diff < Gateway.TCP_FLUSH_TIME)
                    Thread.Sleep(Gateway.TCP_FLUSH_TIME - diff);
                else
                    Thread.Sleep(0);

                sw.Reset();
                sw.Start();

                List<TcpReceiver> receivers;
                lock (clientConnections)
                {
                    receivers = clientChains.Select(row => ((TcpReceiver)row.Value[0])).Where(row => row.IsDirty).ToList();
                }

                Parallel.ForEach(receivers, delegate(TcpReceiver row)
                {
                    try
                    {
                        row.Flush();
                    }
                    catch
                    {
                        DisposeSocket(row.Socket);
                    }
                });

                lock (iocConnections)
                {
                    receivers = iocChains.Select(row => (TcpReceiver)row.Value[0]).Where(row => row.IsDirty).ToList();
                }

                Parallel.ForEach(receivers, delegate(TcpReceiver row)
                    {
                        try
                        {
                            row.Flush();
                        }
                        catch
                        {
                            DisposeSocket(row.Socket);
                        }
                    });

                sw.Stop();
                diff = (int)sw.ElapsedMilliseconds;
            }
            // ReSharper disable FunctionNeverReturns
        }
        // ReSharper restore FunctionNeverReturns


        public static void DisposeGlobalChannel(string channelName)
        {
            Record r = InfoService.ChannelEndPoint[channelName];
            /*if (r != null)
            {
                InfoService.IOID.DeleteForSID(r.GWCID.Value);
            }*/
            InfoService.ChannelEndPoint.Remove(channelName);

            List<KeyValuePair<IPEndPoint, WorkerChain>> l;
            lock (clientConnections)
            {
                l = clientChains.Where(row => row.Value.ChannelCid.Any(r2 => r2.Key == channelName)).ToList();
            }
            foreach (var i in l)
            {
                if (!i.Value.ChannelCid.ContainsKey(channelName))
                    continue;
                DataPacket packet = DataPacket.Create(16);
                packet.Destination = i.Key;
                packet.Command = 27;
                packet.PayloadSize = 0;
                packet.DataCount = 0;
                packet.DataType = 0;
                try
                {
                    packet.Parameter1 = i.Value.ChannelCid[channelName];
                }
                catch
                {
                    continue;
                }
                packet.Parameter2 = 0;
                ((TcpReceiver)i.Value[0]).Send(packet);

                uint u;
                i.Value.ChannelCid.TryRemove(channelName, out u);
                i.Value.Channels.TryTake(channelName);
            }
        }

        /// <summary>
        /// Disposes a socket
        /// </summary>
        /// <param name="socket"></param>
        private static void DisposeSocket(Socket socket)
        {
            WorkerChain chain = null;
            IPEndPoint a;
            lock (iocConnections)
            {
                a = iocConnections.Where(row => row.Value == socket).Select(row => row.Key).FirstOrDefault();
                if (a != null)
                {
                    if (iocChains.ContainsKey(a))
                        chain = iocChains[a];
                }
            }

            if (a == null)
            {
                lock (clientConnections)
                {
                    a = clientConnections.Where(row => row.Value == socket).Select(row => row.Key).FirstOrDefault();
                    if (a != null)
                    {
                        if (clientChains.ContainsKey(a))
                            chain = clientChains[a];
                    }
                }
            }
            if (chain != null)
                chain.Dispose();
        }

        /// <summary>
        /// Registers or retreive a server (IOC) chain.
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static WorkerChain GetIocChain(Gateway gateway, IPEndPoint endPoint)
        {
            lock (iocConnections)
            {
                if (endPoint == null)
                {
                }
                Debug.Assert(endPoint != null, "endPoint != null");
                if (iocConnections.ContainsKey(endPoint) && iocChains.ContainsKey(endPoint))
                    return iocChains[endPoint];
            }

            if (gateway == null)
                return null;

            WorkerChain chain = WorkerChain.TcpResponseChain(gateway, ChainSide.SERVER_CONN, endPoint, endPoint);
            WorkerChain result = null;

            lock (iocConnections)
            {
                if (iocConnections.ContainsKey(endPoint) && iocChains.ContainsKey(endPoint))
                    result = iocChains[endPoint];

                if (result == null)
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                    try
                    {
                        if (!SocketConnect(socket, endPoint, 200))
                        {
                            try
                            {
                                socket.Dispose();
                            }
                            catch
                            {

                            }
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        socket.Dispose();
                        return null;
                    }

                    try
                    {
                        ((TcpReceiver)chain[0]).Socket = socket;
                    }
                    catch
                    {
                        try
                        {
                            chain.Dispose();
                        }
                        catch
                        {

                        }
                        return null;
                    }


                    // Add a monitor on the known channels
                    chain.Channels.BagModified += (ConcurrentBagModification<string>)((bag, newItem, oldItem) => gateway.DoIocConnectedChannels(endPoint.ToString(), newItem, oldItem));
                    if (iocConnections.ContainsKey(endPoint))
                        iocConnections.Remove(endPoint);
                    iocConnections.Add(endPoint, socket);
                    iocChains.Add(endPoint, chain);

                    if (Log.WillDisplay(System.Diagnostics.TraceEventType.Start))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Start, -1, "Building new IOC chain for " + endPoint);
                    result = chain;
                }
            }

            if (!object.ReferenceEquals(result, chain))
                chain.Dispose();
            return result;
        }

        static bool SocketConnect(Socket socket, EndPoint endPoint, int connTimeout)
        {
            IAsyncResult result = socket.BeginConnect(endPoint, null, null);
            bool succeed = result.AsyncWaitHandle.WaitOne(connTimeout, true);
            if (!succeed)
            {
                try
                {
                    socket.Close();
                }
                catch
                {

                }
            }
            return succeed;
        }

        /// <summary>
        /// Registers or retreive a client (MEDM) chain.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static WorkerChain GetClientChain(IPEndPoint endPoint)
        {
            lock (clientConnections)
            {
                if (!clientConnections.ContainsKey(endPoint))
                    return null;
                return clientChains[endPoint];
            }
        }

        /// <summary>
        /// Sends a packet to a server connection
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="packet"></param>
        public static void SendIocPacket(Gateway gateway, DataPacket packet)
        {
            WorkerChain chain = GetIocChain(gateway, packet.Destination);

            if (chain != null)
            {
                chain.LastMessage = Gateway.Now;

                try
                {
                    ((TcpReceiver)chain[0]).Send(packet);
                }
                catch
                {
                    if (Log.WillDisplay(System.Diagnostics.TraceEventType.Stop))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Stop, (packet.Chain == null ? 0 : packet.Chain.ChainId), "Closing IOC TCP: Error while sending to " + packet.Destination);
                    chain.Dispose();
                }
            }
        }

        /// <summary>
        /// Sends a packet to a client connection
        /// </summary>
        /// <param name="packet"></param>
        public static void SendClientPacket(DataPacket packet)
        {
            //Console.WriteLine("Sending to " + packet.Destination + " " + packet.Command);
            //Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, packet.Chain.ChainId, "Pre Sending " + packet.Command);

            Socket socket = null;
            // Not sending to null
            if (packet.Destination == null)
            {
                //Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, packet.Chain.ChainId, "Dest null");
                return;
            }
            WorkerChain clientChain = null;

            lock (clientConnections)
            {
                if (!clientConnections.ContainsKey(packet.Destination))
                {
                    //Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, packet.Chain.ChainId, "client conn null? " + packet.Destination);
                    return;
                }
                if (clientChains.ContainsKey(packet.Destination))
                    clientChain = clientChains[packet.Destination];
            }

            //Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, packet.Chain.ChainId, "Sending " + packet.Command);

            if (clientChain != null)
            {
                clientChain.LastMessage = Gateway.Now;
                try
                {
                    ((TcpReceiver)clientChain[0]).Send(packet);
                }
                catch
                {
                    try
                    {
                        if (Log.WillDisplay(TraceEventType.Stop) && packet != null && packet.Chain != null)
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Stop, packet.Chain.ChainId, "Closing Client TCP: Error while sending to " + packet.Destination);
                    }
                    catch
                    {
                    }
                    clientChain.Dispose();
                }
            }
        }

        /// <summary>
        /// Called by the TCP Listener when receiving a new connection.
        /// Stores a client (request) chain.
        /// </summary>
        /// <param name="iPEndPoint"></param>
        /// <param name="chain"></param>
        internal static void RegisterClient(IPEndPoint iPEndPoint, WorkerChain chain)
        {
            lock (clientConnections)
            {
                if (Log.WillDisplay(TraceEventType.Verbose))
                    Log.TraceEvent(TraceEventType.Verbose, -1, "Register " + iPEndPoint);
                clientConnections.Add(iPEndPoint, ((TcpReceiver)chain[0]).Socket);
                clientChains.Add(iPEndPoint, chain);
            }
        }

        /// <summary>
        /// Removes a client (request) chain.
        /// Could be triggered from a dispose of the TcpReceiver even in the IOC chain
        /// therefore we must check if the chain is indeed a client chain.
        /// </summary>
        /// <param name="iPEndPoint"></param>
        internal static void DropClientConnection(IPEndPoint iPEndPoint)
        {
            if (iPEndPoint == null)
                return;

            WorkerChain chain = null;
            lock (clientConnections)
            {
                if (!clientConnections.ContainsKey(iPEndPoint))
                    return;

                chain = clientChains[iPEndPoint];
            }

            try
            {
                foreach (var i in chain.ChannelCid)
                {
                    DataPacket packet = DataPacket.Create(16);
                    packet.Destination = iPEndPoint;
                    packet.Command = 27;
                    packet.PayloadSize = 0;
                    packet.DataCount = 0;
                    packet.DataType = 0;
                    packet.Parameter1 = i.Value;
                    packet.Parameter2 = 0;

                    ((TcpReceiver)chain[0]).Send(packet);
                }
                //((TcpReceiver)chain[0]).Flush();
            }
            catch
            {
            }

            try
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Stop))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Stop, chain.ChainId, "Disposing client chain " + iPEndPoint);
                lock (clientConnections)
                {
                    clientConnections.Remove(iPEndPoint);
                    clientChains.Remove(iPEndPoint);
                }
                chain.Gateway.DoDropClient(iPEndPoint.ToString());
            }
            catch
            {
            }
            if (chain != null)
                chain.Dispose();
        }

        /// <summary>
        /// Removes an IOC (response) chain
        /// </summary>
        /// <param name="chain"></param>
        internal static void DropServerConnection(WorkerChain chain)
        {
            if (chain == null || chain[0] as TcpReceiver == null || ((TcpReceiver)chain[0]).Socket == null)
                return;

            IPEndPoint endPoint;
            try
            {
                endPoint = ((TcpReceiver)chain[0]).RemoteEndPoint;
            }
            catch
            {
                return;
            }

            Socket toDisconnect = null;
            lock (iocConnections)
            {
                if (!iocConnections.ContainsKey(endPoint))
                    return;

                if (iocChains.ContainsKey(endPoint))
                {
                    if (Log.WillDisplay(System.Diagnostics.TraceEventType.Stop))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Stop, iocChains[endPoint].ChainId, "Disposing IOC chain " + endPoint);
                    iocChains.Remove(endPoint);
                }
                toDisconnect = iocConnections[endPoint];
                iocConnections.Remove(endPoint);
            }

            try
            {
                toDisconnect.Disconnect(false);
            }
            catch
            {
            }

            // Cleanup which shall not be usefull but somehow we get wrong data..
            // It's a work around not the real fix sadly.
            List<string> toCleanup = InfoService.ChannelEndPoint.OfType<KeyValuePair<string, Record>>()
                .Where(row => row.Value.Destination != null
                    && row.Value.Destination.ToString() == endPoint.ToString())
                .Select(row => row.Key).ToList();

            foreach (var i in toCleanup)
                InfoService.ChannelEndPoint.Remove(i);

            chain.Gateway.DoDropIoc(endPoint.ToString());
            chain.Dispose();
        }

        public static void DisposeAll()
        {
            List<IPEndPoint> clients;
            lock (clientConnections)
            {
                clients = clientChains.Keys.ToList();
            }

            foreach (var i in clients)
                DropClientConnection(i);

            List<WorkerChain> iocs;
            lock (iocConnections)
            {
                iocs = iocChains.Values.ToList();
            }
            foreach (var i in iocs)
                DropServerConnection(i);
        }
    }
}
