using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NameServer
{
    class TcpLink : IDisposable
    {
        public event EventHandler LostConnection;
        Socket socket;
        readonly byte[] buffer = new byte[NameServer.BUFFER_SIZE];
        bool disposed = false;
        public IPEndPoint EndPoint { get; private set; }
        NameServer nameServer;
        bool echoSent = false;
        DateTime lastEcho = DateTime.Now;
        static DataPacket echoPacket;
        List<EpicsChannel> registeredChannels = new List<EpicsChannel>();
        SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        uint cid = 1;

        static TcpLink()
        {
            echoPacket = DataPacket.Create(16);
            echoPacket.Command = (ushort)CommandID.CA_PROTO_ECHO;
            echoPacket.DataType = 0;
            echoPacket.DataCount = 0;
            echoPacket.Parameter1 = 0;
            echoPacket.Parameter2 = 0;
        }

        public TcpLink(IPEndPoint endPoint, NameServer nameServer)
        {
            this.EndPoint = endPoint;
            this.nameServer = nameServer;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
                    Dispose();
                }
            }
            catch (Exception ex)
            {
                Dispose();
            }
            Log.Write(System.Diagnostics.TraceEventType.Start, "Open TCP connection to " + endPoint);

            nameServer.TenSecJobs += nameServer_TenSecJobs;


            try
            {
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch
            {
                Dispose();
            }
        }

        void nameServer_TenSecJobs(object sender, EventArgs e)
        {
            if ((DateTime.Now - lastEcho).TotalSeconds > 30)
            {
                Log.Write(System.Diagnostics.TraceEventType.Verbose, "Echo sent to " + this.EndPoint);
                echoSent = true;
                Send(echoPacket);
            }
            /*if ((DateTime.Now - lastEcho).TotalSeconds > 40)
            {
                Log.Write(System.Diagnostics.TraceEventType.Error, "" + this.EndPoint + " doesn't answer to echo");
                Dispose();
            }*/
        }

        bool SocketConnect(Socket socket, EndPoint endPoint, int connTimeout)
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

        void ReceiveTcpData(IAsyncResult ar)
        {
            if (disposed)
                return;

            int n = 0;

            //Log.TraceEvent(TraceEventType.Information, Chain.ChainId, "Got TCP");

            try
            {
                SocketError err;
                n = socket.EndReceive(ar, out err);
                switch (err)
                {
                    case SocketError.Success:
                        break;
                    case SocketError.ConnectionReset:
                        Dispose();
                        return;
                    default:
                        Log.Write(System.Diagnostics.TraceEventType.Error, err.ToString());
                        Dispose();
                        return;
                }
            }
            catch (ObjectDisposedException)
            {
                Dispose();
                return;
            }
            catch (Exception ex)
            {
                Log.Write(System.Diagnostics.TraceEventType.Error, ex.Message);
                Dispose();
                return;
            }

            // Time to quit!
            if (n == 0)
            {
                Log.Write(System.Diagnostics.TraceEventType.Verbose, "Socket closed on the other side");
                Dispose();
                return;
            }

            try
            {
                // Handle message
                DataPacket packet = DataPacket.Create(buffer, n);
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
                SplitMessage(packet);
            }
            catch (SocketException)
            {
                Dispose();
            }
            catch (ObjectDisposedException)
            {
                Dispose();
            }
            catch (Exception ex)
            {
                Log.Write(System.Diagnostics.TraceEventType.Critical, "Error in TCPReceiver: " + ex.ToString() + "\r\n" + ex.StackTrace);
                Dispose();
            }
        }

        public void SplitMessage(DataPacket packet)
        {
            while (packet.Data.Length != 0)
            {
                // We don't even have a complete header, stop
                if (!packet.HasCompleteHeader)
                {
                    return;
                }
                // Full packet, send it.
                if (packet.MessageSize == packet.Data.Length)
                {
                    HandleMessage(packet);
                    return;
                }
                // More than one message in the packet, split and continue
                else if (packet.MessageSize < packet.Data.Length)
                {
                    DataPacket p = DataPacket.Create(packet, packet.MessageSize);
                    HandleMessage(p);
                    DataPacket newPacket = packet.SkipSize(packet.MessageSize);
                    packet.Dispose();
                    packet = newPacket;
                }
                // Message bigger than packet.
                // Cannot be the case on UDP!
                else
                {
                    return;
                }
            }
        }

        void Send(DataPacket packet)
        {
            try
            {
                socket.Send(packet.Data, 0, packet.Data.Length, SocketFlags.None);
            }
            catch
            {
                Dispose();
            }
        }

        void HandleMessage(DataPacket packet)
        {
            // Handles only Searches
            switch ((CommandID)packet.Command)
            {
                case CommandID.CA_PROTO_ECHO:
                    if (echoSent)
                    {
                        Log.Write(System.Diagnostics.TraceEventType.Verbose, "Echo message received back from " + this.EndPoint);
                        lastEcho = DateTime.Now;
                        echoSent = false;
                    }
                    else
                    {
                        lastEcho = DateTime.Now;
                        Log.Write(System.Diagnostics.TraceEventType.Verbose, "Echo message from " + this.EndPoint);
                        Send(packet);
                    }
                    break;
                case CommandID.CA_PROTO_SERVER_DISCONN:
                    locker.Wait();
                    try
                    {
                        foreach (var i in registeredChannels.Where(row => row.CID == packet.Parameter1))
                            i.CallBack();
                        registeredChannels.RemoveAll(row => row.CID == packet.Parameter1);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(System.Diagnostics.TraceEventType.Critical, ex.ToString());
                    }
                    finally
                    {
                        locker.Release();
                    }
                    break;
                default:
                    break;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool callEvent)
        {
            if (disposed)
                return;
            disposed = true;

            this.nameServer.TenSecJobs -= nameServer_TenSecJobs;

            Log.Write(System.Diagnostics.TraceEventType.Stop, "Closed connection to " + this.EndPoint);

            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Disconnect(false);
                socket.Close();
            }
            catch
            {

            }
            if (callEvent)
            {
                LostConnection(this, null);
                locker.Wait();
                try
                {
                    foreach (var i in registeredChannels)
                        i.CallBack();
                }
                catch (Exception ex)
                {
                    Log.Write(System.Diagnostics.TraceEventType.Critical, ex.ToString());
                }
                finally
                {
                    locker.Release();
                }
            }

        }

        internal void AddChannel(string channel, Action LostConnection)
        {
            locker.Wait();
            uint currentCid = cid++;
            try
            {
                registeredChannels.Add(new EpicsChannel { Name = channel, CallBack = LostConnection, CID = currentCid });
            }
            catch (Exception ex)
            {
                Log.Write(System.Diagnostics.TraceEventType.Critical, ex.ToString());
            }
            finally
            {
                locker.Release();
            }

            var createPacket = DataPacket.Create(16 + channel.Length + Padding(channel.Length));
            createPacket.Command = (ushort)CommandID.CA_PROTO_CREATE_CHAN;
            createPacket.DataType = 0;
            createPacket.DataCount = 0;
            createPacket.Parameter1 = currentCid;
            createPacket.Parameter2 = 11;
            createPacket.SetDataAsString(channel);
            Send(createPacket);
        }

        int Padding(int size)
        {
            if (size % 8 == 0)
                return 8;
            else
                return (8 - (size % 8));
        }
    }
}
