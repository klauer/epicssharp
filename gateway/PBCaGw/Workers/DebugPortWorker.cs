using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using GatewayDebugData;
using System.IO;
using System.Linq;
using System;

namespace PBCaGw.Workers
{

    class DebugPortWorker : Worker
    {
        readonly Socket socket;
        readonly BufferedStream sendStream;
        bool firstMessage = true;
        Socket forwarder = null;
        bool firstForward = true;

        readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        private bool disposed = false;

        public DebugPortWorker(WorkerChain chain, IPEndPoint client, IPEndPoint server, DataPacket packet)
        {
            Chain = chain;
            socket = ((TcpReceiver)Chain[0]).Socket;
            base.ClientEndPoint = client;
            base.ServerEndPoint = server;

            string destGateway = GetString(packet, 1);

            // We need to act as a forwarder
            if (destGateway != chain.Gateway.Configuration.GatewayName)
            {
                PBCaGw.Services.Record record;
                record = PBCaGw.Services.InfoService.ChannelEndPoint[destGateway + ":VERSION"];
                if (record == null)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                    return;
                }

                forwarder = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                forwarder.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                forwarder.Connect(record.Destination);

                forwarder.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);

                /*sendStream = new BufferedStream(new NetworkStream(forwarder));
                sendStream.WriteByte(126);
                Send(destGateway);
                Flush();*/

                forwarder.Send(packet.Data, 0, packet.BufferSize, SocketFlags.None);

                return;
            }

