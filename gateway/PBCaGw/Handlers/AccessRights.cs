namespace PBCaGw.Handlers
{
    /// <summary>
    /// 22 (0x16) CA_PROTO_ACCESS_RIGHTS
    /// </summary>
    class AccessRights : CommandHandler
    {
        /// <summary>
        /// Not used as we generate our own access right.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        /// <param name="sendData"></param>
        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }

        /// <summary>
        /// There is no requests for that, it's sent by the server after creating a channel
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="chain"></param>
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }
    }
}
