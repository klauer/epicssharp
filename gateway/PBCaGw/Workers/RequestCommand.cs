using System;
using System.Diagnostics;
using PBCaGw.Services;
using System.Net;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Calls the correct request command handler
    /// </summary>
    public class RequestCommand : CommandWorker
    {
        //IPEndPoint savedDestination = null;
        bool? isTcpMember;

        public override void ProcessData(DataPacket packet)
        {
            if (this.Chain.Gateway.IsDisposed)
                return;
            if (this.Chain.IsDisposed)
                return;

            if (!isTcpMember.HasValue)
                isTcpMember = !(this.Chain[0] is UdpReceiver);

            if (!isTcpMember.Value && packet.Command != 6)
                return;


                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                {
                    if (isTcpMember.Value)
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, this.Chain.ChainId, "Side: " + this.Chain.Side + " " + packet.Sender + " TCP Request: " + Handlers.CommandHandler.GetCommandName(packet.Command) + " " + packet.Command);
                    else
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, this.Chain.ChainId, "Side: " + this.Chain.Side + " UDP Request: " + Handlers.CommandHandler.GetCommandName(packet.Command) + " " + packet.Command);
                }
                // Unkown command => drop connection
                if (!Handlers.CommandHandler.IsAllowed(packet.Command))
                {
                    if (packet.Chain[0] is TcpReceiver)
                    {
                        Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, Chain.ChainId, "Unkown command: " + packet.Command);
                        packet.Chain.Dispose();
                        //packet.Dispose();
                    }
                    return;
                }

                Handlers.CommandHandler.ExecuteRequestHandler(packet.Command, packet, this.Chain, PreSendData);
                //packet.Dispose();
        }

        void PreSendData(DataPacket i)
        {
            /*if (i.Kind == DataPacketKind.HEAD)
                savedDestination = i.Destination;*/

            this.SendData(i);
        }
    }
}
