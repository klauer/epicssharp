using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace GatewayDebugData
{
    public delegate void RefreshAllDelegate(DebugContext ctx);
    public delegate void NewConnectionChannelDelegate(DebugContext ctx, string host, string channel);
    public delegate void DropConnectionDelegate(DebugContext ctx, string host);
    public delegate void ContextConnectionDelegate(DebugContext ctx, System.Data.ConnectionState state);
    public delegate void DebugLogDelegate(string source, TraceEventType eventType, int chainId, string message);
    public delegate void NewGatewayNameDelegate(DebugContext ctx, string name);
    public delegate void DebugLevelDelegate(DebugContext ctx, bool fullLogs);

    public class DebugContext : IDebugDataAccess, IDisposable
    {
        IPEndPoint server;
        Socket socket;
        NetworkStream stream;
        bool isRunning = true;
        bool isDisposed = false;
        bool isConnected = false;
        Thread collectThread, connectThread;
        BufferedStream sendStream;
        string hostName;

        readonly ConnectionDataCollection iocs;
        public ConnectionDataCollection Iocs { get { return iocs; } }
        readonly ConnectionDataCollection clients;
        public ConnectionDataCollection Clients { get { return clients; } }

        public event RefreshAllDelegate RefreshAllIocs;
        public event NewConnectionChannelDelegate NewIocChannel;
        public event DropConnectionDelegate DropIoc;

        public event RefreshAllDelegate RefreshAllClients;
        public event NewConnectionChannelDelegate NewClientChannel;
        public event DropConnectionDelegate DropClient;
        public event NewGatewayNameDelegate NewName;

        public event DebugLogDelegate DebugLog;
        public event DebugLevelDelegate DebugLevel;

        public event ContextConnectionDelegate ConnectionState;

        public string GatewayName { get; private set; }

        public DebugContext(string host)
        {
            iocs = new ConnectionDataCollection(this);
            clients = new ConnectionDataCollection(this);

            string[] p = host.Split(':');

            IPAddress address;
            try
            {
                address = IPAddress.Parse(p[0]);
            }
            catch
            {
                try
                {
                    address = Dns.GetHostEntry(p[0]).AddressList.First();
                }
                catch
                {
                    return;
                }
            }

            int port = 5064;
            if (p.Length > 1)
                port = int.Parse(p[1]);
            hostName = p[0];

            server = new IPEndPoint(address, port);


            connectThread = new Thread(Reconnect);
            connectThread.IsBackground = true;
            connectThread.Start();

            collectThread = new Thread(HandleData);
            collectThread.IsBackground = true;
            collectThread.Start();
        }

        void Reconnect()
        {
            while (isRunning)
            {
                if (!isConnected)
                {
                    try
                    {
                        Connect();
                        isConnected = true;
                        if (ConnectionState != null)
                            ConnectionState(this, System.Data.ConnectionState.Open);
                    }
                    catch
                    {
                    }
                }
                Thread.Sleep(500);
            }
        }

        void Connect()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(server);

            stream = new NetworkStream(socket);
            sendStream = new BufferedStream(stream);

            Send(new byte[] { 126 });
            Send(hostName);
            Flush();

            // Skip the version
            byte[] toSkip=new byte[16];
            stream.Read(toSkip, 0, 16);
        }

        void Flush()
        {
            sendStream.Flush();
        }

        int nbBytesRead = 0;

        bool fullLogs = false;
        public bool FullLogs
        {
            get
            {
                return fullLogs;
            }
            set
            {
                fullLogs = value;
                if (fullLogs)
                    Send((int)DebugDataType.FULL_LOGS);
                else
                    Send((int)DebugDataType.CRITICAL_LOGS);
                Flush();
            }
        }

        void HandleData()
        {
            while (isRunning)
            {
                if (!isConnected)
                {
                    Thread.Sleep(500);
                    continue;
                }

                try
                {                    
                    switch ((DebugDataType)GetInt())
                    {
                        case DebugDataType.FULL_IOC:
                            {
                                iocs.GetAll();
                                if (RefreshAllIocs != null)
                                    RefreshAllIocs(this);
                                break;
                            }
                        case DebugDataType.IOC_NEW_CHANNEL:
                            {
                                string ioc = GetString();
                                string channel = iocs.GetByName(ioc).AddChannel();
                                if (NewIocChannel != null)
                                    NewIocChannel(this, ioc, channel);
                                break;
                            }
                        case DebugDataType.DROP_IOC:
                            {
                                string ioc = GetString();
                                iocs.DropByName(ioc);
                                if (DropIoc != null)
                                    DropIoc(this, ioc);
                                break;
                            }
                        case DebugDataType.DROP_CLIENT:
                            {
                                string client = GetString();
                                clients.DropByName(client);
                                if (DropClient != null)
                                    DropClient(this, client);
                                break;
                            }
                        case DebugDataType.CLIENT_NEW_CHANNEL:
                            {
                                string client = GetString();
                                string channel = clients.GetByName(client).AddChannel();
                                if (NewClientChannel != null)
                                    NewClientChannel(this, client, channel);
                                break;
                            }
                        case DebugDataType.FULL_CLIENT:
                            {
                                clients.GetAll();
                                if (RefreshAllClients != null)
                                    RefreshAllClients(this);
                                break;
                            }
                        case DebugDataType.LOG:
                            {
                                string source = GetString();
                                TraceEventType eventType = (TraceEventType)GetInt();
                                int chainId = GetInt();
                                string message = GetString();

                                if (DebugLog != null)
                                    DebugLog(source, eventType, chainId, message);
                                break;
                            }
                        case DebugDataType.GW_NAME:
                            {
                                GatewayName = GetString();
                                if (NewName != null)
                                    NewName(this, GatewayName);
                                break;
                            }
                        case DebugDataType.FULL_LOGS:
                            fullLogs = true;
                            if (DebugLevel != null)
                                DebugLevel(this, fullLogs);
                            break;
                        case DebugDataType.CRITICAL_LOGS:
                            fullLogs = false;
                            if (DebugLevel != null)
                                DebugLevel(this, fullLogs);
                            break;
                    }
                }
                catch (Exception)
                {
                    Disconnect();
                }
            }
        }

        public int GetInt()
        {
            byte[] data = new byte[4];
            stream.Read(data, 0, 4);
            return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
        }

        public string GetString()
        {
            int lenght = GetInt();
            byte[] data = new byte[lenght];
            stream.Read(data, 0, lenght);
            return Encoding.UTF8.GetString(data);
        }

        public void Send(int data)
        {
            byte[] buff = new byte[4];
            buff[0] = (byte)((data & 0xFF000000u) >> 24);
            buff[1] = (byte)((data & 0x00FF0000u) >> 16);
            buff[2] = (byte)((data & 0x0000FF00u) >> 8);
            buff[3] = (byte)(data & 0x000000FFu);
            Send(buff);
        }

        public void Send(string data)
        {
            byte[] buff = System.Text.Encoding.UTF8.GetBytes(data);
            Send(buff.Length);
            Send(buff);
        }

        public void Send(byte[] data)
        {
            try
            {
                sendStream.Write(data, 0, data.Length);
            }
            catch
            {
                Disconnect();
            }
        }

        void Disconnect()
        {
            try
            {
                sendStream.Dispose();
            }
            catch
            {
            }
            try
            {
                socket.Close();
            }
            catch
            {
            }
            try
            {
                socket.Dispose();
            }
            catch
            {
            }

            isConnected = false;
            if (ConnectionState != null)
                ConnectionState(this, System.Data.ConnectionState.Closed);
            iocs.Clear();
            if (RefreshAllIocs != null)
                RefreshAllIocs(this);
            if (RefreshAllClients != null)
                RefreshAllClients(this);
        }

        public void Dispose()
        {
            if (isDisposed)
                return;
            Disconnect();
            isDisposed = true;
            isRunning = false;
            try
            {
                socket.Close();
            }
            catch
            {
            }
            try
            {
                socket.Dispose();
            }
            catch
            {
            }
        }

    }
}
