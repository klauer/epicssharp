using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;

namespace PBCaGw.Services
{
    /// <summary>
    /// Base class for storage services.
    /// Allows to store data in thread safe ways.
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public class StorageService<TType> : IEnumerable
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

        public void Remove(TType key)
        {
            Record value;
            Records.TryRemove(key, out value);
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
    }
}
