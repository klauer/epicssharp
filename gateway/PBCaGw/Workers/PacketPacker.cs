using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using PBCaGw.Services;
using System.Threading;
using System.Collections;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Packs packets (UDP) together.
    /// </summary>
    public class PacketPacker : PacketWorker
    {
        Dictionary<IPEndPoint, DataQueue> storage = new Dictionary<IPEndPoint, DataQueue>();
        Thread packerThread;

        public PacketPacker()
        {
            packerThread = new Thread(new ThreadStart(Flush));
            packerThread.IsBackground = true;
            packerThread.Start();
        }

        /// <summary>
        /// Runs every 5 millisecs. and checks destinations which need to be sent to.
        /// </summary>
        void Flush()
        {
            while (true)
            {
                Thread.Sleep(5);
                lock (storage)
                {
                    // Retrieve all the "old" buffers
                    //var list = storage.Where(row => row.Value.CreationTime.ElapsedMilliseconds > 5).ToList();
                    var list = storage.ToList();
                    foreach (var i in list)
                    {
                        DataPacket packet = i.Value.Packet;
                        /*if (Log.WillDisplay(System.Diagnostics.TraceEventType.Information))
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Information, Chain.ChainId, "Packet " + packet.BufferSize);*/
                        SendData(packet);
                        storage.Remove(i.Key);
                    }
                }
            }
        }

        public override void ProcessData(DataPacket packet)
        {

            if (packet.Command != 6)
            {
                return;
            }

            if (packet.Destination == null)
                return;
            lock (storage)
            {
                // Checks if we have a storage for this destination
                if (storage.ContainsKey(packet.Destination))
                {
                    // Buffer is too full, let's send it and start a new one
                    if (storage[packet.Destination].Length + packet.BufferSize > Gateway.MAX_UDP_SEND_PACKET)
                    {
                        SendData(storage[packet.Destination].Packet);
                        storage[packet.Destination] = new DataQueue(packet);
                    }
                    else
                        // add packet to the end of the buffer
                        storage[packet.Destination].Enqueue(packet);
                }
                // None, let's create one
                else
                    storage.Add(packet.Destination, new DataQueue(packet));
            }
            //packet.Dispose();
        }
    }
}
