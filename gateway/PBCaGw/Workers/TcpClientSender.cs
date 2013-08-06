using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PBCaGw.Services;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Sends the message to a defined TCP socket which is connected to a CA client (MEDM for example)
    /// </summary>
    class TcpClientSender : SenderWorker
    {
        public override void ProcessData(DataPacket packet)
        {
            TcpManager.SendClientPacket(packet);
        }
    }
}
