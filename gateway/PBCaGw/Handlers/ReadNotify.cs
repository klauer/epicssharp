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
            Record record = InfoService.IOID[key];
            if (record == null || record.Destination == null)
                return;

            CidGenerator.ReleaseCid(key);

            // It's the initial answer as get of "cached" monitor.
            if (!(record.IOID.HasValue && record.IOID.Value == 0))
            {
                WorkerChain chain = TcpManager.GetClientChain(record.Destination);
                if (chain == null)
                    return;
                if (Log.WillDisplay(TraceEventType.Error))
                    Log.TraceEvent(TraceEventType.Error, chain.ChainId, "IOID operation timeout. Dropping client, with the hope to recover the situation.");
                chain.Dispose();
            }
        }

        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            DataPacket newPacket = (DataPacket)packet.Clone();
            UInt32 gwioid = CidGenerator.Next();
            Record record = InfoService.IOID.Create(gwioid);
            record.Destination = packet.Sender;
            record.IOID = packet.Parameter2;

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

            // Removes it to avoid the cleaup            
            InfoService.IOID.Remove(packet.Parameter2);
            CidGenerator.ReleaseCid(packet.Parameter2);

            lock (EventAdd.lockObject)
            {
                // It's the initial answer as get of "cached" monitor.
                if (record.IOID.HasValue && record.IOID.Value == 0)
                {
                    if (!record.SID.HasValue)
                        return;

                    if (record.CID.HasValue && InfoService.ChannelSubscription.Knows(record.CID.Value))
                    {
                        if (InfoService.ChannelSubscription[record.CID.Value].PacketCount == 0 && InfoService.ChannelSubscription[record.CID.Value].FirstValue == true)
                        {
                            if (Log.WillDisplay(TraceEventType.Verbose))
                                Log.TraceEvent(TraceEventType.Verbose, chain.ChainId, "Sending readnotify data on " + record.SID.Value);

                            newPacket.Command = 1;
                            newPacket.Parameter1 = 1;
                            newPacket.Parameter2 = record.SID.Value;
                            newPacket.Destination = record.Destination;
                            newPacket.DataCount = record.DataCount.Value;
                            newPacket.DataType = record.DBRType.Value;

                            sendData(newPacket);
                            InfoService.ChannelSubscription[record.CID.Value].FirstValue = false;
                            InfoService.ChannelSubscription[record.CID.Value].PacketCount = 1;
                        }
                    }
                    return;
                }
            }

            newPacket.Destination = record.Destination;
            newPacket.Parameter1 = 1;
            newPacket.Parameter2 = record.IOID.Value;
            sendData(newPacket);
        }
    }
}
