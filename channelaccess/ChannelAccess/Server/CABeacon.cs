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
using System.Threading;
using System.IO;
using EpicsSharp.ChannelAccess.Constants;

namespace EpicsSharp.ChannelAccess.Server
{
    internal class CABeacon : IDisposable
    {
        bool running = true;
        List<IPAddress> serverIps = new List<IPAddress>();
        IPEndPoint endPoint;
        Thread runningThread;
        int udpPort = 5064;

        public CABeacon(CAServer server, int udpPort, int beaconPort)
        {
            this.udpPort = 5064;
            endPoint = new IPEndPoint(IPAddress.Broadcast, beaconPort);
            if (server.ServerAddress == IPAddress.Any)
                serverIps.AddRange(Dns.GetHostAddresses(Dns.GetHostName()).Where(row => !row.IsIPv6LinkLocal && !row.IsIPv6Multicast && !row.IsIPv6SiteLocal && row.AddressFamily == AddressFamily.InterNetwork));
            else
                serverIps.Add(server.ServerAddress);

            runningThread = new Thread(new ThreadStart(Do));
            runningThread.IsBackground = true;
            runningThread.Start();
        }

        void Do()
        {
            int loopInterval = 30;
            int maxLoopInterval = 15000;

            int loops = 0;
            int counter = 0;
            UdpClient udp = new UdpClient();

            while (running)
            {
                if (10 * loops >= loopInterval)
                {
                    loops = 0;
                    if (loopInterval < maxLoopInterval)
                        loopInterval *= 2;
                    foreach (var i in serverIps)
                    {
                        byte[] buff = beaconMessage(this.udpPort, (counter++), i.GetAddressBytes());
                        udp.Send(buff, buff.Length, endPoint);
                    }
                }
                else
                    loops++;

                Thread.Sleep(10);
            }
            udp.Close();
        }

        byte[] beaconMessage(int port, int sequenceNumber, byte[] ip)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(mem))
                {
                    writer.Write(((short)CommandID.CA_PROTO_RSRV_IS_UP).ToByteArray());
                    writer.Write(new byte[2]);
                    writer.Write(((UInt16)port).ToByteArray());
                    writer.Write(new byte[2]);
                    writer.Write(((UInt32)sequenceNumber).ToByteArray());
                    writer.Write(ip);

                    return mem.ToArray();
                }
            }
        }

        public void Dispose()
        {
            running = false;
        }
    }
}
