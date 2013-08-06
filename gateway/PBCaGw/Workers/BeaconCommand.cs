using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PBCaGw.Services;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Calls the correct request command handler
    /// </summary>
    public class BeaconCommand : CommandWorker
    {
        public override void ProcessData(DataPacket packet)
        {
            if (packet.Kind != DataPacketKind.COMPLETE)
                return;
            if (packet.Command != 13)
                return;
            if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, this.Chain.ChainId, "Side: " + this.Chain.Side + " UDP (Beacon) Request: " + Handlers.CommandHandler.GetCommandName(packet.Command) + " " + packet.Command);
            Handlers.CommandHandler.ExecuteRequestHandler(packet.Command, packet, this.Chain, this.SendData);
            /*List<DataPacket> result = Handlers.CommandHandler.ExecuteRequestHandler(packet.Command, packet, this.Chain);
            foreach (var i in result)
            {
                this.SendData(i);
            }*/
        }
    }
}
