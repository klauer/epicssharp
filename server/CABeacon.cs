using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using CaSharpServer.Constants;

namespace CaSharpServer
{
    internal class CABeacon : IDisposable
    {
        bool running = true;
        List<IPAddress> serverIps = new List<IPAddress>();
        IPEndPoint endPoint;
        Thread runningThread;

        public CABeacon(CAServer server, int udpPort)
        {
            endPoint = new IPEndPoint(IPAddress.Broadcast, udpPort);
            if (server.ServerAddress == IPAddress.Any)
                serverIps.AddRange(Dns.GetHostAddresses(Dns.GetHostName()).Where(row=>!row.IsIPv6LinkLocal && !row.IsIPv6Multicast && !row.IsIPv6SiteLocal));
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
                    foreach(var i in serverIps)
                    {
                        byte[] buff=beaconMessage(endPoint.Port,(counter++),i.GetAddressBytes());
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
