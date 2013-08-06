namespace PBCaGw.Handlers
{
    /// <summary>
    /// 12 (0x0C) CA_PROTO_CLEAR_CHANNEL
    /// </summary>
    class ClearChannel : CommandHandler
    {
        /// <summary>
        /// Closing channel is currently simply answering as if we do it... but in fact does nothing.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        /// <param name="sendData"> </param>
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            DataPacket newPacket = (DataPacket)packet.Clone();
            newPacket.Destination = packet.Sender;
            sendData(newPacket);
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        /// <param name="sendData"> </param>
        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }
    }
}
