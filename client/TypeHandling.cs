using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSI.EpicsClient2
{
    static class TypeHandling
    {
        public static Dictionary<Type, EpicsType> Lookup = new Dictionary<Type, EpicsType>
        {
            {typeof(uint),EpicsType.Internal_UInt},
            {typeof(ushort),EpicsType.Internal_UShort},

            {typeof(sbyte),EpicsType.SByte},
            {typeof(string),EpicsType.String},
            {typeof(short),EpicsType.Short},
            {typeof(int),EpicsType.Int},
            {typeof(float),EpicsType.Float},
            {typeof(double),EpicsType.Double},
            {typeof(Enum),EpicsType.Enum},

            {typeof(ExtType<sbyte>) ,EpicsType.Status_SByte},
            {typeof(ExtType<string>),EpicsType.Status_String},
            {typeof(ExtType<short>) ,EpicsType.Status_Short},
            {typeof(ExtType<int>)   ,EpicsType.Status_Int},
            {typeof(ExtType<float>) ,EpicsType.Status_Float},
            {typeof(ExtType<double>),EpicsType.Status_Double},
            {typeof(ExtType<Enum>)  ,EpicsType.Status_Enum},

            {typeof(ExtTimeType<sbyte>) ,EpicsType.Time_SByte},
            {typeof(ExtTimeType<string>),EpicsType.Time_String},
            {typeof(ExtTimeType<short>) ,EpicsType.Time_Short},
            {typeof(ExtTimeType<int>)   ,EpicsType.Time_Int},
            {typeof(ExtTimeType<float>) ,EpicsType.Time_Float},
            {typeof(ExtTimeType<double>),EpicsType.Time_Double},
            {typeof(ExtTimeType<Enum>)  ,EpicsType.Time_Enum},

            {typeof(ExtGraphic<sbyte>) ,EpicsType.Display_SByte},
            {typeof(ExtGraphic<string>),EpicsType.Display_String},
            {typeof(ExtGraphic<short>) ,EpicsType.Display_Short},
            {typeof(ExtGraphic<int>)   ,EpicsType.Display_Int},
            {typeof(ExtGraphic<float>) ,EpicsType.Display_Float},
            {typeof(ExtGraphic<double>),EpicsType.Display_Double},
            //{typeof(ExtGraphic<Enum>)  ,EpicsType.Display_Enum}, // Does not exists???

            {typeof(ExtControl<sbyte>) ,EpicsType.Control_SByte},
            {typeof(ExtControl<string>),EpicsType.Control_String},
            {typeof(ExtControl<short>) ,EpicsType.Control_Short},
            {typeof(ExtControl<int>)   ,EpicsType.Control_Int},
            {typeof(ExtControl<float>) ,EpicsType.Control_Float},
            {typeof(ExtControl<double>),EpicsType.Control_Double},
            {typeof(ExtControl<Enum>)  ,EpicsType.Control_Enum},

            // Array types

            {typeof(ExtType<sbyte[]>) ,EpicsType.Status_SByte},
            {typeof(ExtType<string[]>),EpicsType.Status_String},
            {typeof(ExtType<short[]>) ,EpicsType.Status_Short},
            {typeof(ExtType<int[]>)   ,EpicsType.Status_Int},
            {typeof(ExtType<float[]>) ,EpicsType.Status_Float},
            {typeof(ExtType<double[]>),EpicsType.Status_Double},
            {typeof(ExtType<Enum[]>)  ,EpicsType.Status_Enum},

            {typeof(ExtTimeType<sbyte[]>) ,EpicsType.Time_SByte},
            {typeof(ExtTimeType<string[]>),EpicsType.Time_String},
            {typeof(ExtTimeType<short[]>) ,EpicsType.Time_Short},
            {typeof(ExtTimeType<int[]>)   ,EpicsType.Time_Int},
            {typeof(ExtTimeType<float[]>) ,EpicsType.Time_Float},
            {typeof(ExtTimeType<double[]>),EpicsType.Time_Double},
            {typeof(ExtTimeType<Enum[]>)  ,EpicsType.Time_Enum},

            {typeof(ExtGraphic<sbyte[]>) ,EpicsType.Display_SByte},
            {typeof(ExtGraphic<string[]>),EpicsType.Display_String},
            {typeof(ExtGraphic<short[]>) ,EpicsType.Display_Short},
            {typeof(ExtGraphic<int[]>)   ,EpicsType.Display_Int},
            {typeof(ExtGraphic<float[]>) ,EpicsType.Display_Float},
            {typeof(ExtGraphic<double[]>),EpicsType.Display_Double},
            //{typeof(ExtGraphic<Enum>)  ,EpicsType.Display_Enum}, // Does not exists???

            {typeof(ExtControl<sbyte[]>) ,EpicsType.Control_SByte},
            {typeof(ExtControl<string[]>),EpicsType.Control_String},
            {typeof(ExtControl<short[]>) ,EpicsType.Control_Short},
            {typeof(ExtControl<int[]>)   ,EpicsType.Control_Int},
            {typeof(ExtControl<float[]>) ,EpicsType.Control_Float},
            {typeof(ExtControl<double[]>),EpicsType.Control_Double},
            {typeof(ExtControl<Enum[]>)  ,EpicsType.Control_Enum},
        };

        public static Dictionary<EpicsType, Type> ReverseLookup;

        static TypeHandling()
        {
            ReverseLookup=new Dictionary<EpicsType, Type>();
            foreach (var i in Lookup)
                if (!ReverseLookup.ContainsKey(i.Value))
                    ReverseLookup.Add(i.Value, i.Key);
            //ReverseLookup = Lookup.ToDictionary(key => key.Value, value => value.Key);
        }

        static public int EpicsSize(object obj)
        {
            if (obj is string)
                return ((string)obj).Length;
            return EpicsSize(obj.GetType());
        }

        static public int EpicsSize(Type t)
        {
            if (t.Equals(typeof(string)))
            {
                return 40;
            }
            else switch (Lookup[t])
                {
                    case EpicsType.Int:
                        return 4;
                    case EpicsType.Short:
                        return 2;
                    case EpicsType.SByte:
                        return 1;
                    case EpicsType.Float:
                        return 4;
                    case EpicsType.Double:
                        return 8;
                    default:
                        throw new Exception("Type not yet supported.");
                }
        }

        static public int Padding(int size)
        {
            if (size % 8 == 0)
                return 8;
            else
                return (8 - (size % 8));
        }
    }
}
