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
    public class StorageService<TType> : IEnumerable<KeyValuePair<TType, Record>>
    {
        //protected ConcurrentDictionary<TType, Record> Records = new ConcurrentDictionary<TType, Record>();
        protected Dictionary<TType, Record> Records = new Dictionary<TType, Record>();

        public Record Create(TType key)
        {
            lock (Records)
            {
                Record newRecord = new Record();
                //Records.Add(key, newRecord);
                Records[key] = newRecord;
                return newRecord;
            }


            /*Records[key] = newRecord;
            return newRecord;*/

            //return Records.GetOrAdd(key, new Record());
        }

        public Record this[TType key]
        {
            get
            {
                lock (Records)
                {
                    if (!Records.ContainsKey(key))
                        return null;
                    return Records[key];
                }
                /*Record val;
                if (!Records.TryGetValue(key, out val))
                    return null;
                return val;*/
            }
            set
            {
                lock (Records)
                {
                    Records[key] = value;
                }
            }
        }

        public bool Remove(TType key)
        {
            /*Record value;
            return Records.TryRemove(key, out value);*/

            lock (Records)
            {
                if (!Records.ContainsKey(key))
                    return false;
                Records.Remove(key);
                return true;
            }
        }

        public int Count
        {
            get
            {
                lock (Records)
                {
                    return Records.Count;
                }
            }
        }

        public bool Knows(TType key)
        {
            lock (Records)
            {
                return Records.ContainsKey(key);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            lock (Records)
            {
                var l = Records.ToList();
                return l.GetEnumerator();
            }

            //return Records.GetEnumerator();
        }

        public void DeleteForGWCID(uint gwcid)
        {
            /*var toDelete = Records.Where(row => row.Value.GWCID == gwcid).Select(row => row.Key).ToList();
            Record o;
            foreach (var i in toDelete)
            {
                Records.TryRemove(i, out o);
            }*/

            lock (Records)
            {
                var toDelete = Records.Where(row => row.Value.GWCID == gwcid).Select(row => row.Key).ToList();
                foreach (var i in toDelete)
                {
                    Records.Remove(i);
                }
            }
        }

        public void DeleteForSID(uint sid)
        {
            /*var toDelete = Records.Where(row => row.Value.SID == sid).Select(row => row.Key).ToList();
            Record o;
            foreach (var i in toDelete)
            {
                Records.TryRemove(i, out o);
            }*/

            lock (Records)
            {
                var toDelete = Records.Where(row => row.Value.SID == sid).Select(row => row.Key).ToList();
                foreach (var i in toDelete)
                {
                    Records.Remove(i);
                }
            }
        }

        public TType SearchKeyForGWCID(uint gwcid)
        {
            lock (Records)
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
            /*try
            {
                var r = Records.First(row => row.Value.GWCID == gwcid);
                return r.Key;
            }
            catch
            {
                return default(TType);
            }*/
        }

        public TType SearchKeyForCID(uint cid)
        {
            lock (Records)
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

            /*try
            {
                var r = Records.First(row => row.Value.CID == cid);
                return r.Key;
            }
            catch
            {
                return default(TType);
            }*/
        }


        IEnumerator<KeyValuePair<TType, Record>> IEnumerable<KeyValuePair<TType, Record>>.GetEnumerator()
        {
            lock (Records)
            {
                var l = Records.ToList();
                return l.GetEnumerator();
            }
            //return Records.GetEnumerator();
        }
    }
}
