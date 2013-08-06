using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 0 (0x00) CA_PROTO_VERSION
    /// </summary>
    class Version : CommandHandler
    {
        public override void DoRequest(DataPacket packet, PBCaGw.Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            /*DataPacket newPacket = (DataPacket)packet.Clone();
            newPacket.Destination = packet.Sender;
            newPacket.DataType = 0;
            newPacket.DataCount = Gateway.CA_PROTO_VERSION;
            SendData(newPacket);*/
        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }
    }
}
