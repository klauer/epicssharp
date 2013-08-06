using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBCaGw.Configurations
{
    [Flags]
    public enum SecurityAccess
    {
        NONE = 0x0,
        READ = 0x1,
        WRITE = 0x2,
        ALL = READ | WRITE
    }

    public static class SecurityAccessExtensions
    {
        //checks if the value contains the provided type
        public static bool Has<T>(this System.Enum type, T value)
        {
            try
            {
                return (((int)(object)type & (int)(object)value) == (int)(object)value);
            }
            catch
            {
                return false;
            }
        }
    }
}
