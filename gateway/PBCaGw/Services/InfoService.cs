using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Concurrent;
using System.Threading;

namespace PBCaGw.Services
{
    /// <summary>
    /// Cache class to store temporary data.
    /// </summary>
    public static class InfoService
    {
        static InfoService()
        {
            SearchChannel = new SearchChannel();
            ChannelEndPoint = new StorageService<string>();
            SearchChannelEndPointA = new AutoCleaningStorageService<string> { Lifetime = 20};
            SearchChannelEndPointB = new AutoCleaningStorageService<string> { Lifetime = 20 };

            //SearchChannelEndPoint = new StorageService<string>();

            ChannelCid = new StorageService<UInt32>();
            IOID = new AutoCleaningStorageService<UInt32> { Lifetime = 10};
            ChannelSubscription = new StorageService<UInt32>();
            EchoSent = new AutoCleaningStorageService<IPEndPoint>();
            SubscribedChannel = new StorageService<string>();
        }

        /// <summary>
        /// Used during the search channel. Automatically cleaned up.
        /// </summary>
        public static SearchChannel SearchChannel { get; private set; }

        /// <summary>
        /// Stores which IOC serves a given channel (for the search on side A).
        /// </summary>
        public static StorageService<string> SearchChannelEndPointA { get; private set; }

        /// <summary>
        /// Stores which IOC serves a given channel (for the search on side B).
        /// </summary>
        public static StorageService<string> SearchChannelEndPointB { get; private set; }

        /// <summary>
        /// Stores info about a given channel.
        /// </summary>
        public static StorageService<string> ChannelEndPoint { get; private set; }

        /// <summary>
        /// Stores a channel id
        /// </summary>
        public static StorageService<UInt32> ChannelCid { get; private set; }

        /// <summary>
        /// Stores an IO ID (for get)
        /// </summary>
        public static AutoCleaningStorageService<UInt32> IOID { get; private set; }

        /// <summary>
        /// Stores a subscrition info for the CA_PROTO_EVENT_ADD
        /// </summary>
        public static StorageService<UInt32> ChannelSubscription { get; private set; }

        /// <summary>
        /// Stores all the ECHO sent and avoid to answer to them.
        /// </summary>
        public static AutoCleaningStorageService<IPEndPoint> EchoSent { get; private set; }

        /// <summary>
        /// Stores all the monitors open.
        /// </summary>
        public static StorageService<string> SubscribedChannel { get; private set; }
    }
}
