using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PSI.EpicsClient2;
using CaSharpServer;
using System.Net;
using PBCaGw;
using System.Threading;
using System.Diagnostics;
using PBCaGw.Services;
using System.Collections.Generic;
using System.Linq;

namespace GWTests
{
    [TestClass]
    public class GWChecks
    {
        EpicsClient clientA,clientB;
        CAServer server;
        Gateway gw;

        [TestInitialize]
        public void Init()
        {
            clientA = new EpicsClient();
            clientA.Configuration.SearchAddress = "129.129.130.44:7500";
            clientA.Configuration.WaitTimeout = 1000;

            clientB = new EpicsClient();
            clientB.Configuration.SearchAddress = "129.129.130.44:7500";
            clientB.Configuration.WaitTimeout = 1000;

            server = new CAServer(IPAddress.Parse("129.129.130.44"), 7100, 7100, 7101);

            gw = new Gateway();
            gw.Configuration.LocalAddressSideA = "129.129.130.44:7500";
            gw.Configuration.RemoteAddressSideA = "129.129.130.44:7501";
            gw.Configuration.RemoteAddressSideB = "129.129.130.44:7100";
            gw.Configuration.LocalAddressSideB = "129.129.130.44:9001";
            gw.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gw.Start();

        }

        [TestCleanup]
        public void Cleanup()
        {
            gw.Dispose();
            clientA.Dispose();
            clientB.Dispose();
            server.Dispose();
        }

        [TestMethod]
        public void Get()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;


            var channel = clientA.CreateChannel<double>("GWTEST:DBL");

            Assert.AreEqual(1.0, channel.Get());
        }

        [TestMethod]
        public void Monitor()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;


