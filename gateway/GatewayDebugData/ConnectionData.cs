using System.Collections.Generic;

namespace GatewayDebugData
{
    public class ConnectionData
    {
        readonly IDebugDataAccess access;
        readonly List<string> channels = new List<string>();
        readonly object lockObject = new object();
        readonly string name;

        public ConnectionData(IDebugDataAccess access, string name)
        {
            this.access = access;
            this.name = name;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        internal void ReadAll()
        {
            lock (lockObject)
            {
                int nbElements = access.GetInt();
                channels.Clear();

                for (int i = 0; i < nbElements; i++)
                    channels.Add(access.GetString());
            }
        }

        internal string AddChannel()
        {
            lock (lockObject)
            {
                string channel = access.GetString();
                channels.Add(channel);
                return channel;
            }
        }

        public IEnumerable<string> GetChannels()
        {
            lock (lockObject)
            {
                return channels.ToArray();
            }
        }
    }
}
