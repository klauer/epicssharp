using System;
using System.Diagnostics;
using System.Linq;
using PBCaGw.Services;
using PBCaGw.Workers;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 1 (0x01) CA_PROTO_EVENT_ADD
    /// </summary>
    class EventAdd : CommandHandler
    {
        public static object lockObject = new object();

        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            DataPacket newPacket = null;

            if (packet.DataCount == 0)
            {
                if (Log.WillDisplay(TraceEventType.Error))
                    Log.TraceEvent(TraceEventType.Error, chain.ChainId, "Event add with datacount == 0!");
                packet.DataCount = 1;
            }
            lock (lockObject)
            {
                Record record = InfoService.ChannelCid[packet.Parameter1];
                // Lost the CID...
                if (record == null)
                {
                    if (Log.WillDisplay(TraceEventType.Error))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "EventAdd not linked to a correct channel");
                    //packet.Chain.Dispose();
                    System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                    return;
                }

                if (record.SID == null)
                {
                    if (Log.WillDisplay(TraceEventType.Error))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "EventAdd SID null");
                    //packet.Chain.Dispose();
                    System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                    return;
                }
                uint recordSID = record.SID.Value;

                if (Log.WillDisplay(TraceEventType.Information))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Information, chain.ChainId, "Add event for " + record.Channel);

                // Not enough info
                if (packet.MessageSize < 12 + 2 + packet.HeaderSize)
                {
                    if (Log.WillDisplay(TraceEventType.Error))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "Packet too small");
                    //packet.Chain.Dispose();
                    System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                    return;
                }



                string channelName = record.Channel;
                //string recId = record.Channel + "°" + packet.DataType + "°" + packet.DataCount + "°" + packet.GetUInt16(12 + (int)packet.HeaderSize);
                string recId = record.Channel + "°" + recordSID + "°" + packet.DataType + "°" + packet.DataCount + "°" + packet.GetUInt16(12 + (int)packet.HeaderSize);
                //Console.WriteLine(recId);


                //if (InfoService.SubscribedChannel.Knows(recId) && (InfoService.SubscribedChannel[recId].SID != record.SID || InfoService.SubscribedChannel[recId].GWCID != record.GWCID))
                if (InfoService.SubscribedChannel.Knows(recId) && (InfoService.SubscribedChannel[recId].SID != record.SID))
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                    return;
                }

                //recId = ""+CidGenerator.Next();

                // Client subscription
                UInt32 gwcid = CidGenerator.Next();
                Record currentMonitor = InfoService.ChannelSubscription.Create(gwcid);
                currentMonitor.Destination = record.Destination;
                currentMonitor.DBRType = packet.DataType;
                currentMonitor.DataCount = packet.DataCount;
                currentMonitor.Client = packet.Sender;
                currentMonitor.SubscriptionId = packet.Parameter2;
                currentMonitor.SID = recordSID;
                currentMonitor.Channel = recId;
                currentMonitor.FirstValue = false;
                currentMonitor.Destination = record.Destination;
                Record newMonitor = currentMonitor;

                chain.Subscriptions[packet.Parameter2] = gwcid;

                // A new monitor
                // Create a new subscription for the main channel
                // And create a list of subscriptions

                /*if (InfoService.SubscribedChannel.Knows(recId) && (InfoService.SubscribedChannel[recId].SID != record.SID || InfoService.SubscribedChannel[recId].GWCID != record.GWCID))
                {
                    CidGenerator.ReleaseCid(gwcid);
                    InfoService.ChannelSubscription.Remove(gwcid);
                    System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                    return;
                }
                else*/ if (!InfoService.SubscribedChannel.Knows(recId))
                //if (!InfoService.SubscribedChannel.Knows(recId) || InfoService.SubscribedChannel[recId].SID != record.SID)
                //if (true)
                {
                    if (Log.WillDisplay(TraceEventType.Information))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Information, chain.ChainId, "Creating new monitor monitor");

                    // Create the subscriptions record.
                    Record subscriptions = new Record();
                    subscriptions.SubscriptionList = new ConcurrentBag<UInt32>();
                    subscriptions.SubscriptionList.Add(gwcid);
                    subscriptions.FirstValue = true;
                    subscriptions.Channel = record.Channel;
                    subscriptions.Server = record.Destination;
                    subscriptions.SID = recordSID;
                    InfoService.SubscribedChannel[recId] = subscriptions;

                    // We don't need to skip till the first packet.
                    currentMonitor.PacketCount = 1;

                    gwcid = CidGenerator.Next();
                    subscriptions.GWCID = gwcid;

                    WorkerChain ioc = TcpManager.GetIocChain((packet.Chain == null ? null : packet.Chain.Gateway), record.Destination);
                    if (ioc == null)
                    {
                        if (Log.WillDisplay(TraceEventType.Error))
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "Lost IOC");
                        //chain.Dispose();
                        System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                        return;
                    }
                    ioc.ChannelSubscriptions[recId] = gwcid;

                    // Main subscription
                    currentMonitor = InfoService.ChannelSubscription.Create(gwcid);
                    currentMonitor.Channel = recId;
                    currentMonitor.Destination = record.Destination;
                    currentMonitor.SID = recordSID;
                    currentMonitor.DBRType = packet.DataType;
                    currentMonitor.DataCount = packet.DataCount;
                    currentMonitor.GWCID = record.GWCID;
                    newMonitor.GWCID = gwcid;

                    newPacket = (DataPacket)packet.Clone();
                    newPacket.Parameter1 = recordSID;
                    newPacket.Parameter2 = gwcid;
                    newPacket.Destination = record.Destination;

                    //sendData(newPacket);
                }
                else
                {
                    Record subscriptions = null;

                    if (Log.WillDisplay(TraceEventType.Information))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Information, chain.ChainId, "Linking to existing monitor");

                    // Add ourself to the subscriptions
                    subscriptions = InfoService.SubscribedChannel[recId];
                    if (subscriptions == null)
                    {
                        if (Log.WillDisplay(TraceEventType.Error))
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "Lost main monitor");
                        //chain.Dispose();
                        System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                        return;
                    }
                    subscriptions.SubscriptionList.Add(gwcid);
                    newMonitor.GWCID = subscriptions.GWCID;

                    // Channel never got the first answer
                    // So let's wait like the others
                    if (subscriptions.FirstValue)
                    //if(true)
                    {
                        currentMonitor.FirstValue = true;
                        currentMonitor.PacketCount = 1;
                    }
                    // Channel already got the first answer
                    // Send a ReadNotify to get the first value
                    else
                    {
                        currentMonitor.FirstValue = true;
                        currentMonitor.PacketCount = 0;
                        var dest = record.Destination;

                        UInt32 gwioid = CidGenerator.Next();
                        record = InfoService.IOID.Create(gwioid);
                        record.Destination = packet.Sender;
                        record.IOID = 0;
                        record.SID = packet.Parameter2;
                        record.DBRType = packet.DataType;
                        record.DataCount = packet.DataCount;
                        record.CID = gwcid;
                        record.Channel = channelName;

                        // Send an intial read-notify
                        newPacket = DataPacket.Create(0, packet.Chain);
                        newPacket.Command = 15;
                        newPacket.DataCount = packet.DataCount;
                        newPacket.DataType = packet.DataType;
                        newPacket.Parameter1 = recordSID;
                        newPacket.Parameter2 = gwioid;
                        newPacket.Destination = dest;

                        //sendData(newPacket);
                    }
                }
            }
            if(newPacket != null)
                sendData(newPacket);

        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            List<DataPacket> newPackets = new List<DataPacket>();

            lock (lockObject)
            {
                if (packet.PayloadSize == 0)
                {
                    // Closing channel.
                    return;
                }

                //Console.WriteLine("Got event add for " + packet.Parameter2);
                Record mainSubscription = InfoService.ChannelSubscription[packet.Parameter2];
                if (mainSubscription == null)
                {
                    if (Log.WillDisplay(TraceEventType.Error))
                        Log.TraceEvent(TraceEventType.Error, chain.ChainId, "Main monitor not found.");
                    //chain.Dispose();
                    return;
                }
                string recId = mainSubscription.Channel;
                Record subscriptions = InfoService.SubscribedChannel[recId];

                if (subscriptions == null)
                {
                    if (Log.WillDisplay(TraceEventType.Error))
                        Log.TraceEvent(TraceEventType.Error, chain.ChainId, "Subscription list not found not found.");
                    //chain.Dispose();
                    System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                    return;
                }

                var channelRecord=InfoService.ChannelEndPoint[mainSubscription.Channel.Split(new char[] { '°' })[0]];
                int origSid = int.Parse(mainSubscription.Channel.Split(new char[] { '°' })[1]);
                // We lost the channel
                if (channelRecord == null || channelRecord.SID != mainSubscription.SID || channelRecord.GWCID != mainSubscription.GWCID)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                    return;
                }

                // Keep a copy of the first packet.
                /*if (subscriptions.FirstPacket == null)
                    subscriptions.FirstPacket = (DataPacket)packet.Clone();*/

                subscriptions.FirstValue = false;

                foreach (UInt32 i in subscriptions.SubscriptionList)
                {
                    DataPacket newPacket = (DataPacket)packet.Clone();
                    Record subscription = InfoService.ChannelSubscription[i];

                    // Received a response after killing it maybe
                    if (subscription == null || subscription.SubscriptionId == null)
                        continue;

                    if (subscription.PacketCount == 0 && subscription.FirstValue == true)
                    {
                        //subscription.PacketCount++;
                        continue;
                    }

                    subscription.PacketCount++;

                    newPacket.Destination = subscription.Client;
                    newPacket.Parameter2 = subscription.SubscriptionId.Value;

                    // Event cancel send a command 1 as response (as event add)
                    // To see the difference check the payload as the event cancel always have a payload of 0
                    if (packet.PayloadSize == 0)
                    {
                        if (InfoService.ChannelSubscription.Remove(packet.Parameter2))
                            CidGenerator.ReleaseCid(packet.Parameter2);
                        WorkerChain clientChain = TcpManager.GetClientChain(newPacket.Destination);
                        if (clientChain != null)
                        {
                            uint val;
                            clientChain.Subscriptions.TryRemove(newPacket.Parameter2, out val);
                        }
                        continue;
                    }

                    /*if (!subscription.Channel.Split(new char[] { '°' })[0].EndsWith(newPacket.GetDataAsString()))
                    {
                        Console.WriteLine("Copy send " + subscription.Channel.Split(new char[] { '°' })[0] + " " + subscription.SubscriptionId.Value + " " + newPacket.GetDataAsString());

                    }*/
                    newPackets.Add(newPacket);
                }
            }

            foreach (var i in newPackets)
                sendData(i);
        }

        internal static void Unsubscribe(uint gwcid)
        {
            DataPacket newPacket=null;
            lock (lockObject)
            {
                Record subscription = InfoService.ChannelSubscription[gwcid];
                if (subscription == null)
                {
                    /*if (Log.WillDisplay(TraceEventType.Error))
                        Log.TraceEvent(TraceEventType.Error, -1, "Monitor not found while unsubscribing.");*/
                    return;
                }
                string recId = subscription.Channel;
                if (recId == null)
                    return;
                Record subscriptions = InfoService.SubscribedChannel[recId];
                if (subscriptions == null)
                    return;

                ConcurrentBag<UInt32> subList = subscriptions.SubscriptionList;
                ConcurrentBag<UInt32> newList = new ConcurrentBag<uint>();
                foreach (UInt32 i in subList.Where(row => row != gwcid))
                    newList.Add(i);
                subscriptions.SubscriptionList = newList;

                if (InfoService.ChannelSubscription.Remove(gwcid))
                    CidGenerator.ReleaseCid(gwcid);

                // Last monitor on the subscription, clean all
                if (subscriptions.SubscriptionList.Count == 0 && subscriptions.GWCID != null)
                {
                    if (Log.WillDisplay(TraceEventType.Information))
                        Log.TraceEvent(TraceEventType.Information, -1, "Removing monitor.");
                    uint mainGWCid = subscriptions.GWCID.Value;
                    Record record = InfoService.ChannelSubscription[mainGWCid];
                    if (record == null || record.Destination == null)
                        return;
                    WorkerChain ioc = TcpManager.GetIocChain(null, record.Destination);
                    if (ioc != null)
                    {
                        uint val;
                        ioc.ChannelSubscriptions.TryRemove(recId, out val);
                    }

                    newPacket = DataPacket.Create(0, null);
                    newPacket.Destination = record.Destination;
                    newPacket.Command = 2;
                    newPacket.DataType = record.DBRType.Value;
                    newPacket.DataCount = record.DataCount.Value;
                    newPacket.Parameter1 = record.SID.Value;
                    newPacket.Parameter2 = mainGWCid;

                    if (InfoService.ChannelSubscription.Remove(mainGWCid))
                        CidGenerator.ReleaseCid(mainGWCid);
                    InfoService.SubscribedChannel.Remove(recId);
                }
            }

            // Sending null as gateway avoid to create a new IOC chain in case the chain is gone
            if(newPacket != null)
                TcpManager.SendIocPacket(null, newPacket);
        }
    }
}
