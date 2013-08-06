using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Net;

namespace PBCaGw.Services
{
    public delegate void DataPacketNotification(object sender, DataPacket packet);

    /// <summary>
    /// Stores data as key/value pair and have a time stamp of the creation
    /// </summary>
    public class Record
    {
        DateTime createdOn = Gateway.Now;
        public DateTime CreatedOn { get { return createdOn; } }

        public bool? knownFromSideA;
        public bool? knownFromSideB;
        public IPEndPoint Destination;
        public uint? IOID;
        public uint? GWCID;
        public uint? SID;
        public uint? CID;
        public string Channel;
        public IPEndPoint Client;
        public IPEndPoint Server;
        public uint? SubscriptionId;
        public UInt16? DBRType;
        public uint? DataCount;
        public PBCaGw.Workers.ChainSide ChainSide;
        public PBCaGw.Configurations.SecurityAccess AccessRight;
        public ConcurrentBag<UInt32> SubscriptionList;
        //public ConcurrentQueue<DateTime> LastMessages;
        /// <summary>
        /// defines if this is the first result of the monitor or not
        /// </summary>
        public bool FirstValue { get; set; }

        public event DataPacketNotification GetNotification;
        public int PacketCount = 0;

        public void Notify(object sender, DataPacket packet)
        {
            if (GetNotification != null)
                GetNotification(sender, packet);
            GetNotification = null;
        }

        //public DataPacket FirstPacket { get; set; }
    }
}
