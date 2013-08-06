using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using PBCaGw.Workers;
using System.Diagnostics;

namespace PBCaGw.Services
{
    /// <summary>
    /// Allows to store data of multiple messages to build back a packet
    /// </summary>
    class DataQueue
    {
        /// <summary>
        /// When was this queue created
        /// </summary>
        public Stopwatch CreationTime { get; private set; }
        /// <summary>
        /// Buffer containing the data
        /// </summary>
        byte[] buff = new byte[0];
        /// <summary>
        /// Destination of the packet
        /// </summary>
        public IPEndPoint Destination { get; private set; }
        /// <summary>
        /// Link to the sending chain
        /// </summary>
        public WorkerChain Chain { get; private set; }
        /// <summary>
        /// Must the answer be reversed (sent back to the sender)
        /// </summary>
        bool ReverseAnswer = false;

        /// <summary>
        /// Creates a new DataQueue based on a packet message
        /// </summary>
        /// <param name="packet"></param>
        public DataQueue(DataPacket packet)
        {
            CreationTime = new Stopwatch();
            CreationTime.Start();
            buff = new byte[packet.BufferSize];
            Array.Copy(packet.Data, packet.Offset, buff, 0, buff.Length);
            Destination = packet.Destination;
            Chain = packet.Chain;
            ReverseAnswer = packet.ReverseAnswer;
        }

        /// <summary>
        /// Adds the data of a packet to the queue
        /// </summary>
        /// <param name="packet"></param>
        public void Enqueue(DataPacket packet)
        {
            int oldSize = buff.Length;
            Array.Resize(ref buff, buff.Length + packet.BufferSize);
            Buffer.BlockCopy(packet.Data, packet.Offset, buff, oldSize, packet.BufferSize);
        }

        /// <summary>
        /// Retreives the length of the buffer
        /// </summary>
        public int Length
        {
            get
            {
                return buff.Length;
            }
        }

        /// <summary>
        /// Build a packet out of the buffer
        /// </summary>
        public DataPacket Packet
        {
            get
            {
                DataPacket p = DataPacket.Create(buff, buff.Length, Chain);
                p.ReverseAnswer = ReverseAnswer;
                p.Destination = Destination;
                return p;
            }
        }
    }
}
