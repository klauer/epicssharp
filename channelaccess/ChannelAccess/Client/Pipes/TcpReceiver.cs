/*
 *  EpicsSharp - An EPICS Channel Access library for the .NET platform.
 *
 *  Copyright (C) 2013 - 2015  Paul Scherrer Institute, Switzerland
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using EpicsSharp.ChannelAccess.Constants;

namespace EpicsSharp.ChannelAccess.Client.Pipes
{
    internal class TcpReceiver : DataFilter, IDisposable
    {
        Socket socket;
        bool disposed = false;
        byte[] buffer = new byte[8192 * 3];

        internal Dictionary<string, uint> ChannelSID = new Dictionary<string, uint>();
        internal Dictionary<uint, Channel> PendingIo = new Dictionary<uint, Channel>();
        internal List<Channel> ConnectedChannels = new List<Channel>();
        private IPEndPoint destination;

        static readonly DataPacket echoPacket;

        public IPEndPoint Destination
        {
            get
            {
                return destination;
            }
        }

        static TcpReceiver()
        {
            echoPacket = DataPacket.Create(16);
            echoPacket.Command = (ushort)CommandID.CA_PROTO_ECHO;
            echoPacket.DataType = 0;
            echoPacket.DataCount = 0;
            echoPacket.Parameter1 = 0;
            echoPacket.Parameter2 = 0;
        }

        public void Start(IPEndPoint dest)
        {
            this.destination = dest;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            socket.Connect(dest);
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);

            DataPacket p = DataPacket.Create(16);
            p.Command = (ushort)CommandID.CA_PROTO_VERSION;
            p.DataType = 1;
            p.DataCount = (uint)CAConstants.CA_MINOR_PROTOCOL_REVISION;
            p.Parameter1 = 0;
            p.Parameter2 = 0;
            Send(p);

            p = DataPacket.Create(16 + this.Client.Configuration.Hostname.Length + TypeHandling.Padding(this.Client.Configuration.Hostname.Length));
            p.Command = (ushort)CommandID.CA_PROTO_HOST_NAME;
            p.DataCount = 0;
            p.DataType = 0;
            p.Parameter1 = 0;
            p.Parameter2 = 0;
            p.SetDataAsString(this.Client.Configuration.Hostname);
            Send(p);

            p = DataPacket.Create(16 + this.Client.Configuration.Username.Length + TypeHandling.Padding(this.Client.Configuration.Username.Length));
            p.Command = (ushort)CommandID.CA_PROTO_CLIENT_NAME;
            p.DataCount = 0;
            p.DataType = 0;
            p.Parameter1 = 0;
            p.Parameter2 = 0;
            p.SetDataAsString(this.Client.Configuration.Username);
            Send(p);
        }

        void ReceiveTcpData(IAsyncResult ar)
        {
            int n = 0;
            try
            {
                if (socket.Connected)
                    n = socket.EndReceive(ar);
                else
                {
                    Dispose();
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                Dispose();
                return;
            }
            catch
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
                Pipe.LastMessage = DateTime.Now;
                if (n > 0)
                {
                    DataPacket p = DataPacket.Create(buffer, n);
                    p.Sender = (IPEndPoint)socket.RemoteEndPoint;
                    this.SendData(p);
                }
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveTcpData, null);
            }
            catch (ObjectDisposedException)
            {
                Dispose();
            }
            catch (SocketException)
            {
                Dispose();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
                Dispose();
            }
        }

        internal void AddChannel(Channel channel)
        {
            lock (ConnectedChannels)
            {
                if (!ConnectedChannels.Contains(channel))
                    ConnectedChannels.Add(channel);
            }
        }

        internal void RemoveChannel(Channel channel)
        {
            lock (ConnectedChannels)
            {
                ConnectedChannels.Remove(channel);
            }

            lock (Client.Channels)
            {
                if (!Client.Channels.Any(row => row.Value.ChannelName == channel.ChannelName))
                    ChannelSID.Remove(channel.ChannelName);
            }
        }

        internal void Send(DataPacket packet)
        {
            if (disposed)
                return;
            try
            {
                socket.Send(packet.Data);
                //Pipe.LastMessage = DateTime.Now;
            }
            catch
            {
                Dispose();
            }
        }

        public override void ProcessData(DataPacket packet)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            if (disposed)
                return;
            lock (Client.Iocs)
                Client.Iocs.Remove(destination);
            List<Channel> toDisconnect;
            lock (ConnectedChannels)
            {
                toDisconnect = ConnectedChannels.ToList();
            }
            foreach (Channel channel in toDisconnect)
                channel.Disconnect();

            try
            {
                socket.Disconnect(false);
            }
            catch
            {
            }
            try
            {
                socket.Dispose();
            }
            catch
            {
            }
            disposed = true;
        }

        internal void Echo()
        {
            Pipe.GeneratedEcho = true;
            Send(echoPacket);
        }
    }
}
