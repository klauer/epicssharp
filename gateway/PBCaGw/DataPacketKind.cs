using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBCaGw
{
    /// <summary>
    /// Defines what kind of data the DataPacket contains
    /// </summary>
    public enum DataPacketKind
    {
        RAW,
        HEAD,
        BODY,
        TAIL,
        COMPLETE
    }
}
