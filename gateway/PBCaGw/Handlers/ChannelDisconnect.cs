using PBCaGw.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBCaGw.Handlers
{
    class ChannelDisconnect : CommandHandler
    {
        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        /// <param name="sendData"> </param>
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }

        /// <summary>
        /// Send to subscribed clients the message
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        /// <param name="sendData"> </param>
        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            string channelName=InfoService.ChannelEndPoint.SearchKeyForGWCID(packet.Parameter1);
            if(channelName == null)            
            {
                Log.TraceEvent(System.Diagnostics.TraceEventType.Critical,chain.ChainId,"Channel NAME / GWCID lost.");
                chain.Dispose();
                return;
            }
            TcpManager.DisposeGlobalChannel(channelName);


            /*chain.ChannelCid
            DataPacket newPacket = (DataPacket)packet.Clone();
            newPacket.Destination = packet.Sender;
            sendData(newPacket);*/
        }
    }
}