            var channel = clientA.CreateChannel<double>("GWTEST:DBL");
            AutoResetEvent evt = new AutoResetEvent(false);
            double lastVal = -1;
            channel.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                lastVal = newValue;
                evt.Set();
            };
            evt.WaitOne(500);
            Assert.AreEqual(1.0, lastVal);
        }

        [TestMethod]
        public void Put()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;


            var channel = clientA.CreateChannel<double>("GWTEST:DBL");
            channel.Put(2);
            Assert.AreEqual(2.0, pv.Value);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "Connection timeout.")]
        public void NotFound()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;


            var channel = clientA.CreateChannel<double>("GWTEST:DBL2");
            channel.Get();
        }

        [TestMethod]
        public void MultipleGet()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;


            var channel = clientA.CreateChannel<double>("GWTEST:DBL");

            for (var i = 0; i < 10; i++)
                Assert.AreEqual(1.0, channel.Get());
        }

        [TestMethod]
        public void TwoMonitors()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;

            var channelA = clientA.CreateChannel<double>("GWTEST:DBL");
            AutoResetEvent evtA = new AutoResetEvent(false);
            double lastValA = -1;
            channelA.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                lastValA = newValue;
                evtA.Set();
            };

            var channelB = clientB.CreateChannel<double>("GWTEST:DBL");
            AutoResetEvent evtB = new AutoResetEvent(false);
            double lastValB = -1;
            channelB.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                lastValB = newValue;
                evtB.Set();
            };

            evtA.WaitOne(500);
            evtB.WaitOne(500);
            Assert.AreEqual(1.0, lastValA);
            Assert.AreEqual(1.0, lastValB);
        }

        [TestMethod]
        public void TwoMonitorsDelayed()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;

            var channelA = clientA.CreateChannel<double>("GWTEST:DBL");
            AutoResetEvent evtA = new AutoResetEvent(false);
            double lastValA = -1;
            channelA.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                lastValA = newValue;
                evtA.Set();
            };
            evtA.WaitOne(500);

            var channelB = clientB.CreateChannel<double>("GWTEST:DBL");
            AutoResetEvent evtB = new AutoResetEvent(false);
            double lastValB = -1;
            channelB.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                lastValB = newValue;
                evtB.Set();
            };

            evtB.WaitOne(500);
            Assert.AreEqual(1.0, lastValA);
            Assert.AreEqual(1.0, lastValB);
        }

        [TestMethod]
        public void TwoMonitorsConstant()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;
            pv.Scan = CaSharpServer.Constants.ScanAlgorithm.HZ5;
            pv.PrepareRecord += delegate(object obj, EventArgs e)
            {
                pv.Value += 0.1;
            };

            var channelA = clientA.CreateChannel<double>("GWTEST:DBL");
            double lastValA = -1;
            int nbChangesA = 0;
            channelA.StatusChanged += delegate(EpicsChannel sender, ChannelStatus newStatus)
            {
                nbChangesA++;
            };
            channelA.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                lastValA = newValue;
            };

            Thread.Sleep(200);

            var channelB = clientB.CreateChannel<double>("GWTEST:DBL");
            double lastValB = -1;
            int nbChangesB = 0;
            channelB.StatusChanged += delegate(EpicsChannel sender, ChannelStatus newStatus)
            {
                nbChangesB++;
            };
            channelB.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                lastValB = newValue;
            };

            Thread.Sleep(15000);
            Assert.AreEqual(1, nbChangesA);
            Assert.AreEqual(1, nbChangesB);
        }

        [TestMethod]
        public void MultipleUpdates()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;

            var channelA = clientA.CreateChannel<double>("GWTEST:DBL");
            AutoResetEvent evtA = new AutoResetEvent(false);
            int nbChangesA = 0;
            channelA.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                nbChangesA++;
                if(nbChangesA == 10)
                    evtA.Set();
            };

            var channelB = clientB.CreateChannel<double>("GWTEST:DBL");
            int nbChangesB = 0;
            channelB.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                pv.Value += 0.1;
                nbChangesB++;
            };


            evtA.WaitOne();

            Assert.AreEqual(10, nbChangesA);
            Assert.AreEqual(10, nbChangesB);
        }

        [TestMethod]
        public void ReconnectServerMonitor()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;


            var channel = clientA.CreateChannel<double>("GWTEST:DBL");
            AutoResetEvent evt = new AutoResetEvent(false);
            double lastVal = -1;
            int stateChange = 0;
            channel.StatusChanged += delegate(EpicsChannel sender, ChannelStatus newStatus)
            {
                stateChange++;
            };
            channel.MonitorChanged += delegate(EpicsChannel<double> sender, double newValue)
            {
                lastVal = newValue;
                evt.Set();
            };
            evt.WaitOne(500);

            server.Dispose();
            server = new CAServer(IPAddress.Parse("129.129.130.44"), 7100, 7100, 7101);
            pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 2;

            evt.WaitOne(5000);
            Assert.AreEqual(3, stateChange);
            Assert.AreEqual(2.0, lastVal);
        }

        [TestMethod]
        public void CheckSearch()
        {
            Log.ShowAll = true;
            DebugTraceListener.TraceAll = true;

            List<string> messages = new List<string>();
            DebugTraceListener.LogEntry += delegate(string source, TraceEventType eventType, int chainId, string message)
            {
                messages.Add(message);
            };


            var channel = clientA.CreateChannel<double>("GWTEST:DBL");
            try
            {
                channel.Connect();
            }
            catch
            {
            }

            Assert.AreEqual(true, messages.Where(row=>row.Contains("Search") && row.EndsWith("GWTEST:DBL")).Count() > 0);
        }

        [TestMethod]
        public void CheckCachedSearch()
        {
            var pv = server.CreateRecord<CADoubleRecord>("GWTEST:DBL");
            pv.Value = 1;

            Log.ShowAll = true;
            DebugTraceListener.TraceAll = true;

            List<string> messages = new List<string>();
            DebugTraceListener.LogEntry += delegate(string source, TraceEventType eventType, int chainId, string message)
            {
                messages.Add(message);
            };


            var channel = clientA.CreateChannel<double>("GWTEST:DBL");
            try
            {
                channel.Connect();
            }
            catch
            {
            }

            var channelb = clientB.CreateChannel<double>("GWTEST:DBL");
            try
            {
                channelb.Connect();
            }
            catch
            {
            }

            Assert.AreEqual(true, messages.Where(row => row.Contains("Cached responce")).Count() > 0 && messages.Where(row => row.Contains("Cached search")).Count() > 0);
        }
    }
}
