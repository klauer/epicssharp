using System;
using System.Collections.Generic;
using System.Linq;

namespace PBCaGw.Services
{
    /// <summary>
    /// Storage Service which removes older entries.
    /// Entries after 20 secs are removed.
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public class AutoCleaningStorageService<TType> : StorageService<TType>
    {
        public delegate void CleanupKeyDelegate(TType key);
        public event CleanupKeyDelegate CleanupKey;

        /// <summary>
        /// Lifetime to keep the record in seconds.
        /// </summary>
        public int Lifetime { get; set; }

        public AutoCleaningStorageService()
        {
            Gateway.OneSecJobs += new EventHandler(CleanOldRecords);
            Lifetime = 3;
        }

        /// <summary>
        /// Removes the old entries
        /// </summary>
        void CleanOldRecords(object sender,EventArgs evt)
        {

            DateTime limit = Gateway.Now.AddSeconds(-Lifetime);
            /*if(Lifetime == 5)
                Console.WriteLine("Clean me "+Lifetime);*/
            /*List<TType> toClean = Records.Where(row => row.Value.CreatedOn < limit).Select(row => row.Key).ToList();
            foreach (TType i in toClean)
            {
                Record value;
                if (Records.TryRemove(i, out value))
                {
                    if (CleanupKey != null)
                        CleanupKey(i);
                }
            }*/

            lock (Records)
            {
                List<TType> toClean = Records.Where(row => row.Value.CreatedOn < limit).Select(row => row.Key).ToList();
                foreach (TType i in toClean)
                {
                    if (CleanupKey != null)
                        CleanupKey(i);
                    Records.Remove(i);
                }
            }
        }
    }
}
