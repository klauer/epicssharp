using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NameServer
{
    public class NameServer
    {
        public const int BUFFER_SIZE = 8192 * 30;
        public const UInt16 CA_PROTO_VERSION = 11;

        UdpReceiver udpReceiver;
        readonly internal NameCache Cache;
        readonly internal ServerCache Servers;
        readonly internal IdCache IdCache;
        public int Port { get; set; }
        public IPAddress BindingAddress { get; set; }

        public string SearchAddress = "255.255.255.255:5064";
        List<IPEndPoint> dests = new List<IPEndPoint>();

        public string ClusterPrefix { get; set; }

        public int NodeId { get; set; }

        public int NodesInCluster { get; set; }

        /// <summary>
        /// Wakes up every 10 sec
        /// </summary>
        public event EventHandler TenSecJobs;
        /// <summary>
        /// Wakes up every 5 sec
        /// </summary>
        public event EventHandler FiveSecJobs;
        /// <summary>
        /// Wakes up every sec
        /// </summary>
        public event EventHandler OneSecJobs;
        bool isRunning = false;

        DebugServer debugServer;

        Thread bgJobs;

        public NameServer()
        {
            this.Cache = new NameCache(this);
            this.IdCache = new IdCache(this);
            this.Servers = new ServerCache(this);


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
                    Log.Write(System.Diagnostics.TraceEventType.Critical, "Wrong IP: " + addr);
                    throw ex2;
                }
            }
        }


        public void Start()
        {
            udpReceiver = new UdpReceiver(this, BindingAddress, Port);
            dests = (SearchAddress + ";" + BindingAddress + ":7654").Replace(" ", ";").Replace(",", ";")
                .Split(new char[] { ';' })
                .Select(row => ParseAddress(row))
                .ToList();

            Log.Write(System.Diagnostics.TraceEventType.Start, "Starting Name Service on " + BindingAddress + ":" + Port);
            if (!string.IsNullOrEmpty(ClusterPrefix))
            {
                Log.Write(System.Diagnostics.TraceEventType.Start, "Cluster " + ClusterPrefix);
                Log.Write(System.Diagnostics.TraceEventType.Start, "We are node " + NodeId + " of " + NodesInCluster);
            }

            isRunning = true;
            bgJobs = new Thread(RunBgJobs);
            bgJobs.IsBackground = true;
            bgJobs.Start();

            udpReceiver.Start();

            debugServer = new DebugServer(BindingAddress);
        }

        public void Stop()
        {
            Log.Write(System.Diagnostics.TraceEventType.Stop, "Stopping name saver");

            debugServer.Dispose();
            isRunning = false;
            udpReceiver.Stop();
            Servers.StopAll();
            this.IdCache.Clear();
            this.Cache.Clear();
        }

        void RunBgJobs()
        {
            int jobCounter = 0;
            while (isRunning)
            {
                Thread.Sleep(1000);
                if (jobCounter == 10)
                {
                    ThreadPool.QueueUserWorkItem(RunTenSecJob);
                    jobCounter = 0;
                }
                if (jobCounter % 5 == 0 && FiveSecJobs != null)
                    ThreadPool.QueueUserWorkItem(RunFiveSecJob);

                ThreadPool.QueueUserWorkItem(RunOneSecJob);
                jobCounter++;
            }
            // ReSharper disable FunctionNeverReturns
        }

        void RunTenSecJob(object state)
        {
            /*if(Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "10 sec job running");*/
            if (TenSecJobs != null)
            {
                foreach (var i in TenSecJobs.GetInvocationList())
                {
                    bool faulty = false;
                    for (var n = 0; n < 5; n++)
                    {
                        try
                        {
                            i.Method.Invoke(i.Target, new object[] { null, null });
                            faulty = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Write(System.Diagnostics.TraceEventType.Critical, ex.Message + "\r\n" + ex.StackTrace);
                            faulty = true;
                            Thread.Sleep(500);
                        }
                    }
                    if (faulty)
                        Environment.Exit(1);
                }
                //TenSecJobs(null, null);
            }
            /*if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "10 sec job done");*/
        }

        void RunFiveSecJob(object state)
        {
            /*if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "5 sec job running");*/
            if (FiveSecJobs != null)
            {
                foreach (var i in FiveSecJobs.GetInvocationList())
                {
                    bool faulty = false;
                    for (var n = 0; n < 5; n++)
                    {
                        try
                        {

                            i.Method.Invoke(i.Target, new object[] { null, null });
                            faulty = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Write(System.Diagnostics.TraceEventType.Critical, ex.Message + "\r\n" + ex.StackTrace);
                            faulty = true;
                            Thread.Sleep(500);
                        }
                    }
                    if (faulty)
                        Environment.Exit(1);
                }
            }
            /*if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "5 sec job done");*/
        }

        void RunOneSecJob(object state)
        {
            /*if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(TraceEventType.Verbose, -1, "1 sec job running");*/
            if (OneSecJobs != null)
            {
                foreach (var i in OneSecJobs.GetInvocationList())
                {
                    bool faulty = false;
                    for (var n = 0; n < 5; n++)
                    {
                        try
                        {
                            i.Method.Invoke(i.Target, new object[] { null, null });
                            faulty = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Write(System.Diagnostics.TraceEventType.Critical, ex.Message + "\r\n" + ex.StackTrace);
                            faulty = true;
                            Thread.Sleep(500);
                        }
                    }
                    if (faulty)
                        Environment.Exit(1);
                }
            }
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
