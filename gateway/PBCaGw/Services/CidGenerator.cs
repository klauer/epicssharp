using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace PBCaGw.Services
{
    /// <summary>
    /// Generates unique ID to be used accross the gateway
    /// </summary>
    public static class CidGenerator
    {
        static int cidCounter = 1;
        static ConcurrentQueue<int> freeIds = new ConcurrentQueue<int>();

        static public UInt32 Next()
        {
            int result;

            if (freeIds.Count > 1000)
            {
                if (freeIds.TryDequeue(out result))
                    return (uint)result;
            }

            result = Interlocked.Increment(ref cidCounter);
            return (uint)result;
        }

        static public void ReleaseCid(UInt32 id)
        {
            freeIds.Enqueue((int)id);
        }

        public static UInt32 Peek()
        {
            return (uint)cidCounter;
        }
    }
}
