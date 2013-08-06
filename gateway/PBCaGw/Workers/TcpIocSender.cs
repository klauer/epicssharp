using PBCaGw.Services;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Sends the message to a defined TCP socket which is connected to a CA server (IOC for example)
    /// </summary>
    class TcpIocSender : SenderWorker
    {
        public override void ProcessData(DataPacket packet)
        {
            if (packet.Destination == null)
                return;
            /*try
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Verbose))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, (this.Chain == null ? 0 : this.Chain.ChainId), "Sending " + Handlers.CommandHandler.GetCommandName(packet.Command) + " " + packet.Command + " to " + packet.Destination + " size: " + packet.BufferSize);
            }
            catch
            {
            }*/

            if (object.ReferenceEquals(packet.Sender, packet.Destination))
            {
                TcpManager.SendClientPacket(packet);
            }
            else
            {
// ReSharper disable PossibleNullReferenceException
                this.Chain.UseChain(TcpManager.GetIocChain(this.Chain.Gateway, packet.Destination));
// ReSharper restore PossibleNullReferenceException
                TcpManager.SendIocPacket(this.Chain.Gateway, packet);
            }
        }
    }
}
