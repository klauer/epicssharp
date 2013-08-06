using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PBCaGw.Services;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 23 (0x17) CA_PROTO_ECHO
    /// </summary>
    class Echo : CommandHandler
    {
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            HandleEcho(packet, chain, sendData);
        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            HandleEcho(packet, chain, sendData);
        }

        /// <summary>
        /// Both request / answer should be handled in the same way.
        /// If we didn't sent the first packet then we should answer,
        /// otherwise it's the answer to our own echo, therefore drop it.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        void HandleEcho(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate SendData)
        {
            // Answer to our own?
            if (InfoService.EchoSent[packet.Sender] != null)
            {
                // Yes then drop the packet and remove the info from our list
                InfoService.EchoSent.Remove(packet.Sender);
            }
            else
            {
                // No then let's answer with the same content just changing the destination as the sender
                DataPacket newPacket = (DataPacket)packet.Clone();
                newPacket.Destination = packet.Sender;
                //newPacket.NeedToFlush = true;
                SendData(newPacket);
            }
        }

    }
}
