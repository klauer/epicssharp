using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.Collections;

namespace PBCaGw.Services
{
    /// <summary>
    /// Generates unique ID to be used accross the gateway
    /// </summary>
    public static class CidGenerator
    {
        const int MAX_CID = 1024000;
        //const int MAX_CID = 8000;
        static bool[] freeCids = new bool[MAX_CID];
        static string[] askedBy = new string[MAX_CID];
        static UInt32 cidCounter = 0;
        static object lockObject = new object();
        public static int freeNbCid;

        static CidGenerator()
        {
            for (var i = 0; i < freeCids.Length; i++)
                freeCids[i] = true;
            freeNbCid = freeCids.Length - 1;
        }

        static IEnumerable UsedCidStats()
        {
            return askedBy.Where(row => row != null)
                .Select(row => new
                {
                    Where = row.Split(new char[] { '|' })[0],
                    When = DateTime.FromBinary(long.Parse(row.Split(new char[] { '|' })[1]))
                })
                .ToList()
                .GroupBy(row => row.Where)
                .OrderByDescending(row => row.Count())
                .ToList()
                .Select(row => new
                {
                    NB = row.Count(),
                    Where = row.First().Where,
                    Oldest = (DateTime.Now - row.OrderBy(r2 => r2.When).First().When).ToString()
                }).ToList();
        }

        static public UInt32 Next()
        {
            lock (lockObject)
            {
                if (freeNbCid < 1)
                {
                    //var q = UsedCidStats();
                    throw new Exception("All cids exausted!!!");
                }
                int nbChecked = 0;
                do
                {
                    cidCounter = (UInt32)((cidCounter + 1) % freeCids.Length);
                    if (cidCounter == 0)
                        cidCounter++;
                    nbChecked++;
                } while (freeCids[cidCounter] == false && nbChecked < freeCids.Length);
                if (nbChecked >= freeCids.Length)
                {
                    //var q = UsedCidStats();
                    throw new Exception("All cids exausted!!!");
                }

                // Stores who asked the cid
                /*StackTrace stackTrace = new StackTrace(true);
                StackFrame[] stackFrames = stackTrace.GetFrames();
                askedBy[cidCounter] = stackFrames[1].GetMethod().ReflectedType.Name + "." + stackFrames[1].GetMethod().Name + ":" + stackFrames[1].GetFileLineNumber() + "|" + DateTime.Now.ToBinary();*/
                freeCids[cidCounter] = false;
                freeNbCid--;
                return cidCounter;
            }
        }

        static public void ReleaseCid(UInt32 id)
        {
            lock (lockObject)
            {
                if (freeCids[id])
                {

                }
                else
                {
                    freeNbCid++;
                    freeCids[id] = true;

                    askedBy[id] = null;

                    /*StackTrace stackTrace = new StackTrace(true);
                    StackFrame[] stackFrames = stackTrace.GetFrames();
                    askedBy[id] = stackFrames[1].GetMethod().ReflectedType.Name + "." + stackFrames[1].GetMethod().Name + ":" + stackFrames[1].GetFileLineNumber();*/

                }
            }
        }

        public static UInt32 Peek()
        {
            return (uint)cidCounter;
        }
    }
}
