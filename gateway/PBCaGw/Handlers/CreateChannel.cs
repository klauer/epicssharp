using System;
using System.Diagnostics;
using PBCaGw.Services;
using PBCaGw.Configurations;
using PBCaGw.Workers;
using System.Net;
using System.Collections.Concurrent;
using System.Linq;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 18 (0x12) CA_PROTO_CREATE_CHAN
    /// </summary>
    class CreateChannel : CommandHandler
    {
        public static object lockObject = new object();
        //static ConcurrentDictionary<string, object> locks = new ConcurrentDictionary<string, object>();

        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            /*Stopwatch sw = new Stopwatch();
            sw.Start();*/
            string channelName = packet.GetDataAsString();
            Record channelInfo = null;

            SecurityAccess access;
            switch (chain.Side)
            {
                case Workers.ChainSide.SIDE_A:
                    access = chain.Gateway.Configuration.Security.EvaluateSideA(channelName, chain.Username, chain.Hostname, packet.Sender.Address.ToString());
                    break;
                default:
                    access = chain.Gateway.Configuration.Security.EvaluateSideB(channelName, chain.Username, chain.Hostname, packet.Sender.Address.ToString());
                    break;
            }

            // Don't have the right, return a fail create channel
            if (!access.Has(SecurityAccess.READ))
            {
                if (Log.WillDisplay(TraceEventType.Warning))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Warning, packet.Chain.ChainId, "Create channel " + channelName + " from " + packet.Sender + " while not having rights to read");
                DataPacket newPacket = DataPacket.Create(0, chain);
                newPacket.Command = 26;
                newPacket.Parameter1 = packet.Parameter1;
                newPacket.Sender = packet.Sender;
                newPacket.Destination = packet.Sender;
                sendData(newPacket);
                return;
            }


            // Get a lock object for this particula channel name
            lock (lockObject)
            {
                // object lockOper = locks.GetOrAdd(channelName, new object());

                //if (InfoService.ChannelEndPoint.Knows(channelName))
                channelInfo = InfoService.ChannelEndPoint[channelName];

                // Never got this channel!
                if (channelInfo == null)
                {
                    StorageService<string> searchService;
                    if (chain.Side == ChainSide.SIDE_A)
                        searchService = InfoService.SearchChannelEndPointA;
                    else
                        searchService = InfoService.SearchChannelEndPointB;

                    if (searchService.Knows(channelName))
                        channelInfo = searchService[channelName];
                    else
                    {
                        var tmp = searchService.Where(row => row.Key.Split('.').First() == channelName.Split('.').First()).Select(row => row.Value).FirstOrDefault();
                        if (tmp == null)
                            tmp = InfoService.ChannelEndPoint.Where(row => row.Key.Split('.').First() == channelName.Split('.').First()).Select(row => row.Value).FirstOrDefault();
                        channelInfo = tmp;
                        if (tmp != null)
                            channelInfo = new Record { Destination = tmp.Destination, knownFromSideA = tmp.knownFromSideA, knownFromSideB = tmp.knownFromSideB, ChainSide = tmp.ChainSide, Server = tmp.Server };
                    }
                    if (channelInfo == null)
                    {

                        if (Log.WillDisplay(TraceEventType.Error))
                            Log.TraceEvent(TraceEventType.Error, chain.ChainId, "Created channel (" + channelName + ") without knowing where it should point at...");
                        System.Threading.ThreadPool.QueueUserWorkItem(action => chain.Dispose());
                        return;
                    }
                    //channelInfo = searchService[channelName];
                    channelInfo.ChainSide = chain.Side;
                    InfoService.ChannelEndPoint[channelName] = channelInfo;

                    channelInfo.GWCID = null;
                }
                //Console.WriteLine("Create 1: " + sw.Elapsed);

                if (Log.WillDisplay(TraceEventType.Verbose))
                    Log.TraceEvent(TraceEventType.Verbose, chain.ChainId, "Request " + channelName + " with cid " + packet.Parameter1);

                var knownGWCID = false;
                var knownSID = false;
                //lock (lockOper)
                //{
                knownGWCID = channelInfo.GWCID.HasValue;
                knownSID = channelInfo.SID.HasValue;
                //}

                // We never got back any answer, let's trash the GWCID to re-create it
                if (knownGWCID
                    && !knownSID
                    && (Gateway.Now - channelInfo.CreatedOn).TotalSeconds > 10)
                {
                    channelInfo.GWCID = null;
                    knownGWCID = false;
                }

                // Checks if we have already a channel open with the IOC or not
                // If we have it we can answer directly
                if (knownGWCID)
                {
                    //Console.WriteLine("Create 2: " + sw.Elapsed);
                    if (Log.WillDisplay(TraceEventType.Verbose))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, (packet.Chain == null ? 0 : packet.Chain.ChainId), "Request of a known channel (" + channelName + ")");
                    // We need to check if this is something currently in creation

                    // Seems we know all
                    if (knownSID)
                    {
                        if (Log.WillDisplay(TraceEventType.Verbose))
                            Log.TraceEvent(TraceEventType.Verbose, chain.ChainId, "Cached responce create channel cid " + packet.Parameter1);

                        chain.ChannelCid[channelName] = packet.Parameter1;

                        // We have all the info we can continue.
                        chain.Gateway.DoClientConnectedChannels(chain.ClientEndPoint.ToString(), channelName);

                        // Give back access rights before the create channel
                        DataPacket newPacket = DataPacket.Create(0, packet.Chain);
                        newPacket.Command = 22;
                        newPacket.DataType = 0;
                        newPacket.DataCount = 0;
                        newPacket.Parameter1 = packet.Parameter1;
                        newPacket.Parameter2 = (uint)access;
                        newPacket.Sender = packet.Sender;
                        newPacket.Destination = packet.Sender;
                        sendData(newPacket);

                        // Gives the create channel answer
                        newPacket = DataPacket.Create(0, packet.Chain);
                        newPacket.Command = 18;
                        newPacket.DataType = channelInfo.DBRType.Value;
                        newPacket.DataCount = channelInfo.DataCount.Value;
                        newPacket.Parameter1 = packet.Parameter1;
                        newPacket.Parameter2 = channelInfo.GWCID.Value;
                        newPacket.Sender = packet.Sender;
                        newPacket.Destination = packet.Sender;
                        sendData(newPacket);

                        try
                        {
                            ((TcpReceiver)TcpManager.GetClientChain(packet.Sender)[0]).Flush();
                        }
                        catch
                        {
                            try
                            {
                                packet.Chain.Dispose();
                            }
                            catch
                            {
                            }
                        }

                        //Console.WriteLine("Channel " + channelName + " " + channelInfo.GWCID.Value);
                        return;
                    }

                    // Still in creation then let's wait till the channel actually is created
                    //lock (lockOper)
                    //{
                    uint clientCid = packet.Parameter1;
                    IPEndPoint clientIp = packet.Sender;
                    Log.TraceEvent(TraceEventType.Verbose, chain.ChainId, "Add event for " + clientCid);

                    channelInfo.GetNotification += delegate(object sender, DataPacket receivedPacket)
                    {
                        //Console.WriteLine("Create 5: " + sw.Elapsed);
                        Record record = InfoService.ChannelCid[receivedPacket.Parameter1];
                        if (record == null || record.Channel == null) // Response too late, we drop it.
                            return;

                        Record resChannelInfo = InfoService.ChannelEndPoint[record.Channel];
                        if (resChannelInfo == null)
                            return;

                        if (Log.WillDisplay(TraceEventType.Verbose))
                            Log.TraceEvent(TraceEventType.Verbose, chain.ChainId, "Event responce create channel cid " + clientCid);

                        chain.ChannelCid[channelName] = clientCid;
                        chain.Gateway.DoClientConnectedChannels(chain.ClientEndPoint.ToString(), channelName);

                        // Give back access rights before the create channel
                        DataPacket resPacket = DataPacket.Create(0, packet.Chain);
                        resPacket.Command = 22;
                        resPacket.DataType = 0;
                        resPacket.DataCount = 0;
                        //resPacket.Parameter1 = packet.Parameter1;
                        resPacket.Parameter1 = clientCid;
                        resPacket.Parameter2 = (uint)access;
                        resPacket.Sender = clientIp;
                        resPacket.Destination = clientIp;
                        TcpManager.SendClientPacket(resPacket);

                        resPacket = (DataPacket)receivedPacket.Clone();
                        resPacket.Command = 18;
                        resPacket.Destination = clientIp;
                        resPacket.Parameter1 = clientCid;
                        resPacket.Parameter2 = channelInfo.GWCID.Value;
                        resPacket.Sender = clientIp;
                        TcpManager.SendClientPacket(resPacket);

                        //Console.WriteLine("2Channel " + channelName + " " + channelInfo.GWCID.Value);
                    };
                    //}
                }
                // We don't have, we need therefore to connect to the IOC to create one
                else
                {
                    //Console.WriteLine("Create 3: " + sw.Elapsed);
                    /*if (chain.ChannelCid.ContainsKey(channelName))
                    {
                        if (Log.WillDisplay(TraceEventType.Warning))
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Warning, (packet.Chain == null ? 0 : packet.Chain.ChainId), "Duplicated request (" + channelName + ")");
                    }*/

                    if (Log.WillDisplay(TraceEventType.Verbose))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, (packet.Chain == null ? 0 : packet.Chain.ChainId), "Request of a new channel (" + channelName + ")");
                    channelInfo.GWCID = packet.Parameter1;

                    chain.Gateway.DoClientConnectedChannels(chain.ClientEndPoint.ToString(), channelName);

                    UInt32 gwcid = CidGenerator.Next();
                    Record record = InfoService.ChannelCid.Create(gwcid);
                    record.Channel = channelName;
                    record.GWCID = gwcid;
                    record.Client = packet.Sender;
                    record.AccessRight = access;
                    record.Destination = channelInfo.Server;
                    record.ChainSide = chain.Side;
                    chain.ChannelCid[channelName] = packet.Parameter1;

                    // Send create channel
                    DataPacket newPacket = (DataPacket)packet.Clone();
                    newPacket.Parameter1 = gwcid;
                    // Version
                    newPacket.Parameter2 = Gateway.CA_PROTO_VERSION;
                    newPacket.Destination = channelInfo.Server;
                    sendData(newPacket);
                }
                //Console.WriteLine("Create 6: " + sw.Elapsed);
            }
        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            lock (lockObject)
            {
                if (!InfoService.ChannelCid.Knows(packet.Parameter1))  // Response too late, we drop it.
                {
                    if (Log.WillDisplay(TraceEventType.Verbose))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, (packet.Chain == null ? 0 : packet.Chain.ChainId), "Drop late reponse.");
                    return;
                }

                Record record = InfoService.ChannelCid[packet.Parameter1];
                if (record.Channel == null) // Response too late, we drop it.
                {
                    if (Log.WillDisplay(TraceEventType.Verbose))
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, (packet.Chain == null ? 0 : packet.Chain.ChainId), "Drop late reponse.");
                    return;
                }
                if (Log.WillDisplay(TraceEventType.Verbose))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, (packet.Chain == null ? 0 : packet.Chain.ChainId), "Got response for " + record.Channel + ".");

                record.SID = packet.Parameter2;
                if (!chain.Channels.Any(row => row == record.Channel))
                    chain.Channels.Add(record.Channel);

                //object lockOper = locks.GetOrAdd(record.Channel, new object());

                // Stores in the channel end point the retreiven info
                Record channelInfo;
                //lock (lockOper)
                //if(true)
                {
                    channelInfo = InfoService.ChannelEndPoint[record.Channel];
                    if (channelInfo == null)
                    {
                        if (Log.WillDisplay(TraceEventType.Error))
                            Log.TraceEvent(System.Diagnostics.TraceEventType.Error, (packet.Chain == null ? 0 : packet.Chain.ChainId), "Got create channel response, but lost the request.");
                        return;
                    }
                    channelInfo.SID = packet.Parameter2;
                    channelInfo.DBRType = packet.DataType;
                    channelInfo.DataCount = packet.DataCount;
                    channelInfo.GWCID = packet.Parameter1;
                }
                channelInfo.Notify(chain, packet);

                //Console.WriteLine("2Channel " + record.Channel + " " + packet.Parameter1+" "+packet.Parameter2);

                // Was a prepared creation, let's stop
                if (record.Client != null && record.Channel != null)
                {
                    WorkerChain destChain = TcpManager.GetClientChain(record.Client);
                    if (destChain != null && destChain.ChannelCid.ContainsKey(record.Channel))
                    {
                        if (Log.WillDisplay(TraceEventType.Verbose))
                            Log.TraceEvent(TraceEventType.Verbose, chain.ChainId, "Direct responce create channel cid " + destChain.ChannelCid[record.Channel]);

                        // Give back access rights before the create channel
                        DataPacket accessPacket = DataPacket.Create(0, packet.Chain);
                        accessPacket.Command = 22;
                        accessPacket.DataType = 0;
                        accessPacket.DataCount = 0;
                        accessPacket.Parameter1 = destChain.ChannelCid[record.Channel];
                        accessPacket.Parameter2 = (uint)record.AccessRight;
                        accessPacket.Sender = packet.Sender;
                        accessPacket.Destination = record.Client;
                        sendData(accessPacket);

                        DataPacket newPacket = (DataPacket)packet.Clone();
                        newPacket.Parameter1 = destChain.ChannelCid[record.Channel];
                        newPacket.Parameter2 = packet.Parameter1;
                        newPacket.Destination = record.Client;
                        sendData(newPacket);

                        try
                        {
                            ((TcpReceiver)TcpManager.GetClientChain(record.Client)[0]).Flush();
                        }
                        catch
                        {
                            try
                            {
                                packet.Chain.Dispose();
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
        }
    }
}
