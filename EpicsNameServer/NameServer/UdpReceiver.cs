using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NameServer
{
    class UdpReceiver
    {
        int port;
        Socket udpSocket;
        readonly byte[] buff = new byte[NameServer.BUFFER_SIZE];
        readonly IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        bool disposed = false;
        IPAddress address;

        // Found on http://stackoverflow.com/questions/5199026/c-sharp-async-udp-listener-socketexception
        // Allows to reset the socket in case of malformed UDP packet.
        const int SioUdpConnReset = -1744830452;
        NameServer nameServer;

        public UdpReceiver(NameServer nameServer, IPAddress address, int port)
        {
            this.nameServer = nameServer;
            this.address = address;
            this.port = port;
        }

        internal void Start()
        {
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpSocket.IOControl(SioUdpConnReset, new byte[] { 0, 0, 0, 0 }, null);
            udpSocket.Bind(new IPEndPoint(this.address, this.port));


            EndPoint tempRemoteEp = sender;
            udpSocket.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref tempRemoteEp, GotUdpMessage, tempRemoteEp);
        }

        void GotUdpMessage(IAsyncResult ar)
        {
            if (disposed)
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
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

            // Store the message
            DataPacket packet = DataPacket.Create(buff, size);
            packet.Sender = (IPEndPoint)epSender;

            // Start listening
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
                Console.WriteLine(ex.ToString());
            }

            // Avoid ourself
            if (packet.Sender.Address.ToString() == this.address.ToString() && packet.Sender.Port == this.port)
                return;

            // Handle the buffer
            SplitMessage(packet);
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

        void HandleMessage(DataPacket packet)
        {
            // Handles only Searches
            switch ((CommandID)packet.Command)
            {
                case CommandID.CA_PROTO_SEARCH:
                    // Answer packet
                    if (packet.PayloadSize == 8)
                    {
                        Log.Write(System.Diagnostics.TraceEventType.Verbose, "Search packet answer");
                        NameEntry record = nameServer.IdCache[packet.Parameter2];
                        if (record != null)
                            record.GotAnswer(packet);
                    }
                    // Search request
                    else
                    {
                        DebugServer.NbSearches++;

                        string channel = packet.GetDataAsString();
                        Log.Write(System.Diagnostics.TraceEventType.Verbose, "Search packet request for " + channel);
                        // We can't use the record name (without property) as gateways will have issues for non-existent properties
                        //string name = channel.Split(new char[] { '.' })[0];
                        //name.GetHashCode() % nbNodes;
                        //NameEntry record = nameServer.Cache[name];
                        NameEntry record = nameServer.Cache[channel];
                        record.AnswerTo(packet.Sender, packet.Parameter1);
                    }
                    break;
                default:
                    break;
            }
        }

        internal void Send(DataPacket newPacket)
        {
            udpSocket.SendTo(newPacket.Data, newPacket.Destination);
        }

        internal void Stop()
        {
            udpSocket.Close();
            udpSocket.Dispose();
        }
    }
}
