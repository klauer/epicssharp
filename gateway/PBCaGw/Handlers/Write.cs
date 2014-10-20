using System;
using System.Diagnostics;
using PBCaGw.Services;
using PBCaGw.Configurations;
using PBCaGw.Workers;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 4 (0x04) CA_PROTO_WRITE
    /// </summary>
    class Write : CommandHandler
    {
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            Record channelInfo = InfoService.ChannelCid[packet.Parameter1];
            // Lost the CID
            if (channelInfo == null)
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Error))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Error, chain.ChainId, "Write not linked to a correct channel");
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

            newPacket.Destination = channelInfo.Destination;
            // No SID? Can't write
            if (channelInfo.SID == null)
            {
                if (Log.WillDisplay(TraceEventType.Critical))
                    Log.TraceEvent(TraceEventType.Critical, chain.ChainId, "Write without SID");
                WorkerChain ioc = TcpManager.GetIocChain(null, channelInfo.Destination);
                if (ioc != null)
                    ioc.Dispose();
                chain.Dispose();
                //chain.DisposeChannel();
                return;
            }

            newPacket.Parameter1 = channelInfo.SID.Value;
            newPacket.Parameter2 = gwioid;
            CidGenerator.ReleaseCid(gwioid);

            sendData(newPacket);
        }

        /// <summary>
        /// CA_PROTO_WRITE doesn't have a response.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        /// <param name="sendData"></param>
        /// <exception cref="NotImplementedException"></exception>
        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            throw new NotImplementedException();
        }
    }
}
