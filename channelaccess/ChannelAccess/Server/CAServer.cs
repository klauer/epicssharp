using EpicsSharp.ChannelAccess.Server.RecordTypes;
using EpicsSharp.Common.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EpicsSharp.ChannelAccess.Server
{
    public class CAServer
    {
        DataPipe udpPipe;
        internal CARecordCollection records = new CARecordCollection();
        internal CARecordCollection Records { get { return records; } }
        CaServerListener listener;

        public int TcpPort { get; private set; }
        public int UdpPort { get; private set; }
        public int BeaconPort { get; private set; }

        public CAServer(IPAddress ipAddress = null, int tcpPort = 5064, int udpPort = 5064, int beaconPort = 0)
        {
            if (ipAddress == null)
                ipAddress = IPAddress.Any;

            if (beaconPort == 0)
                beaconPort = udpPort + 1;
            this.TcpPort = tcpPort;
            this.UdpPort = udpPort;
            this.BeaconPort = beaconPort;
            listener = new CaServerListener(this, new IPEndPoint(ipAddress, tcpPort));
            udpPipe = DataPipe.CreateServerUdp(this, ipAddress, udpPort);
        }

        public CAType CreateRecord<CAType>(string name) where CAType : CARecord
        {
            CAType result = null;
            try
            {
                result = (CAType)(typeof(CAType)).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { }, null).Invoke(new object[] { });
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
            result.Name = name;
            records.Add(result);
            return result;
        }


        internal void RegisterClient(IPEndPoint clientEndPoint, DataPipe chain)
        {
        }
    }
}
