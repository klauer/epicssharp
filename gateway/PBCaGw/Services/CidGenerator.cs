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
        static bool[] freeCids = new bool[128000];
        //static bool[] freeCids = new bool[2000];
        static UInt32 cidCounter = 1;
        static object lockObject = new object();
        static int freeNbCid;

        static CidGenerator()
        {
            for (var i = 0; i < freeCids.Length; i++)
                freeCids[i] = true;
            freeNbCid = freeCids.Length - 1;
        }

        static public UInt32 Next()
        {
            lock (lockObject)
            {
                if (freeNbCid < 3)
                    throw new Exception("All cids exausted!!!");
                UInt32 result = cidCounter;
                freeCids[cidCounter] = false;
                int nbChecked = 0;
                do
                {
                    cidCounter = (UInt32)((cidCounter + 1) % freeCids.Length);
                    if (cidCounter == 0)
                        cidCounter++;
                    nbChecked++;
                } while (freeCids[cidCounter] == false && nbChecked < freeCids.Length);
                if(nbChecked >= freeCids.Length)
                    throw new Exception("All cids exausted!!!");
                freeNbCid--;
                return result;
            }
        }

        static public void ReleaseCid(UInt32 id)
        {
            lock (lockObject)
            {
                freeNbCid++;
                freeCids[id] = true;
            }
        }

        public static UInt32 Peek()
        {
            return (uint)cidCounter;
        }

        /*static int cidCounter = 1;
        const int minFree=10000;
        static ConcurrentQueue<int> freeIds = new ConcurrentQueue<int>();

        static CidGenerator()
        {
            for(int i=0;i < minFree;i++)
            {
                int result = Interlocked.Increment(ref cidCounter);
                freeIds.Enqueue(result);
            }
        }

        static public UInt32 Next()
        {
            int result;

            if (freeIds.Count > minFree)
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
        }*/
    }
}
