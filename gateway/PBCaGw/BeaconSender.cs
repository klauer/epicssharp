using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace PBCaGw
{
    public interface IBeaconResetter : IDisposable
    {
        void ResetBeacon();
    }

    public class BeaconSender : IDisposable, IBeaconResetter
    {
        //Socket socket;
        readonly List<IPEndPoint> destinations;
        readonly Socket socket;
        readonly Thread thread;

        int loopInterval = 30;
        private const int MaxLoopInterval = 15000;
        int loops = 0;
        uint counter = 0;
        bool disposed = false;

        readonly DataPacket packet = DataPacket.Create(0, null);

        public BeaconSender(Socket socket, IPEndPoint local, IEnumerable<IPEndPoint> destinations)
        {
            packet.Command = 13;
            packet.DataType = (ushort)local.Port;
            packet.DataCount = 0;
            packet.SetBytes(8, local.Address.GetAddressBytes());

            this.destinations = destinations.Select(dest => new IPEndPoint(dest.Address, dest.Port + 1)).ToList();
            this.socket = socket;

            thread = new Thread(new ThreadStart(SendBeacons));
            thread.IsBackground = true;
            thread.Start();
        }

        void SendBeacons()
        {
            while (!disposed)
            {
                if (10 * loops >= loopInterval)
                {
                    loops = 0;
                    if (loopInterval < MaxLoopInterval)
                        loopInterval *= 2;

                    packet.Parameter1 = counter++;
                    foreach (IPEndPoint destination in destinations)
                    {
                        try
                        {
                            socket.SendTo(packet.Data, destination);
                        }
                        catch
                        {
                        }
                    }
                }
                else
                    loops++;

                Thread.Sleep(10);
            }
        }

        public void ResetBeacon()
        {
            loopInterval = 30;
            loops = 0;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
        }
    }
}
