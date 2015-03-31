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
    static class TimeChannel
    {
        /*internal override void Decode(Channel channel, uint nbElements)
        {
            Status = (AlarmStatus)channel.DecodeData<ushort>(1, 0);
            Severity = (AlarmSeverity)channel.DecodeData<ushort>(1, 2);
            Time = channel.DecodeData<DateTime>(1, 4);
            int pos = 12;
            Type t = typeof(TType);
            if (t.IsArray)
                t = t.GetElementType();
            if (t == typeof(object))
                t = channel.ChannelDefinedType;
            // padding for "RISC alignment"
            if (t == typeof(byte))
                pos += 3;
            else if (t == typeof(double))
                pos += 4;
            else if (t == typeof(short))
                pos += 2;
            Value = channel.DecodeData<TType>(nbElements, pos);
         * 
         *                 long secs = RawData.GetUInt32((int)RawData.HeaderSize + startPost);
                long nanoSecs = RawData.GetUInt32((int)RawData.HeaderSize + startPost + 4);
                DateTime d = (new DateTime(timestampBase.Ticks + (secs * 10000000L) + (nanoSecs / 100L))).ToLocalTime();
                return d;

        }*/

        static public DataPacket Encode(EpicsType type, object value, CARecord record, int nbElements = 1)
        {
            int size = 12;
            int startPos = 0;
            switch (type)
            {
                case EpicsType.Time_Double:
                    size += 4 + nbElements * 8;
                    startPos = 4;
                    break;
                case EpicsType.Time_Byte:
                    size += 3 + nbElements;
                    startPos = 3;
                    break;
                case EpicsType.Time_Int:
                case EpicsType.Time_Float:
                    size += nbElements * 4;
                    startPos = 0;
                    break;
                case EpicsType.Time_Short:
                    size += 2 + nbElements * 2;
                    startPos = 2;
                    break;
                case EpicsType.Time_String:
                    startPos = 0;
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
            res.SetDateTime(16 + 4, DateTime.Now);

            DataPacketBuilder.Encode(res, type, 12 + startPos, value);
            return res;
        }
    }
}
