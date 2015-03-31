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
    static class ExtChannel
    {
        /*internal override void Decode(Channel channel, uint nbElements)
        {
            Status = (AlarmStatus)channel.DecodeData<ushort>(1, 0);
            Severity = (AlarmSeverity)channel.DecodeData<ushort>(1, 2);
            int pos = 4;
            Type t = typeof(TType);
            if (t.IsArray)
                t = t.GetElementType();
            if (t == typeof(object))
                t = channel.ChannelDefinedType;
            // padding for "RISC alignment"
            if (t == typeof(double))
                pos += 4;
            else if (t == typeof(byte))
                pos++;
            Value = channel.DecodeData<TType>(nbElements, pos);
        }*/

        static public DataPacket Encode(EpicsType type, object value, CARecord record, int nbElements = 1)
        {
            int size = 4;
            switch (type)
            {
                case EpicsType.Status_Double:
                    size += 4 + nbElements * 8;
                    break;
                case EpicsType.Status_Byte:
                    size += 1 + nbElements;
                    break;
                case EpicsType.Status_Int:
                case EpicsType.Status_Float:
                    size += nbElements * 4;
                    break;
                case EpicsType.Status_Short:
                    size += nbElements * 2;
                    break;
                case EpicsType.Status_String:
                    size += 40;
                    break;
                default:
                    break;
            }
            size += DataPacketBuilder.Padding(size);

            DataPacket res = DataPacket.Create(16 + size);
            res.DataCount = 1;
            res.DataType = (ushort)type;

            res.SetInt16(16, (short)record.AlarmStatus);
            res.SetInt16(16 + 2, (short)record.CurrentAlarmSeverity);

            DataPacketBuilder.Encode(res, type, (type == EpicsType.Status_Double ? 8 : type == EpicsType.Status_Byte ? 5 : 4), value);
            return res;
        }
    }
}
