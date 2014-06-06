using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace PBCaGw.Services
{
    /// <summary>
    /// Base class for storage services.
    /// Allows to store data in thread safe ways.
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public class StorageService<TType> : IEnumerable<KeyValuePair<TType,Record>>
    {
        protected ConcurrentDictionary<TType, Record> Records = new ConcurrentDictionary<TType, Record>();

        public Record Create(TType key)
        {
            /*Record newRecord = new Record();
            Records[key] = newRecord;
            return newRecord;*/
            return Records.GetOrAdd(key, new Record());
        }

        public Record this[TType key]
        {
            get
            {
                Record val;
                if (!Records.TryGetValue(key, out val))
                    return null;
                return val;
            }
            set
            {
                Records[key] = value;
            }
        }

        public bool Remove(TType key)
        {
            Record value;
            return Records.TryRemove(key, out value);
        }

        public int Count
        {
            get
            {
                return Records.Count;
            }
        }

        public bool Knows(TType key)
        {
            return Records.ContainsKey(key);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Records.GetEnumerator();
        }

        public void DeleteForGWCID(uint gwcid)
        {
            var toDelete = Records.Where(row => row.Value.GWCID == gwcid).Select(row => row.Key).ToList();
            Record o;
            foreach (var i in toDelete)
            {
                Records.TryRemove(i, out o);
            }
        }

        public void DeleteForSID(uint sid)
        {
            var toDelete = Records.Where(row => row.Value.SID == sid).Select(row => row.Key).ToList();
            Record o;
            foreach (var i in toDelete)
            {
                Records.TryRemove(i, out o);
            }
        }

        public TType SearchKeyForGWCID(uint gwcid)
        {
            try
            {
                var r = Records.First(row => row.Value.GWCID == gwcid);
                return r.Key;
            }
            catch
            {
                return default(TType);
            }
        }

        public TType SearchKeyForCID(uint cid)
        {
            try
            {
                var r = Records.First(row => row.Value.CID == cid);
                return r.Key;
            }
            catch
            {
                return default(TType);
            }
        }


        IEnumerator<KeyValuePair<TType, Record>> IEnumerable<KeyValuePair<TType, Record>>.GetEnumerator()
        {
            return Records.GetEnumerator();
        }
    }
}
