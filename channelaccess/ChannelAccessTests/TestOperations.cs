﻿using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChannelAccessTests
{
    [TestClass]
    public class TestOperations
    {
        CAServer server;
        CAClient client;
        CADoubleRecord[] records;

        [TestInitialize]
        public void SetUp()
        {
            client = new CAClient();
            client.Configuration.SearchAddress = "127.0.0.1";
            client.Configuration.WaitTimeout = 500;  // .5 seconds

            serverInit();
        }

        void serverInit()
        {
            server = new CAServer(IPAddress.Parse("127.0.0.1"));
            records = new CADoubleRecord[10];
            for (var i = 0; i < 10; i++)
            {
                records[i] = server.CreateRecord<CADoubleRecord>("TEST:DBL:" + i);

                records[i].Scan = EpicsSharp.ChannelAccess.Constants.ScanAlgorithm.HZ10;
                records[i].LowAlarmLimit = 25;
                records[i].LowAlarmSeverity = EpicsSharp.ChannelAccess.Constants.AlarmSeverity.MINOR;
                records[i].LowLowAlarmLimit = 20;
                records[i].LowLowAlarmSeverity = EpicsSharp.ChannelAccess.Constants.AlarmSeverity.MAJOR;
                records[i].HighAlarmLimit = 100;
                records[i].HighAlarmSeverity = EpicsSharp.ChannelAccess.Constants.AlarmSeverity.MINOR;
                records[i].HighHighAlarmLimit = 105;
                records[i].HighHighAlarmSeverity = EpicsSharp.ChannelAccess.Constants.AlarmSeverity.MAJOR;
                records[i].EngineeringUnits = "My";
                /*AutoResetEvent waitOne = new AutoResetEvent(false);
                records[i].RecordProcessed += delegate(object obj, EventArgs args)
                {
                    waitOne.Set();
                };
                records[i].Value = 10;
                waitOne.WaitOne();*/
                records[i].Value = 10;
            }
        }

        [TestCleanup]
        public void TearDown()
        {
            server.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void TestGet()
        {
            var c = client.CreateChannel("TEST:DBL:5");
            var d = c.Get<double>();
            Assert.AreEqual(10, d);
        }

        [TestMethod]
        public void TestCheckInfo()
        {
            var c = client.CreateChannel("TEST:DBL:5");
            c.Connect();
            Assert.AreEqual(typeof(double), c.ChannelDefinedType);
        }

        [TestMethod]
        public void TestGetAsync()
        {
            var c = client.CreateChannel("TEST:DBL:5");
            var d = c.GetAsync<double>();
            d.Wait();
            Assert.AreEqual(10, d.Result);
        }

        [TestMethod]
        public void TestPut()
        {
            var c = client.CreateChannel("TEST:DBL:5");
            c.Put<double>(50);
            Assert.AreEqual(50, records[5].Value);
            AutoResetEvent waitOne = new AutoResetEvent(false);
            records[5].RecordProcessed += delegate(object obj, EventArgs args)
            {
                waitOne.Set();
            };
            waitOne.WaitOne();
            Assert.AreEqual(EpicsSharp.ChannelAccess.Constants.AlarmStatus.NO_ALARM, records[5].AlarmStatus);
        }

        [TestMethod]
        public void MultiGet()
        {
            List<Channel> channels = new List<Channel>();
            for (var i = 0; i < 10; i++)
                channels.Add(client.CreateChannel("TEST:DBL:" + i));
            var res = client.MultiGet<double>(channels);
            for (var i = 0; i < 10; i++)
                Assert.AreEqual(10.0, res[i]);
        }

        [TestMethod]
        public void MultiConnect()
        {
            List<Channel> channels = new List<Channel>();
            for (var i = 0; i < 10; i++)
                channels.Add(client.CreateChannel("TEST:DBL:" + i));
            client.MultiConnect(channels);
            for (var i = 0; i < 10; i++)
                Assert.AreEqual(EpicsSharp.ChannelAccess.Constants.ChannelStatus.CONNECTED, channels[i].Status);
        }

        [TestMethod]
        public void Monitor()
        {
            var channel = client.CreateChannel("TEST:DBL:5");
            AutoResetEvent waitOne = new AutoResetEvent(false);
            double findValue=0;
            channel.MonitorChanged += delegate(Channel sender, object newValue)
            {
                findValue = (double)newValue;
                waitOne.Set();
            };
            waitOne.WaitOne();
            Assert.AreEqual(10.0, findValue);
        }

        [TestMethod]
        public void MonitorSequence()
        {
            var channel = client.CreateChannel("TEST:DBL:5");
            AutoResetEvent waitOne = new AutoResetEvent(false);
            double findValue = 0;
            channel.MonitorChanged += delegate(Channel sender, object newValue)
            {
                findValue = (double)newValue;
                waitOne.Set();
            };
            waitOne.WaitOne();
            records[5].Value = 2;
            waitOne.WaitOne();
            records[5].Value = 3;
            waitOne.WaitOne();
            records[5].Value = 1;
            waitOne.WaitOne();
            Assert.AreEqual(1.0, findValue);
        }

        [TestMethod]
        public void DisconnectReconnect()
        {
            var channel = client.CreateChannel("TEST:DBL:5");
            AutoResetEvent waitOne = new AutoResetEvent(false);
            double findValue = 0;
            channel.MonitorChanged += delegate(Channel sender, object newValue)
            {
                findValue = (double)newValue;
                waitOne.Set();
            };
            waitOne.WaitOne();
            records[5].Value = 2;
            findValue = 0;
            server.Dispose();
            Thread.Sleep(100);
            serverInit();
            waitOne.WaitOne();
            Assert.AreEqual(10.0, findValue);
        }
    }
}