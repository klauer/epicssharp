using System;
using System.Diagnostics;
using System.Linq;
using PBCaGw.Services;
using PBCaGw.Workers;
using System.Collections.Concurrent;
using System.Threading;

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
                    packet.Chain.Dispose();
                    return;
                }

                if (record.SID == null)
                {
                    if (Log.WillDisplay(TraceEventType.Error))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "EventAdd SID null");
                    packet.Chain.Dispose();
                    return;
                }

                if (Log.WillDisplay(TraceEventType.Information))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Information, chain.ChainId, "Add event for " + record.Channel);

                // Not enough info
                if (packet.MessageSize < 12 + 2 + packet.HeaderSize)
                {

                }

                string recId = record.Channel + "/" + packet.DataType + "/" + packet.DataCount + "/" + packet.GetUInt16(12 + (int)packet.HeaderSize);
                //Console.WriteLine(recId);

                //recId = ""+CidGenerator.Next();

                UInt32 gwcid = CidGenerator.Next();
                Record currentMonitor = InfoService.ChannelSubscription.Create(gwcid);
                currentMonitor.Destination = record.Destination;
                currentMonitor.DBRType = packet.DataType;
                currentMonitor.DataCount = packet.DataCount;
                currentMonitor.Client = packet.Sender;
                currentMonitor.SubscriptionId = packet.Parameter2;
                currentMonitor.SID = record.SID.Value;
                currentMonitor.Channel = recId;
                currentMonitor.FirstValue = false;

                chain.Subscriptions[packet.Parameter2] = gwcid;

                // A new monitor
                // Create a new subscription for the main channel
                // And create a list of subscriptions
                if (!InfoService.SubscribedChannel.Knows(recId))
                {
                    if (Log.WillDisplay(TraceEventType.Information))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Information, chain.ChainId, "Creating new monitor monitor");

                    // Create the subscriptions record.
                    Record subscriptions = new Record();
                    subscriptions.SubscriptionList = new ConcurrentBag<UInt32>();
                    subscriptions.SubscriptionList.Add(gwcid);
                    subscriptions.FirstValue = true;
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
                        chain.Dispose();
                        return;
                    }
                    ioc.ChannelSubscriptions[recId] = gwcid;

                    currentMonitor = InfoService.ChannelSubscription.Create(gwcid);
                    currentMonitor.Channel = recId;
                    currentMonitor.Destination = record.Destination;
                    currentMonitor.SID = record.SID;
                    currentMonitor.DBRType = packet.DataType;
                    currentMonitor.DataCount = packet.DataCount;

                    DataPacket newPacket = (DataPacket)packet.Clone();
                    newPacket.Parameter1 = record.SID.Value;
                    newPacket.Parameter2 = gwcid;
                    newPacket.Destination = record.Destination;
                    sendData(newPacket);
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
                        chain.Dispose();
                        return;
                    }
                    subscriptions.SubscriptionList.Add(gwcid);

                    // Channel never got the first answer
                    // So let's wait like the others
                    if (subscriptions.FirstValue)
                    {
                        currentMonitor.FirstValue = true;
                        currentMonitor.PacketCount = 1;
                    }
                    // Channel already got the first answer
                    // Send a ReadNotify to get the first value
                    else
                    {
                        /*DataPacket newPacket = (DataPacket)subscriptions.FirstPacket.Clone();
                        newPacket.Destination = packet.Sender;
                        newPacket.Parameter2 = packet.Parameter2;
                        newPacket.Sender = packet.Sender;
                        sendData(newPacket);*/

                        currentMonitor.FirstValue = true;
                        currentMonitor.PacketCount = 0;

                        UInt32 gwioid = CidGenerator.Next();
                        // Send an intial read-notify
                        DataPacket newPacket = DataPacket.Create(0, packet.Chain);
                        newPacket.Command = 15;
                        newPacket.DataCount = packet.DataCount;
                        newPacket.DataType = packet.DataType;
                        newPacket.Parameter1 = record.SID.Value;
                        newPacket.Parameter2 = gwioid;
                        newPacket.Destination = record.Destination;

                        record = InfoService.IOID.Create(gwioid);
                        record.Destination = packet.Sender;
                        record.IOID = 0;
                        record.SID = packet.Parameter2;
                        record.DBRType = packet.DataType;
                        record.DataCount = packet.DataCount;
                        record.CID = gwcid;

                        sendData(newPacket);
                    }
                }
            }
        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            lock (lockObject)
            {
                if (packet.PayloadSize == 0)
                {
                    // Closing channel.
                    return;
                }

                Record mainSubscription = InfoService.ChannelSubscription[packet.Parameter2];
                if (mainSubscription == null)
                {
                    /*if (Log.WillDisplay(TraceEventType.Error))
                        Log.TraceEvent(TraceEventType.Error, chain.ChainId, "Main monitor not found.");*/
                    //chain.Dispose();
                    return;
                }
                string recId = mainSubscription.Channel;
                Record subscriptions = InfoService.SubscribedChannel[recId];

                if (subscriptions == null)
                {
                    if (Log.WillDisplay(TraceEventType.Error))
                        Log.TraceEvent(TraceEventType.Error, chain.ChainId, "Subscription list not found not found.");
                    chain.Dispose();
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
                        InfoService.ChannelSubscription.Remove(packet.Parameter2);
                        CidGenerator.ReleaseCid(packet.Parameter2);
                        WorkerChain clientChain = TcpManager.GetClientChain(newPacket.Destination);
                        if (clientChain != null)
                        {
                            uint val;
                            clientChain.Subscriptions.TryRemove(newPacket.Parameter2, out val);
                        }
                        continue;
                    }

                    sendData(newPacket);
                }
            }
        }

        internal static void Unsubscribe(uint gwcid)
        {
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

                InfoService.ChannelSubscription.Remove(gwcid);
                CidGenerator.ReleaseCid(gwcid);

                // Last monitor on the subscription, clean all
                if (subscriptions.SubscriptionList.Count == 0)
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

                    DataPacket newPacket = DataPacket.Create(0, null);
                    newPacket.Destination = record.Destination;
                    newPacket.Command = 2;
                    newPacket.DataType = record.DBRType.Value;
                    newPacket.DataCount = record.DataCount.Value;
                    newPacket.Parameter1 = record.SID.Value;
                    newPacket.Parameter2 = mainGWCid;
                    // Sending null as gateway avoid to create a new IOC chain in case the chain is gone
                    TcpManager.SendIocPacket(null, newPacket);

                    InfoService.ChannelSubscription.Remove(mainGWCid);
                    CidGenerator.ReleaseCid(mainGWCid);
                    InfoService.SubscribedChannel.Remove(recId);
                }
            }
        }
    }
}
