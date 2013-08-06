using System.Net;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 13 (0x0D) CA_PROTO_RSRV_IS_UP
    /// </summary>
    class Beacon : CommandHandler
    {
        public override void DoRequest(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            IPAddress senderAddress=packet.Sender.Address;
            if (senderAddress.Equals(chain.Gateway.Configuration.LocalSideA.Address) || senderAddress.Equals(chain.Gateway.Configuration.LocalSideB.Address))
                return;

            // Use only the 5th beacon as restart
            if (packet.Parameter1 != 5)
                return;

            // Reset the beacon sender
            if (chain.Side == Workers.ChainSide.SIDE_A && chain.Gateway.beaconB != null)
                chain.Gateway.beaconB.ResetBeacon();
            else if (chain.Side == Workers.ChainSide.SIDE_B && chain.Gateway.beaconA != null)
                chain.Gateway.beaconA.ResetBeacon();
        }

        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
        }
    }
}
