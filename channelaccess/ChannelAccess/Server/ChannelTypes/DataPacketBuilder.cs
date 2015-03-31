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
    static class DataPacketBuilder
    {
        static public DataPacket Encode(EpicsType type, object source, CARecord record)
        {
            switch (type)
            {
                case EpicsType.Int:
                case EpicsType.Short:
                case EpicsType.Float:
                case EpicsType.Double:
                case EpicsType.String:
                    return SimpleChannel.Encode(type, source, record);
                case EpicsType.Status_Int:
                case EpicsType.Status_Short:
                case EpicsType.Status_Float:
                case EpicsType.Status_Double:
                case EpicsType.Status_String:
                    return ExtChannel.Encode(type, source, record);
                default:
                    throw new Exception("Not yet supported");
            }
        }

        public static int Padding(int size)
        {
            return (8 - (size % 8));
        }

        public static void Encode(DataPacket result, EpicsType type, int offset, object value)
        {
            switch (type)
            {
                case EpicsType.Status_Byte:
                case EpicsType.Byte:
                    result.SetInt32(16 + offset, Convert.ToByte(value));
                    break;
                case EpicsType.Int:
                case EpicsType.Status_Int:
                    result.SetInt32(16 + offset, Convert.ToInt32(value));
                    break;
                case EpicsType.Float:
                case EpicsType.Status_Float:
                    result.SetFloat(16 + offset, Convert.ToSingle(value));
                    break;
                case EpicsType.Double:
                case EpicsType.Status_Double:
                    result.SetDouble(16 + offset, Convert.ToDouble(value));
                    break;
                case EpicsType.Short:
                case EpicsType.Status_Short:
                    result.SetInt16(16 + offset, Convert.ToInt16(value));
                    break;
                case EpicsType.String:
                case EpicsType.Status_String:
                    result.SetDataAsString(Convert.ToString(value), offset, 40);
                    break;
                default:
                    throw new Exception("Unsuported type");
            }
        }
    }
}
