using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using System.Net.Sockets;

namespace PBCaGw.Configurations
{
    /// <summary>
    /// Stores gateway configuration
    /// </summary>
    [Serializable]
    [XmlRoot("Config", IsNullable = false)]
    public class Configuration
    {
        public Configuration()
        {
            Security = new Security();
        }
        [XmlIgnore]
        IPEndPoint localSideA = null;
        [XmlIgnore]
        public IPEndPoint LocalSideA { get { return localSideA ?? (localSideA = ParseAddress(LocalAddressSideA)); } }
        [XmlIgnore]
        public List<IPEndPoint> RemoteSideA { get { return ParseListAddress(RemoteAddressSideA); } }
        [XmlIgnore]
        IPEndPoint localSideB = null;
        [XmlIgnore]
        public IPEndPoint LocalSideB { get { return localSideB ?? (localSideB = ParseAddress(LocalAddressSideB)); } }
        [XmlIgnore]
        public List<IPEndPoint> RemoteSideB { get { return ParseListAddress(RemoteAddressSideB); } }
        [XmlElement("Type")]
        public ConfigurationType ConfigurationType { get; set; }
        [XmlElement("Name")]
        public string GatewayName { get; set; }

        [XmlIgnore]
        private string localAddressSideA = null;
        [XmlElement("LocalAddressSideA")]
        public string LocalAddressSideA { get { return localAddressSideA; } set { localAddressSideA = value; localSideA = null; } }
        /// <summary>
        /// Used for bi-directional and beacon messages
        /// </summary>
        [XmlElement("RemoteAddressSideA")]
        public string RemoteAddressSideA { get; set; }

        [XmlIgnore]
        private string localAddressSideB = null;
        [XmlElement("LocalAddressSideB")]
        public string LocalAddressSideB { get { return localAddressSideB; } set { localAddressSideB = value; localSideB = null; } }
        /// <summary>
        /// Used for bi-directional and beacon messages
        /// </summary>
        [XmlElement("RemoteAddressSideB")]
        public string RemoteAddressSideB { get; set; }

        /*[XmlElement("UdpSenderPortA")]
        public int UdpSenderPortA { get; set; }

        [XmlElement("UdpSenderPortB")]
        public int UdpSenderPortB { get; set; }*/

        [XmlElement("Security")]
        public Security Security { get; set; }

        static public IPEndPoint ParseAddress(string addr)
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
                    PBCaGw.Services.Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, -1, "Wrong IP: " + addr);
                    throw ex2;
                }
            }
        }

        static private List<IPEndPoint> ParseListAddress(string list)
        {
            return list
                .Replace(";", ",")
                .Split(new char[] { ',' })
                .Select(ParseAddress)
                .ToList();
        }

        // Found on http://stackoverflow.com/questions/5199026/c-sharp-async-udp-listener-socketexception
        // Allows to reset the socket in case of malformed UDP packet.
        const int SioUdpConnReset = -1744830452;

        [XmlIgnore]
        Socket udpReceiverSocketA = null;
        [XmlIgnore]
        public Socket UdpReceiverA
        {
            get
            {
                if (udpReceiverSocketA == null)
                {
                    udpReceiverSocketA = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    udpReceiverSocketA.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpReceiverSocketA.IOControl(SioUdpConnReset, new byte[] { 0, 0, 0, 0 }, null);
                    udpReceiverSocketA.Bind(LocalSideA);
                }
                return udpReceiverSocketA;
            }
            internal set
            {
                udpReceiverSocketA = value;
            }
        }

        [XmlIgnore]
        Socket udpReceiverSocketB = null;
        [XmlIgnore]
        public Socket UdpReceiverB
        {
            get
            {
                if (udpReceiverSocketB == null)
                {
                    udpReceiverSocketB = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    udpReceiverSocketB.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpReceiverSocketB.IOControl(SioUdpConnReset, new byte[] { 0, 0, 0, 0 }, null);
                    udpReceiverSocketB.Bind(LocalSideB);
                }
                return udpReceiverSocketB;
            }
            internal set
            {
                udpReceiverSocketB = value;
            }
        }

    }
}
