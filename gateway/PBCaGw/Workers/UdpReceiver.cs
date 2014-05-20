using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using PBCaGw.Services;
using System.Threading;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Receives data from an UDP connection
    /// </summary>
    public class UdpReceiver : ReceiverWorker
    {
        Socket udpSocket;
        readonly byte[] buff = new byte[Gateway.BUFFER_SIZE];
        readonly IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        bool disposed = false;
        DateTime lastReceived = Gateway.Now;

        /// <summary>
        /// Defines on which IPEndPoint the code shall monitor for UDP connections.
        /// Once defined the code will start the monitoring.
        /// </summary>
        public override IPEndPoint ClientEndPoint
        {
            get
            {
                return (IPEndPoint)udpSocket.LocalEndPoint;
            }
            set
            {
                EndPoint tempRemoteEp = sender;

                switch (this.Chain.Side)
                {
                    case ChainSide.SIDE_A:
                        udpSocket = Chain.Gateway.Configuration.UdpReceiverA;
                        break;
                    case ChainSide.SIDE_B:
                        udpSocket = Chain.Gateway.Configuration.UdpReceiverB;
                        break;
                    case ChainSide.UDP_RESP_SIDE_A:
                        udpSocket = Chain.Gateway.Configuration.UdpReceiverB;
                        break;
                    case ChainSide.UDP_RESP_SIDE_B:
                        udpSocket = Chain.Gateway.Configuration.UdpReceiverA;
                        break;
                }

                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Start))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Start, this.Chain.ChainId, "UDP Listener " + this.Chain.Side + " on " + udpSocket.LocalEndPoint);
                udpSocket.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref tempRemoteEp, GotUdpMessage, tempRemoteEp);

                /*if(!System.Diagnostics.Debugger.IsAttached)
                    Gateway.OneSecJobs += CheckUdpIsReceiving;*/
                //Gateway.OneSecJobs += CheckUdpIsReceiving;
            }
        }

        bool rebuilding = false;
        void CheckUdpIsReceiving(object objSender, EventArgs e)
        {
            double diff = (Gateway.Now - lastReceived).TotalSeconds;
            if (diff > 10)
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                    Log.TraceEvent(TraceEventType.Critical, Chain.ChainId, "UDP Not responsive, let's try to rebuild it.");

                if(!rebuilding)
                    Rebuild();
            }
            else if (diff > 1)
            {
                //Log.TraceEvent(TraceEventType.Verbose, Chain.ChainId, "Send watchdog packet.");
                try
                {
                    udpSocket.SendTo(new byte[] { 0, 0, 0, 0 }, udpSocket.LocalEndPoint);
                }
                catch
                {
                }
            }
        }

        void Rebuild()
        {
            rebuilding = true;
            try
            {
                udpSocket.Close();
            }
            catch
            {
            }
            try
            {
                udpSocket.Dispose();
            }
            catch
            {
            }

            if (Chain.IsDisposed || this.disposed)
            {
                rebuilding = false;
                return;
            }

            Thread.Sleep(1000);

            try
            {
                switch (this.Chain.Side)
                {
                    case ChainSide.SIDE_A:
                        Chain.Gateway.Configuration.UdpReceiverA = null;
                        udpSocket = Chain.Gateway.Configuration.UdpReceiverA;
                        break;
                    case ChainSide.SIDE_B:
                        Chain.Gateway.Configuration.UdpReceiverB = null;
                        udpSocket = Chain.Gateway.Configuration.UdpReceiverB;
                        break;
                    case ChainSide.UDP_RESP_SIDE_A:
                        Chain.Gateway.Configuration.UdpReceiverB = null;
                        udpSocket = Chain.Gateway.Configuration.UdpReceiverB;
                        break;
                    case ChainSide.UDP_RESP_SIDE_B:
                        Chain.Gateway.Configuration.UdpReceiverA = null;
                        udpSocket = Chain.Gateway.Configuration.UdpReceiverA;
                        break;
                }

                EndPoint tempRemoteEp = sender;
                Debug.Assert(udpSocket != null, "udpSocket != null");
                udpSocket.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref tempRemoteEp, GotUdpMessage, tempRemoteEp);
                if(Log.WillDisplay(TraceEventType.Start))
                    Log.TraceEvent(TraceEventType.Start, Chain.ChainId, "UDP Rebuilt");
            }
            catch
            {
            }

            lastReceived = Gateway.Now;
            rebuilding = false;
        }

        void GotUdpMessage(IAsyncResult ar)
        {
            if (disposed || rebuilding)
                return;

            IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint epSender = ipeSender;
            int size = 0;

            try
            {
                size = udpSocket.EndReceiveFrom(ar, ref epSender);
            }
            catch (ObjectDisposedException)
            {
                // Stop receiving
                this.Chain.Dispose();
                return;
            }
            catch (Exception ex)
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, this.Chain.ChainId, ex.Message);
                Rebuild();
                return;
            }

            // Watchdog packet
            if (epSender.ToString() == udpSocket.LocalEndPoint.ToString())
            {
                lastReceived = Gateway.Now;
                udpSocket.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref epSender, GotUdpMessage, epSender);
                return;
            }

            // Get the data back
            //DataPacket packet = DataPacket.Create(buff, size, this.Chain);
            DataPacket packet = DataPacket.Create(buff, size, this.Chain,true);
            packet.Sender = (IPEndPoint)epSender;

            lastReceived = Gateway.Now;
            if(this.Chain[1] != null)
                ((PacketSplitter)this.Chain[1]).Reset();
            try
            {
                this.SendData(packet);
            }
            catch (Exception ex)
            {
                /*if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();*/
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, Chain.ChainId, "Error in UDPReceiver: " + ex + "\r\n" + ex.StackTrace);
            }

            // Start Accepting again
            try
            {
                udpSocket.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref epSender, GotUdpMessage, epSender);
            }
            catch (ObjectDisposedException)
            {
                if (!disposed)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                Rebuild();
            }
        }

        public override void Dispose()
        {
            if (disposed)
                return;
            if (!System.Diagnostics.Debugger.IsAttached)
                Gateway.OneSecJobs -= CheckUdpIsReceiving;

            disposed = true;
            try
            {
                udpSocket.Close();
            }
            catch
            {
            }
        }
    }
}
