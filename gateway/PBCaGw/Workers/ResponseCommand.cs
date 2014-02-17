using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PBCaGw.Services;
using System.Net;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Calls the correct response command handler
    /// </summary>
    public class ResponseCommand : CommandWorker
    {
        IPEndPoint savedDestination = null;
        bool? isTcpMember;

        public override void ProcessData(DataPacket packet)
        {
            if (this.Chain.Gateway.IsDisposed)
                return;

            if (!isTcpMember.HasValue)
                isTcpMember = !(this.Chain[0] is UdpReceiver);

            if (!isTcpMember.Value && packet.Command != 6)
                return;

            // Partial packet, let's pass it as it is
            if (packet.Kind != DataPacketKind.COMPLETE && packet.Kind != DataPacketKind.HEAD)
            {
                packet.Destination = savedDestination;
                this.SendData(packet);
                if (packet.Kind == DataPacketKind.TAIL)
                    savedDestination = null;
            }
            else
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                {
                    if (isTcpMember.Value)
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, Chain.ChainId, "Side: " + this.Chain.Side + " TCP Response: " + Handlers.CommandHandler.GetCommandName(packet.Command) + " " + packet.Command);
                    else
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, Chain.ChainId, "Side: " + this.Chain.Side + " UDP Response: " + Handlers.CommandHandler.GetCommandName(packet.Command) + " " + packet.Command);
                }
                // Unkown command => drop connection
                if (!Handlers.CommandHandler.IsAllowed(packet.Command))
                {
                    packet.Chain.Dispose();
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, Chain.ChainId, "Unkown command: " + packet.Command);
                    //packet.Dispose();
                    return;
                }

                Handlers.CommandHandler.ExecuteResponseHandler(packet.Command, packet, this.Chain, PreSendData);
                //packet.Dispose();
            }
        }

        void PreSendData(DataPacket i)
        {
            if (i.Kind == DataPacketKind.HEAD)
                savedDestination = i.Destination;

            this.SendData(i);
            /*if (isTcpMember.Value && i.NeedToFlush)
                TcpManager.FlushBuffer(i.Destination);*/
        }
    }
}
