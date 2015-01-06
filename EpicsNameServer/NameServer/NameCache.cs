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
        SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        Dictionary<string, NameEntry> cache = new Dictionary<string, NameEntry>();
        readonly private NameServer nameServer;

        public NameCache(NameServer nameServer)
        {
            this.nameServer = nameServer;
        }

        public void Remove(string key)
        {
            locker.Wait();
            try
            {
                if (cache.ContainsKey(key))
                    cache.Remove(key);
            }
            catch
            {

            }
            finally
            {
                locker.Release();
            }

        }

        public NameEntry this[string key]
        {
            get
            {
                locker.Wait();
                try
                {
                    if (!cache.ContainsKey(key))
                        cache.Add(key, new NameEntry(key, nameServer));
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

        internal void Clear()
        {
            List<NameEntry> entries = null;
            locker.Wait();
            try
            {
                entries = cache.Values.ToList();
            }
            catch
            {

            }
            finally
            {
                locker.Release();
            }
            if(entries != null) foreach (var i in entries)
                i.Dispose();
        }
    }
}
