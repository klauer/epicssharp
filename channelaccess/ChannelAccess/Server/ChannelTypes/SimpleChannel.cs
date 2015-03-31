using EpicsSharp.ChannelAccess.Common;
using EpicsSharp.ChannelAccess.Constants;
using EpicsSharp.ChannelAccess.Server.RecordTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicsSharp.ChannelAccess.Server.ChannelTypes
{
    static class SimpleChannel
    {
        static public DataPacket Encode(EpicsType type, object value, CARecord record, int nbElements = 1)
        {
            DataPacket res = DataPacket.Create(16 + (type == EpicsType.String ? 40 : 8));
            res.DataCount = 1;
            res.DataType = (ushort)type;
            DataPacketBuilder.Encode(res, type, 0, value);
            return res;
        }
    }
}
