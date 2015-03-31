using EpicsSharp.ChannelAccess.Server.RecordTypes;
using EpicsSharp.Common.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EpicsSharp.ChannelAccess.Server
{
    class ServerTcpReceiver : TcpReceiver
    {
        uint nextSid = 1;
        object locker = new object();
        Dictionary<string, uint> channelIds = new Dictionary<string, uint>();

        public void Init(Socket socket)
        {
            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            base.Start(socket);
        }

        public uint RegisterChannel(string channelName)
        {
            lock (locker)
            {
                uint sid = nextSid++;
                channelIds.Add(channelName, sid);
                return sid;
            }
        }

        public string FindProperty(CAServer server, uint sid)
        {
            string channelName = null;
            lock (locker)
            {
                channelName = channelIds.Where(row => row.Value == sid).Select(row => row.Key).First();
            }
            string property = "VAL";
            if (channelName.IndexOf('.') != -1)
                property = channelName.Split('.').Last();
            return property;
        }

        public CARecord FindRecord(CAServer server, uint sid)
        {
            string channelName = null;
            lock (locker)
            {
                channelName = channelIds.Where(row => row.Value == sid).Select(row => row.Key).First();
            }
            string property = "VAL";
            if (channelName.IndexOf('.') != -1)
            {
                property = channelName.Split('.').Last();
                channelName = channelName.Split('.').First();
            }

            return server.Records[channelName];
        }

        internal object RecordValue(CAServer server, uint sid)
        {
            string channelName = null;
            lock (locker)
            {
                channelName = channelIds.Where(row => row.Value == sid).Select(row => row.Key).First();
            }
            string property = "VAL";
            if (channelName.IndexOf('.') != -1)
            {
                property = channelName.Split('.').Last();
                channelName = channelName.Split('.').First();
            }

            return server.Records[channelName][property];
        }
    }
}
