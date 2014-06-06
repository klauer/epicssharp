using System;
using PBCaGw.Services;
using PBCaGw.Configurations;
using System.Diagnostics;
using PBCaGw.Workers;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 19 (0x13) CA_PROTO_WRITE_NOTIFY
    /// </summary>
    class WriteNotify : CommandHandler
    {
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            Record channelInfo = InfoService.ChannelCid[packet.Parameter1];
            // Lost the CID
            if (channelInfo == null)
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Error))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "WriteNotify not linked to a correct channel");
                packet.Chain.Dispose();
                return;
            }

            SecurityAccess access;
            switch (chain.Side)
            {
                case Workers.ChainSide.SIDE_A:
                    access = chain.Gateway.Configuration.Security.EvaluateSideA(channelInfo.Channel, chain.Username, chain.Hostname, packet.Sender.Address.ToString());
                    break;
                default:
                    access = chain.Gateway.Configuration.Security.EvaluateSideB(channelInfo.Channel, chain.Username, chain.Hostname, packet.Sender.Address.ToString());
                    break;
            }
            // We don't have write access quit!
            if (!access.Has(SecurityAccess.WRITE))
            {
                return;
            }

            DataPacket newPacket = (DataPacket)packet.Clone();
            UInt32 gwioid = CidGenerator.Next();
            Record record = InfoService.IOID.Create(gwioid);
            record.Destination = packet.Sender;
            record.IOID = packet.Parameter2;
            record.GWCID = packet.Parameter1;

            newPacket.Destination = channelInfo.Destination;
            // No SID? Can't write
            if (channelInfo.SID == null)
            {
                if (Log.WillDisplay(TraceEventType.Critical))
                    Log.TraceEvent(TraceEventType.Critical, chain.ChainId, "Write without SID");
                // Error packet.
                newPacket.ReverseAnswer = true;
                newPacket.Destination = packet.Sender;
                newPacket.Command = 11;
                /*WorkerChain ioc = TcpManager.GetIocChain(null, channelInfo.Destination);
                if (ioc != null)
                    ioc.Dispose();
                chain.Dispose();*/
                return;
            }


            newPacket.Parameter1 = channelInfo.SID.Value;
            newPacket.Parameter2 = gwioid;

            sendData(newPacket);
        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            DataPacket newPacket = (DataPacket)packet.Clone();
            Record record = InfoService.IOID[packet.Parameter2];
            if (record == null || record.IOID == null)
                return;

            if(InfoService.IOID.Remove(packet.Parameter2))
                CidGenerator.ReleaseCid(packet.Parameter2);

            newPacket.Destination = record.Destination;
            newPacket.Parameter2 = record.IOID.Value;
            sendData(newPacket);
        }
    }
}
