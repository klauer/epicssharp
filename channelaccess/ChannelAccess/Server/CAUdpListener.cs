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

namespace EpicsSharp.ChannelAccess.Server
{
    /// <summary>
    /// Answers to the search requests
    /// </summary>
    internal class CAUdpListener : IDisposable
    {
        Socket UDPSocket;
        byte[] buff = new byte[300000];
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        CAServerFilter filter;
        bool running = true;

        /// <summary>
        /// Bind to the
        /// </summary>
        /// <param name="port"></param>
        public CAUdpListener(IPAddress serverAddress, int port, CAServerFilter filter)
        {
            this.filter = filter;

            UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            UDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            UDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            UDPSocket.Bind(new IPEndPoint(serverAddress, port));
            EndPoint tempRemoteEP = (EndPoint)sender;
            UDPSocket.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref tempRemoteEP, GotUdpMessage, tempRemoteEP);
        }

        void GotUdpMessage(IAsyncResult ar)
        {
            IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint epSender = (EndPoint)ipeSender;
            int size = 0;

            try

            {
                size = UDPSocket.EndReceiveFrom(ar, ref epSender);
            }
            catch(Exception ex)
            {
                if (running)
                    throw ex;
                return;
            }

            string senderAddress = sender.Address.ToString();
            int senderPort = sender.Port;

            // Get the data back
            /*byte[] data = new byte[buff.Length];
            buff.CopyTo(data, 0);*/
            Pipe pipe = new Pipe();
            //pipe.Write(data, 0, size);
            pipe.Write(buff, 0, size);
            filter.ProcessReceivedData(pipe, epSender, size, false);

            // Start Accepting again
            UDPSocket.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref epSender, GotUdpMessage, epSender);
        }

        internal void Send(byte[] data, IPEndPoint receiver)
        {
            lock (UDPSocket)
            {
                UDPSocket.SendTo(data, receiver);
            }
        }

        public void Dispose()
        {
            if (running == false)
                return;
            running = false;
            UDPSocket.Shutdown(SocketShutdown.Both);
            UDPSocket.Close();
        }
    }
}
