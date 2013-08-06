using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Security
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint udpPort = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);

            Socket udpSocket = new Socket(udpPort.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            udpSocket.Bind(udpPort);
            Console.WriteLine(udpSocket.LocalEndPoint);
            Console.ReadKey();
        }
    }
}
