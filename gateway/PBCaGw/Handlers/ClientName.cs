namespace PBCaGw.Handlers
{
    /// <summary>
    /// 20 (0x14) CA_PROTO_CLIENT_NAME
    /// </summary>
    class ClientName : CommandHandler
    {
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            chain.Username = packet.GetDataAsString();
        }

        /// <summary>
        /// Doesn't have a response for this message
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        /// <param name="sendData"> </param>
        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }
    }
}
