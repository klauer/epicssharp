using PBCaGw.Services;
using System.Net.Sockets;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Sends the UDP packet directly.
    /// </summary>
    public class UdpSender : SenderWorker
    {
        public override void ProcessData(DataPacket packet)
        {
            Socket socket;

            // For a search with direct answer don't use the usual rules
            if (packet.ReverseAnswer)
            {
                if (this.Chain.Side == ChainSide.SIDE_A || this.Chain.Side == ChainSide.UDP_RESP_SIDE_B)
                    socket = this.Chain.Gateway.Configuration.UdpReceiverA;
                else
                    socket = this.Chain.Gateway.Configuration.UdpReceiverB;
            }
            else
            {
                if (this.Chain.Side == ChainSide.SIDE_A || this.Chain.Side == ChainSide.UDP_RESP_SIDE_B)
                    socket = this.Chain.Gateway.Configuration.UdpReceiverB;
                else
                    socket = this.Chain.Gateway.Configuration.UdpReceiverA;
            }

            if (packet.Destination != null)
            {
                //Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, -1, "UDP From " + socket.LocalEndPoint + " to " + packet.Destination);
                try
                {
                    socket.SendTo(packet.Data, packet.Offset, packet.BufferSize, SocketFlags.None, packet.Destination);
                }
                catch
                {
                }
            }
            //packet.Dispose();
        }
    }
}
