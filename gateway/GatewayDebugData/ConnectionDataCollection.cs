using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GatewayDebugData
{
    public class ConnectionDataCollection : IEnumerable<ConnectionData>
    {
        readonly object lockObject = new object();
        readonly Dictionary<string, ConnectionData> hosts = new Dictionary<string, ConnectionData>();
        readonly IDebugDataAccess access;

        public ConnectionDataCollection(IDebugDataAccess access)
        {
            this.access = access;
        }

        public IEnumerator<ConnectionData> GetEnumerator()
        {
            IEnumerable<ConnectionData> tempIocs;
            lock (lockObject)
            {
                tempIocs = hosts.Values.ToArray();
            }
            return tempIocs.GetEnumerator();
        }

        public void GetAll()
        {
            lock (lockObject)
            {
                hosts.Clear();
                int nbElements = access.GetInt();
                for (int i = 0; i < nbElements; i++)
                {
                    string name = access.GetString();
                    hosts.Add(name, new ConnectionData(access, name));
                    hosts[name].ReadAll();
                }
            }
        }

        public ConnectionData GetByName(string name)
        {
            lock (lockObject)
            {
                if (!hosts.ContainsKey(name))
                    hosts.Add(name, new ConnectionData(access, name));
                return hosts[name];
            }
        }

        public void DropByName(string name)
        {
            lock (lockObject)
            {
                if (hosts.ContainsKey(name))
                    hosts.Remove(name);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        internal void Clear()
        {
            lock (lockObject)
            {
                hosts.Clear();
            }
        }
    }
}
