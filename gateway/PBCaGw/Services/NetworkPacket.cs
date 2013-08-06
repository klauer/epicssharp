using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBCaGw.Services
{
    enum NetworkDirection
    {
        IN,
        OUT
    }

    class NetworkPacket
    {
        public NetworkDirection Direction { get; set; }
        public int Command { get; set; }
        public string RemotePoint { get; set; }
        public byte[] FullData { get; set; }
    }
}
