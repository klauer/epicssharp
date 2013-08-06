using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 21 (0x15) CA_PROTO_CLIENT_NAME
    /// </summary>
    class HostName : CommandHandler
    {
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            chain.Hostname = packet.GetDataAsString();
        }

        /// <summary>
        /// Doesn't have a response for this message
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            throw new NotImplementedException();
        }
    }
}
