using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using PBCaGw.Services;
using System.IO;
using System.Threading;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Receives data from the TCP connection
    /// </summary>
    class TcpReceiver : ReceiverWorker
    {
        readonly byte[] buffer = new byte[Gateway.BUFFER_SIZE];
        bool disposed = false;

        NetworkStream netStream;
        BufferedStream stream;
        bool isDirty = false;
        //AutoResetEvent dataSent = new AutoResetEvent(true);

        public IPEndPoint RemoteEndPoint
        {
            get;
            private set;
        }

        Socket socket;
        /// <summary>
        /// Defines the socket linked on the receiver.
        /// Once defined, the code will start to monitor the TCP socket for incoming data.
        /// </summary>
        public Socket Socket
        {
            get
            {
                return socket;
            }
            set
            {
                socket = value;
                socket.SendTimeout = 30000;
                /*socket.SendTimeout = 500;
                socket.Blocking = false;*/

                RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;

                if (Gateway.BufferedSockets)
                {
                    netStream = new NetworkStream(socket);
                    netStream.WriteTimeout = 500;
                    stream = new BufferedStream(netStream);
                }
                try
                {
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
                }
                catch
                {
                    Chain.Dispose();
                }
            }
        }

        public bool IsDirty { get { return isDirty; } }


        public void Send(DataPacket packet)
        {
            if (Gateway.BufferedSockets)
            {
                lock (stream)
                {
                    stream.Write(packet.Data, packet.Offset, packet.BufferSize);
                    isDirty = true;
                }
            }
            else
            {
                try
                {
                    /*if (!dataSent.WaitOne(1000))
                    {
                        Dispose();
                        return;
                    }
                    socket.BeginSend(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None, Sent, this);*/
                    socket.Send(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None);
                }
                catch
                {
                    this.Dispose();
                }
            }
        }

        /*void Sent(IAsyncResult obj)
        {
            try
            {
                socket.EndSend(obj);
                dataSent.Set();
            }
            catch
            {
                Dispose();
            }
        }*/

        public void Flush()
        {
            if (Gateway.BufferedSockets)
            {
                lock (stream)
                {
                    try
                    {
                        stream.Flush();
                    }
                    catch
                    {
                        this.Dispose();
                    }
                    isDirty = false;
                }
            }
        }

        /// <summary>
        /// Got data from the TCP connection
        /// </summary>
        /// <param name="ar"></param>
        void ReceiveTcpData(IAsyncResult ar)
        {
            if (disposed)
                return;

            int n = 0;

            //Log.TraceEvent(TraceEventType.Information, Chain.ChainId, "Got TCP");

            try
            {
                SocketError err;
                n = Socket.EndReceive(ar, out err);
                switch (err)
                {
                    case SocketError.Success:
                        break;
                    case SocketError.ConnectionReset:
                        Dispose();
                        return;
                    default:
                        if (Log.WillDisplay(System.Diagnostics.TraceEventType.Error))
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Error, Chain.ChainId, err.ToString());
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
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Error))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Error, Chain.ChainId, ex.Message);
                Dispose();
                return;
            }

            // Time to quit!
            if (n == 0)
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, Chain.ChainId, "Socket closed on the other side");
                Dispose();
                return;
            }

            try
            {
                this.Chain.LastMessage = Gateway.Now;
                if (n > 0)
                {
                    DataPacket p = DataPacket.Create(buffer, n, this.Chain, true);
                    p.Sender = (IPEndPoint)socket.RemoteEndPoint;
                    this.SendData(p);
                }
                Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
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
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, Chain.ChainId, "Error in TCPReceiver: " + ex.ToString() + "\r\n" + ex.StackTrace);
                Dispose();
            }
        }

        public override void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            IPEndPoint endPoint = null;
            try
            {
                endPoint = (IPEndPoint)socket.RemoteEndPoint;
            }
            catch (Exception)
            {
            }

            if (Gateway.BufferedSockets)
            {
                try
                {
                    stream.Dispose();
                }
                catch
                {
                }

                try
                {
                    netStream.Dispose();
                }
                catch
                {
                }
            }

            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Disconnect(false);
                //Socket.Disconnect(true);
                Socket.Close();
            }
            catch
            {
            }

            if (endPoint != null)
            {
                if (Chain.Side == ChainSide.SERVER_CONN)
                    PBCaGw.Services.TcpManager.DropServerConnection(this.Chain);
                else
                    PBCaGw.Services.TcpManager.DropClientConnection(endPoint);
            }
        }
    }
}
