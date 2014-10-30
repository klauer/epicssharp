using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NameServer
{
    class NameCache
    {
        SemaphoreSlim locker= new SemaphoreSlim(1, 1);
        Dictionary<string, NameEntry> cache = new Dictionary<string, NameEntry>();
        readonly private NameServer nameServer;

        public NameCache(NameServer nameServer)
        {
            this.nameServer = nameServer;
        }
        
        public NameEntry this[string key]
        {
            get
            {
                locker.Wait();
                try
                {
                    if (!cache.ContainsKey(key))
                    {
                        cache.Add(key, new NameEntry(nameServer));
                        cache[key].Name = key;
                    }
                    return cache[key];
                }
                catch
                {

                }
                finally
                {
                    locker.Release();
                }
                return null;
            }
        }
    }
}
