using PBCaGw.Services;
namespace PBCaGw.Handlers
{
    /// <summary>
    /// 2 (0x02) CA_PROTO_EVENT_CANCEL
    /// </summary>
    class EventCancel : CommandHandler
    {
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            try
            {
                EventAdd.Unsubscribe(chain.Subscriptions[packet.Parameter2]);
                uint res;
                chain.Subscriptions.TryRemove(packet.Parameter2, out res);
            }
            catch
            {
                if (Log.WillDisplay(System.Diagnostics.TraceEventType.Critical))
                    Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, chain.ChainId, "Error while cancelling a monitor.");
                //chain.Dispose();
            }
        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }
    }
}
