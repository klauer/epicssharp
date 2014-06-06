using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace PBCaGw.Services
{
    /// <summary>
    /// Stores search info and automatically cleans it up after max 20 sec.
    /// </summary>
    public class SearchChannel : AutoCleaningStorageService<UInt32>
    {
        public SearchChannel()
        {
            this.CleanupKey += SearchChannel_CleanupKey;
        }

        void SearchChannel_CleanupKey(uint key)
        {
            CidGenerator.ReleaseCid(key);
        }

        public Record Create()
        {
            UInt32 gwcid = CidGenerator.Next();
            Record record = this.Create(gwcid);
            record.GWCID = gwcid;
            return record;
        }
    }
}
