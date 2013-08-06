using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// Skips the message (at least for the moment)
    /// </summary>
    class DoNothing : CommandHandler
    {
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }
    }
}