            sendStream = new BufferedStream(new NetworkStream(socket));
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.GW_NAME);
                    Send(Chain.Gateway.Configuration.GatewayName);

                    Send((int)DebugDataType.FULL_IOC);
                    Send(Chain.Gateway.KnownIocs.Count);
                    foreach (var i in Chain.Gateway.KnownIocs)
                    {
                        Send(i.Key);
                        Send(i.Value.Count);
                        foreach (var j in i.Value)
                        {
                            Send(j);
                        }
                    }
                    Flush();

                    Send((int)DebugDataType.FULL_CLIENT);
                    Send(Chain.Gateway.KnownClients.Count);
                    foreach (var i in Chain.Gateway.KnownClients)
                    {
                        Send(i.Key);
                        Send(i.Value.Count);
                        foreach (var j in i.Value)
                        {
                            Send(j);
                        }
                    }
                    Flush();

                    if (PBCaGw.Services.DebugTraceListener.TraceAll)
                        Send((int)DebugDataType.FULL_LOGS);
                    else
                        Send((int)DebugDataType.CRITICAL_LOGS);
                    Flush();

                    PBCaGw.Services.LogEntry[] logs = PBCaGw.Services.DebugTraceListener.LastEntries;
                    foreach (var i in logs)
                    {
                        Send((int)DebugDataType.LOG);
                        Send(i.Source);
                        Send((int)i.EventType);
                        Send(i.Id);
                        Send(i.Message);
                    }
                    Flush();
                }
                SendSearch(null, null);
            }
            catch
            {
                Chain.Dispose();
            }

            Chain.Gateway.NewIocChannel += new NewIocChannelDelegate(GatewayNewIocChannel);
            Chain.Gateway.DropIoc += new DropIocDelegate(GatewayDropIoc);
            Chain.Gateway.NewClientChannel += new NewClientChannelDelegate(GatewayNewClientChannel);
            Chain.Gateway.DropClient += new DropClientDelegate(GatewayDropClient);
            Chain.Gateway.UpdateSearch += SendSearch;

            PBCaGw.Services.DebugTraceListener.LogEntry += GatewayLogEntry;
            PBCaGw.Services.DebugTraceListener.TraceLevelChanged += new System.EventHandler(DebugTraceListenerTraceLevelChanged);
        }

        void ReceiveTcpData(System.IAsyncResult ar)
        {
            if (disposed)
                return;

            int n = 0;

            //Log.TraceEvent(TraceEventType.Information, Chain.ChainId, "Got TCP");

            try
            {
                SocketError err;
                n = forwarder.EndReceive(ar, out err);
                switch (err)
                {
                    case SocketError.Success:
                        break;
                    case SocketError.ConnectionReset:
                        Dispose();
                        return;
                    default:
                        Dispose();
                        return;
                }
            }
            catch (System.ObjectDisposedException)
            {
                Dispose();
                return;
            }
            catch (System.Exception ex)
            {
                Dispose();
                return;
            }

            // Time to quit!
            if (n == 0)
            {
                Dispose();
                return;
            }

            try
            {
                this.Chain.LastMessage = Gateway.Now;
                if (firstForward)
                    firstForward = false;
                else if (n > 0)
                    socket.Send(buffer, n, SocketFlags.None);
                forwarder.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (SocketException)
            {
                Dispose();
            }
            catch (System.ObjectDisposedException)
            {
                Dispose();
            }
            catch (Exception ex)
            {
                Dispose();
            }
        }


        void SendSearch(object sender, System.EventArgs e)
        {
            try
            {
                lock (sendStream)
                {
                    lock (Chain.Gateway.searchLock)
                    {
                        Send((int)DebugDataType.SEARCH_STATS);
                        Send(Chain.Gateway.searchStats.Count);
                        foreach (var i in Chain.Gateway.searchStats.OrderByDescending(row => row.Value.PreviousSearch))
                        {
                            Send(i.Key);
                            Send(i.Value.PreviousSearch);
                        }

                        Send((int)DebugDataType.SEARCHERS_STATS);
                        Send(Chain.Gateway.searchersStats.Count);
                        foreach (var i in Chain.Gateway.searchersStats.OrderByDescending(row => row.Value.PreviousSearch))
                        {
                            Send(i.Key);
                            Send(i.Value.PreviousSearch);
                        }

                    }
                    Flush();
                }
            }
            catch
            {
                Chain.Dispose();
            }
        }

        void DebugTraceListenerTraceLevelChanged(object sender, System.EventArgs e)
        {
            try
            {
                lock (sendStream)
                {
                    if (PBCaGw.Services.DebugTraceListener.TraceAll)
                        Send((int)DebugDataType.FULL_LOGS);
                    else
                        Send((int)DebugDataType.CRITICAL_LOGS);
                    Flush();
                }
            }
            catch
            {
                Chain.Dispose();
            }
        }

        void GatewayLogEntry(string source, System.Diagnostics.TraceEventType eventType, int chainId, string message)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.LOG);
                    Send(source);
                    Send((int)eventType);
                    Send(chainId);
                    Send(message);
                    Flush();
                }
            }
            catch
            {
                Chain.Dispose();
            }
        }

        void GatewayDropClient(string client)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.DROP_CLIENT);
                    Send(client);
                    Flush();
                }
            }
            catch
            {
                Chain.Dispose();
            }
        }

        void GatewayNewClientChannel(string client, string channel)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.CLIENT_NEW_CHANNEL);
                    Send(client);
                    Send(channel);
                    Flush();
                }
            }
            catch
            {
                Chain.Dispose();
            }
        }

        void Flush()
        {
            sendStream.Flush();
        }

        void GatewayDropIoc(string ioc)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.DROP_IOC);
                    Send(ioc);
                    Flush();
                }
            }
            catch
            {
                Chain.Dispose();
            }
        }

        void GatewayNewIocChannel(string ioc, string channel)
        {
            try
            {
                lock (sendStream)
                {
                    Send((int)DebugDataType.IOC_NEW_CHANNEL);
                    Send(ioc);
                    Send(channel);
                    Flush();
                }
            }
            catch
            {
                Chain.Dispose();
            }
        }

        void Send(int data)
        {
            byte[] buff = new byte[4];
            buff[0] = (byte)((data & 0xFF000000u) >> 24);
            buff[1] = (byte)((data & 0x00FF0000u) >> 16);
            buff[2] = (byte)((data & 0x0000FF00u) >> 8);
            buff[3] = (byte)(data & 0x000000FFu);
            Send(buff);
        }

        void Send(string data)
        {
            byte[] buff = System.Text.Encoding.UTF8.GetBytes(data);
            Send(buff.Length);
            Send(buff);
        }

        void Send(byte[] data)
        {
            sendStream.Write(data, 0, data.Length);
        }

        public int GetInt(DataPacket packet, int pos)
        {
            return (int)packet.GetUInt32(pos);
        }

        public string GetString(DataPacket packet, int pos)
        {
            int lenght = GetInt(packet, pos);
            byte[] data = new byte[lenght];
            System.Array.Copy(packet.Data, pos + 4, data, 0, lenght);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        public override void ProcessData(DataPacket packet)
        {
            int pos = 0;
            if (firstMessage)
            {
                firstMessage = false;
            }
            else if (forwarder != null)
                forwarder.Send(packet.Data, 0, packet.BufferSize, SocketFlags.None);
            else switch ((DebugDataType)packet.GetUInt32(pos))
                {
                    case DebugDataType.FULL_LOGS:
                        PBCaGw.Services.DebugTraceListener.TraceAll = true;
                        if (PBCaGw.Services.Log.WillDisplay(System.Diagnostics.TraceEventType.Information))
                            PBCaGw.Services.Log.TraceEvent(TraceEventType.Information, 0, "Debug set to show all messages.");
                        break;
                    case DebugDataType.CRITICAL_LOGS:
                        if (PBCaGw.Services.Log.WillDisplay(System.Diagnostics.TraceEventType.Information))
                            PBCaGw.Services.Log.TraceEvent(TraceEventType.Information, 0, "Debug set to show critical messages.");
                        PBCaGw.Services.DebugTraceListener.TraceAll = false;
                        break;
                }
        }

        public override void Dispose()
        {
            disposed = true;
            try
            {
                sendStream.Dispose();
            }
            catch
            {
            }

            if (forwarder != null)
            {
                try
                {
                    forwarder.Dispose();
                }
                catch
                {
                }
            }
            Chain.Gateway.NewIocChannel -= new NewIocChannelDelegate(GatewayNewIocChannel);
            Chain.Gateway.DropIoc -= new DropIocDelegate(GatewayDropIoc);
            Chain.Gateway.NewClientChannel -= new NewClientChannelDelegate(GatewayNewClientChannel);
            Chain.Gateway.DropClient -= new DropClientDelegate(GatewayDropClient);
            PBCaGw.Services.DebugTraceListener.LogEntry -= GatewayLogEntry;
            Chain.Gateway.UpdateSearch -= SendSearch;
        }
    }
}
