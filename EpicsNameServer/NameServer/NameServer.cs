using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NameServer
{
    public class NameServer
    {
        public const int BUFFER_SIZE = 8192 * 30;
        public const UInt16 CA_PROTO_VERSION = 11;

        UdpReceiver udpReceiver;
        readonly internal NameCache Cache;
        readonly internal IdCache IdCache;
        public int Port { get; set; }
        public IPAddress BindingAddress { get; set; }

        public string SearchAddress = "255.255.255.255:5064";
        List<IPEndPoint> dests = new List<IPEndPoint>();

        public string ClusterPrefix { get; set; }

        public int NodeId { get; set; }

        public int NodesInCluster { get; set; }

        public NameServer()
        {
            this.Cache = new NameCache(this);
            this.IdCache = new IdCache(this);


            if (!string.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["BindingAddress"]))
            {
                var s = System.Configuration.ConfigurationManager.AppSettings["BindingAddress"];
                this.BindingAddress = IPAddress.Parse(s.Split(new char[] { ':' })[0]);
                this.Port = int.Parse(s.Split(new char[] { ':' })[1]);
            }

            if (!string.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["SearchAddress"]))
            {
                SearchAddress = System.Configuration.ConfigurationManager.AppSettings["SearchAddress"];
            }

            if (!string.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["ClusterPrefix"]))
            {
                ClusterPrefix = System.Configuration.ConfigurationManager.AppSettings["ClusterPrefix"];
            }

            if (!string.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["NodeId"]))
            {
                NodeId = int.Parse(System.Configuration.ConfigurationManager.AppSettings["NodeId"]);
            }

            if (!string.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["NodesInCluster"]))
            {
                NodesInCluster = int.Parse(System.Configuration.ConfigurationManager.AppSettings["NodesInCluster"]);
            }
        }

        IPEndPoint ParseAddress(string addr)
        {
            string[] parts = addr.Split(new char[] { ':' });
            try
            {
                return new IPEndPoint(IPAddress.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()));
            }
            catch (Exception ex)
            {
                try
                {
                    return new IPEndPoint(Dns.GetHostEntry(parts[0]).AddressList.First(), int.Parse(parts[1].Trim()));
                }
                catch (Exception ex2)
                {
                    Console.WriteLine("Wrong IP: " + addr);
                    throw ex2;
                }
            }
        }


        public void Start()
        {
            udpReceiver = new UdpReceiver(this, BindingAddress, Port);
            dests = SearchAddress.Replace(" ", ";").Replace(",", ";")
                .Split(new char[] { ';' })
                .Select(row => ParseAddress(row))
                .ToList();
            udpReceiver.Start();
        }

        internal void Send(DataPacket newPacket)
        {
            udpReceiver.Send(newPacket);
        }

        internal void SendSearch(DataPacket newPacket)
        {
            foreach (var i in dests)
            {
                newPacket.Destination = i;
                Send(newPacket);
            }
        }
    }
}
