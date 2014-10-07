using System;
using System.Diagnostics;
using PBCaGw.Services;
using PBCaGw.Workers;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 15 (0x0F) CA_PROTO_READ_NOTIFY
    /// </summary>
    class ReadNotify : CommandHandler
    {
        static ReadNotify()
        {
            InfoService.IOID.CleanupKey += new AutoCleaningStorageService<uint>.CleanupKeyDelegate(IoidCleanupKey);
        }

        // Is too slow to answer, we drop the client chain, that should cleanup the mess.
        // It's a workaround not a real solution
        static void IoidCleanupKey(uint key)
        {
            CidGenerator.ReleaseCid(key);
            Record record = InfoService.IOID[key];

            if (record == null || record.Destination == null)
                return;

            // It's the initial answer as get of "cached" monitor.
            if (!(record.IOID.HasValue && record.IOID.Value == 0))
            {
                WorkerChain chain = TcpManager.GetClientChain(record.Destination);
                if (chain == null)
                    return;
                /*if (Log.WillDisplay(TraceEventType.Error))
                    Log.TraceEvent(TraceEventType.Error, chain.ChainId, "IOID operation timeout. Dropping client, with the hope to recover the situation.");
                chain.Dispose();*/

                if (Log.WillDisplay(TraceEventType.Error))
                    Log.TraceEvent(TraceEventType.Error, chain.ChainId, "IOID operation timeout. Disposing client channel.");

                chain.DisposeChannel(InfoService.ChannelEndPoint.SearchKeyForGWCID(record.SID.Value), record.SID.Value);
            }
        }

        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            DataPacket newPacket = (DataPacket)packet.Clone();
            UInt32 gwioid = CidGenerator.Next();
            Record record = InfoService.IOID.Create(gwioid);
            record.Destination = packet.Sender;
            record.IOID = packet.Parameter2;
            record.SID = packet.Parameter1;

            record = InfoService.ChannelCid[packet.Parameter1];

            // Lost the CID
            if (record == null)
            {
                if (Log.WillDisplay(TraceEventType.Error))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "Readnotify not linked to a correct channel");
                packet.Chain.Dispose();
                return;
            }

            if (record.SID == null)
            {
                if (Log.WillDisplay(TraceEventType.Error))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "Readnotify without SID");
                chain.Dispose();
                //chain.DisposeChannel(packet.Parameter1);
                return;
            }

            newPacket.Destination = record.Destination;
            newPacket.Parameter1 = record.SID.Value;
            newPacket.Parameter2 = gwioid;

            sendData(newPacket);
        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            DataPacket newPacket = (DataPacket)packet.Clone();
            Record record = InfoService.IOID[packet.Parameter2];

            if (record == null)
                return;

            if (InfoService.IOID.Remove(packet.Parameter2))
                CidGenerator.ReleaseCid(packet.Parameter2);

            // It's the initial answer as get of "cached" monitor.
            if (record.IOID.HasValue && record.IOID.Value == 0)
            {
                lock (EventAdd.lockObject)
                {

                    if (!record.SID.HasValue)
                    {
                        /*InfoService.IOID.Remove(packet.Parameter2);
                        CidGenerator.ReleaseCid(packet.Parameter2);*/
                        return;
                    }

                    if (record.CID.HasValue && InfoService.ChannelSubscription.Knows(record.CID.Value))
                    {
                        try
                        {
                            if (InfoService.ChannelSubscription[record.CID.Value].PacketCount == 0 && InfoService.ChannelSubscription[record.CID.Value].FirstValue == true)
                            {
                                if (Log.WillDisplay(TraceEventType.Verbose))
                                    Log.TraceEvent(TraceEventType.Verbose, chain.ChainId, "Sending readnotify data on " + record.SID.Value);

                                /*if (!record.Channel.Split(new char[] { '°' })[0].EndsWith(newPacket.GetDataAsString()))
                                {
                                    Console.WriteLine("Copy send " + record.Channel.Split(new char[] { '°' })[0] + " " + record.SubscriptionId.Value + " " + newPacket.GetDataAsString());

                                }*/

                                newPacket.Command = 1;
                                newPacket.Parameter1 = 1;
                                newPacket.Parameter2 = record.SID.Value;
                                newPacket.Destination = record.Destination;
                                newPacket.DataCount = record.DataCount.Value;
                                newPacket.DataType = record.DBRType.Value;
                                // Don't move it out for the moment
                                sendData(newPacket);

                                //Console.WriteLine("Get send " + record.Channel + " " + record.SID.Value+" "+newPacket.GetDataAsString());

                                InfoService.ChannelSubscription[record.CID.Value].FirstValue = false;
                                InfoService.ChannelSubscription[record.CID.Value].PacketCount = 1;
                            }
                        }
                        catch
                        {
                        }
                    }
                    // Removes it to avoid the cleaup            
                    /*InfoService.IOID.Remove(packet.Parameter2);
                    CidGenerator.ReleaseCid(packet.Parameter2);*/
                    return;
                }
            }

            newPacket.Destination = record.Destination;
            newPacket.Parameter1 = 1;
            newPacket.Parameter2 = record.IOID.Value;
            sendData(newPacket);

            // Removes it to avoid the cleaup            
            /*InfoService.IOID.Remove(packet.Parameter2);
            CidGenerator.ReleaseCid(packet.Parameter2);*/
        }
    }
}
