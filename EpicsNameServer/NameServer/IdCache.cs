using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NameServer
{
    class IdCache
    {
        SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        private NameServer nameServer;
        uint nextId = 1;
        Dictionary<uint, NameEntry> cache = new Dictionary<uint, NameEntry>();

        public IdCache(NameServer nameServer)
        {
            this.nameServer = nameServer;
        }

        public NameEntry this[uint key]
        {
            get
            {
                locker.Wait();
                try
                {
                    return cache[key];
                }
                finally
                {
                    locker.Release();
                }
            }
        }

        internal void Release(NameEntry nameEntry)
        {
            locker.Wait();
            try
            {
                cache.Remove(nameEntry.SearchId);
            }
            finally
            {
                locker.Release();
            }
        }

        internal void Store(NameEntry nameEntry)
        {
            locker.Wait();
            try
            {
                nameEntry.SearchId = nextId++;
                cache.Add(nameEntry.SearchId, nameEntry);
            }
            finally
            {
                locker.Release();
            }
        }
    }
}
