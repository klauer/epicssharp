using System;
using System.Collections.Generic;
using System.Linq;
using CaSharpServer;
using System.Net;
using PSI.EpicsClient2;
using System.Threading;
using PBCaGw;
using System.Diagnostics;
using System.IO;
using PBCaGw.Workers;

namespace MultiGetTest
{
    class Program
    {
        private static CAIntRecord[] intRecords = new CAIntRecord[1000];
        private static Random rnd = new Random();
        private static CAServer server;
        private static EpicsClient client;
        private static Gateway gateway;

        //const string ip = "127.0.0.1";
        const string ip = "129.129.130.44";
        const string remoteIp = "127.0.0.1";

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);



            /*// Setup the console look
            try
            {
                Console.Title = "Test gateway and client";
                Console.WindowWidth = 120;
                Console.BufferWidth = 120;
                Console.WindowHeight = 60;
                Console.BufferHeight = 3000;
            }
            catch
            {
            }*/

            //QuickMonitor();

            //SimpleServer();

            //MultipleCreate();
            //SplitTest
            //ZTest();
            ReconnectIoc();
            //MonAll();
            //ProscanWave();
            //ReconnectProscan();
        }

        static bool needToShow = true;

        private static void QuickMonitor()
        {
            gateway = new Gateway();
            gateway.Configuration.GatewayName = "TESTGW";
            gateway.Configuration.LocalAddressSideA = "127.0.0.1:5432";
            //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
            gateway.Configuration.RemoteAddressSideA = "127.0.0.1:5552";
            gateway.Configuration.LocalAddressSideB = "129.129.130.87:5064";
            //gateway.Configuration.LocalAddressSideB = "172.22.200.116:5432";
            //gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
            //gateway.Configuration.RemoteAddressSideB = "172.22.255.255:5064";
            gateway.Configuration.RemoteAddressSideB = "172.22.200.117:5062";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gateway.SaveConfig();
            gateway.Start();

            Thread.Sleep(1000);

            EpicsClient client = new EpicsClient();
            string clientConfig = "127.0.0.1:5432";
            //string clientConfig="172.22.100.101:5064";
            client.Configuration.SearchAddress = clientConfig;

            //string[] channelNames = { "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL", "ZPSAF101-VME:CALNCONN.EGU", "ZPSAF101-VME:CALDCONN.EGU", "ZPSAF101-VME:LOAD", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:HBT" };
            //string[] channelNames = { "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CALNCONN.EGU", "ZPSAF101-VME:CALDCONN.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU" };
            string[] channelNames = { "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CALNCONN.EGU", "ZPSAF101-VME:CALDCONN.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:CAL.EGU", "ZPSAF101-VME:HBT.EGU", "ZPSAF101-VME:HBT.EGU", "ZPSAF101-VME:LOAD.EGU" };
            //string[] channelNames = { "ZPSAF101-VME:CAL.EGU" };
            while (true)
            {
                for (int l = 0; l < 10; l++)
                {
                    List<EpicsChannel<ExtGraphic<string>>> channels = new List<EpicsChannel<ExtGraphic<string>>>();
                    foreach (var i in channelNames)
                    {
                        EpicsChannel<ExtGraphic<string>> c = client.CreateChannel<ExtGraphic<string>>(i);
                        c.MonitorChanged += new EpicsDelegate<ExtGraphic<string>>(QuickMonitor);
                        channels.Add(c);
                    }
                    foreach (var i in channels)
                        i.Dispose();
                    client.Dispose();
                    client = new EpicsClient();
                    client.Configuration.SearchAddress = clientConfig;
                }

                if (true)
                {
                    bool gotError = false;
                    List<EpicsChannel<ExtGraphic<string>>> channels = new List<EpicsChannel<ExtGraphic<string>>>();
                    foreach (var i in channelNames)
                    {
                        EpicsChannel<ExtGraphic<string>> c = client.CreateChannel<ExtGraphic<string>>(i);
                        channels.Add(c);
                    }

                    CountdownEvent multiActionCountDown = new CountdownEvent(channelNames.Count());
                    Dictionary<uint, bool> gotMonitor = new Dictionary<uint, bool>();

                    foreach (EpicsChannel<ExtGraphic<string>> c in channels)
                    {
                        c.MonitorChanged += delegate(EpicsChannel<ExtGraphic<string>> sender, ExtGraphic<string> newValue)
                        {
                            if ((newValue.Value != "#" && sender.ChannelName.Contains(":CAL")) ||
                                (newValue.Value != "%" && sender.ChannelName.Contains(":LOAD")) ||
                                (newValue.Value != "ticks" && sender.ChannelName.Contains(":HBT")))
                            {
                                Console.WriteLine("Wrong data on CID (" + sender.ChannelName + "): " + sender.CID + ", " + newValue.Value);
                                gotError = true;
                            }
                            lock (gotMonitor)
                            {
                                if (!gotMonitor.ContainsKey(sender.CID))
                                {
                                    gotMonitor.Add(sender.CID, true);
                                    multiActionCountDown.Signal();
                                }
                            }
                        };

                        c.MonitorChanged += new EpicsDelegate<ExtGraphic<string>>(QuickMonitor);
                    }

                    multiActionCountDown.Wait(2500);

                    bool allConnected = true;
                    foreach (var i in channels)
                    {
                        if (i.Status != ChannelStatus.CONNECTED)
                        {
                            allConnected = false;
                            Console.WriteLine(i.ChannelName + " not connected.");
                            //gotError = true;
                        }
                    }

                    if (!allConnected)
                    {
                        needToShow = false;
                        //gotError = true;
                        Console.WriteLine("Not all connected!!!!!");
                    }

                    foreach (var i in channels)
                        i.Dispose();
                    client.Dispose();
                    client = new EpicsClient();
                    client.Configuration.SearchAddress = clientConfig;
                    multiActionCountDown.Dispose();

                    if (gotError)
                    {
                        Console.Beep();
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        needToShow = true;
                    }
                }
            }
        }

        static void QuickMonitor(EpicsChannel<ExtGraphic<string>> sender, ExtGraphic<string> newValue)
        {
            /*if (needToShow)
                Console.WriteLine(sender.ChannelName + ": " + newValue);*/
        }

        private static void SimpleServer()
        {
            Console.WindowWidth = 120;
            Console.BufferWidth = 120;
            Console.WindowHeight = 60;
            Console.BufferHeight = 3000;

            server = new CAServer(IPAddress.Parse(ip), 5777, 5777);
            CAIntRecord record = server.CreateRecord<CAIntRecord>("MXI1:ILOG:2");
            record.Value = 5;

            gateway = new Gateway();
            gateway.Configuration.GatewayName = "TESTGW";
            gateway.Configuration.LocalAddressSideA = ip + ":5432";
            //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
            gateway.Configuration.RemoteAddressSideA = ip + ":5552";
            gateway.Configuration.LocalAddressSideB = ip + ":5888";
            //gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
            gateway.Configuration.RemoteAddressSideB = ip + ":5777";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gateway.SaveConfig();

            Gateway.AutoCreateChannel = false;
            Gateway.RestoreCache = false;
            gateway.Start();
            Console.ReadKey();
            gateway.Dispose();
        }

        private static void MultipleCreate()
        {
            Console.WindowWidth = 120;
            Console.BufferWidth = 120;
            Console.WindowHeight = 60;
            Console.BufferHeight = 3000;

            server = new CAServer(IPAddress.Parse(ip), 5777, 5777);
            CAStringRecord record = server.CreateRecord<CAStringRecord>("PCTEST:STR");
            record.Value = "Hello there!";

            gateway = new Gateway();
            gateway.Configuration.GatewayName = "TESTGW";
            gateway.Configuration.LocalAddressSideA = ip + ":5555";
            //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
            gateway.Configuration.RemoteAddressSideA = ip + ":5552";
            gateway.Configuration.LocalAddressSideB = ip + ":5888";
            //gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
            gateway.Configuration.RemoteAddressSideB = ip + ":5777";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gateway.SaveConfig();

            Gateway.AutoCreateChannel = false;
            Gateway.RestoreCache = false;
            gateway.Start();

            Thread.Sleep(2000);

            EpicsClient c1 = new EpicsClient();
            c1.Configuration.SearchAddress = ip + ":5555";
            EpicsClient c2 = new EpicsClient();
            c2.Configuration.SearchAddress = ip + ":5555";

            EpicsChannel ca1 = c1.CreateChannel("PCTEST:STR");
            EpicsChannel ca2 = c2.CreateChannel("PCTEST:STR");

            Console.WriteLine("Reading...");

            ca1.MonitorChanged += new EpicsDelegate(ca1_MonitorChanged);
            ca2.MonitorChanged += new EpicsDelegate(ca2_MonitorChanged);

            Console.ReadKey();
        }

        static void ca1_MonitorChanged(EpicsChannel sender, object newValue)
        {
            Console.WriteLine("Ca1: " + newValue);
        }

        static void ca2_MonitorChanged(EpicsChannel sender, object newValue)
        {
            Console.WriteLine("Ca2: " + newValue);
        }

        static void SplitTest()
        {
            PacketSplitter splitter = new PacketSplitter();
            splitter.ReceiveData += new ReceiveDataDelegate(splitter_ReceiveData);

            byte[] data = new byte[4096];
            for (int i = 0; i < data.Length; i++)
                data[i] = 1;
            PBCaGw.DataPacket packet = PBCaGw.DataPacket.Create(data, 320, null, true);
            for (int i = 0; i < 10; i++)
            {
                packet.SetUInt16(i * 32, (UInt16)(1 + i));
                packet.SetUInt16(i * 32 + 2, 16);
            }
            /*packet.Command = 1;
            packet.SetUInt16(2, 80);*/

            splitter.ProcessData(packet);
            packet = PBCaGw.DataPacket.Create(data, 32, null, true);
            splitter.ProcessData(packet);
            /*splitter.ProcessData(packet);

            packet = PBCaGw.DataPacket.Create(64);
            packet.Command = 1;
            packet.SetUInt16(2, 80);            
            packet.SetUInt16(34, 16);

            splitter.ProcessData(packet);*/


            /*for (int i = 0; i < 10; i++)
            {
                splitter.ProcessData(packet);
            }*/

            Console.WriteLine("End...");
            Console.ReadKey();
        }

        static void splitter_ReceiveData(PBCaGw.DataPacket packet)
        {
            Console.WriteLine("Got packet of " + packet.BufferSize + " / " + packet.Data.Length + ", cmd: " + packet.Command);
        }

        static AutoResetEvent gotValue;
        static AutoResetEvent gotDisconnect;
        static bool needToStop = false;

        private static void ZTest()
        {
            string[] channelNames = File.ReadAllLines(@"channels.txt");


            List<EpicsChannel> channels = new List<EpicsChannel>();

            client = new EpicsClient();
            //client.Configuration.SearchAddress = "172.22.200.117:5062";
            //client.Configuration.SearchAddress = "172.22.200.103:5062";
            client.Configuration.WaitTimeout = 2000;

            channelNames = channelNames.OrderBy(a => Guid.NewGuid()).ToArray();

            gotDisconnect = new AutoResetEvent(false);

            foreach (var i in channelNames)
            {
                if (needToStop)
                    break;
                Console.WriteLine("Checking " + i);
                EpicsChannel<ExtControl<string>> c = client.CreateChannel<ExtControl<string>>(i.Trim());
                //EpicsChannel c = client.CreateChannel(i.Trim());
                gotValue = new AutoResetEvent(false);
                c.MonitorChanged += gotValueNotified;
                channels.Add(c);
                /*if (channels.Count > 295)
                    break;*/
                c.StatusChanged += new EpicsStatusDelegate(c_StatusChanged);
                Thread.Sleep(100);
                /*try
                {
                    if (!gotValue.WaitOne(2000))
                    {
                        Console.WriteLine("Never got back.");
                    }
                }
                catch
                {
                    Console.WriteLine("Didn't get value for "+i);
                }
                c.Dispose();
                client.Dispose();*/
            }
            //Console.ReadKey();
            gotDisconnect.WaitOne();
            Console.WriteLine("Nb ok: " + channels.Count);
            Console.WriteLine("Disconnected!");
            client.Dispose();
            Console.ReadKey();
        }

        static void c_StatusChanged(EpicsChannel sender, ChannelStatus newStatus)
        {
            if (newStatus == ChannelStatus.DISCONNECTED)
            {
                gotDisconnect.Set();
                needToStop = true;
            }

        }

        static void gotValueNotified(EpicsChannel<ExtControl<string>> sender, ExtControl<string> newValue)
        //static void gotValueNotified(EpicsChannel sender, object newValue)
        {
            //gotValue.Set();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.ToString();
            //System.Diagnostics.Debugger.Break();
        }


        static List<EpicsChannel> chans;
        static void ReconnectIoc()
        {
            InitServer();

            gateway = new Gateway();
            gateway.Configuration.GatewayName = "TESTGW";
            gateway.Configuration.LocalAddressSideA = ip + ":5555";
            //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
            gateway.Configuration.RemoteAddressSideA = ip + ":5552";
            gateway.Configuration.LocalAddressSideB = ip + ":5888";
            //gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
            gateway.Configuration.RemoteAddressSideB = ip + ":5777";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gateway.SaveConfig();

            Gateway.AutoCreateChannel = false;
            Gateway.RestoreCache = false;
            gateway.Start();

            client = new EpicsClient();
            client.Configuration.SearchAddress = ip + ":5555";
            client.Configuration.WaitTimeout = 200;
            chans = new List<EpicsChannel>();

            for (int i = 0; i < 1; i++)
            {
                EpicsChannel<int> intChan = client.CreateChannel<int>("PCT:INT-" + i);
                chans.Add(intChan);
                intChan.MonitorChanged += new EpicsDelegate<int>(intChan_MonitorChanged);
            }

            Thread t = new Thread(ChanToGet);
            t.IsBackground = true;
            t.Start();

            //Thread.Sleep(2000);
            //chans[0].Get<int>();

            //for (int i = 0; i < 100; i++)
            while(true)
            {
                Thread.Sleep(5000);
                server.Dispose();
                Thread.Sleep(2000);
                InitServer();
            }

            Console.ReadKey();
        }

        static void ChanToGet()
        {
            while (true)
            {
                Thread.Sleep(500);

                object[] res = client.MultiGet<int>(chans);
                Console.WriteLine("Connected: " + res.Count(row => row != null));
            }
        }

        static void intChan_MonitorChanged(EpicsChannel<int> sender, int newValue)
        {
            Console.WriteLine("New val: " + newValue);
        }

        static void ReconnectProscan()
        {
            gateway = new Gateway();
            gateway.Configuration.GatewayName = "TESTGW";
            gateway.Configuration.LocalAddressSideA = ip + ":5555";
            //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
            gateway.Configuration.RemoteAddressSideA = remoteIp + ":5552";
            gateway.Configuration.LocalAddressSideB = ip + ":5888";
            //gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
            gateway.Configuration.RemoteAddressSideB = "172.25.60.67:5062";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gateway.SaveConfig();
            gateway.Start();


            client = new EpicsClient();
            client.Configuration.WaitTimeout = 2000;
            client.Configuration.SearchAddress = ip + ":5555";

            EpicsChannel<ExtGraphic<string>> ch = client.CreateChannel<ExtGraphic<string>>("MMAP10Y:CMODE:1");
            ch.StatusChanged += new EpicsStatusDelegate(ch_StatusChanged);
            ch.MonitorChanged += new EpicsDelegate<ExtGraphic<string>>(ch_MonitorChangedView);

            EpicsChannel<ExtGraphic<string>> ch2 = client.CreateChannel<ExtGraphic<string>>("MMAP12Y:CNSAMPLES:1");
            ch2.StatusChanged += new EpicsStatusDelegate(ch_StatusChanged);
            ch2.MonitorChanged += new EpicsDelegate<ExtGraphic<string>>(ch_MonitorChangedView);

            Thread.Sleep(1000);

            Console.WriteLine("------------------------------------------------------------------------");
            gateway.Dispose();
            //Thread.Sleep(5000);

            gateway = new Gateway();
            gateway.Configuration.LocalAddressSideA = ip + ":5555";
            //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
            gateway.Configuration.RemoteAddressSideA = remoteIp + ":5552";
            gateway.Configuration.LocalAddressSideB = ip + ":5888";
            //gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
            gateway.Configuration.RemoteAddressSideB = "172.25.60.67:5062";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gateway.Start();

            /*EpicsChannel restart = client.CreateChannel("TESTGW:RESTART");
            try
            {
                restart.Put<int>(2);
            }
            catch
            {
            }*/

            Thread.Sleep(5000);
            Console.ReadKey();
        }

        static void ch_StatusChanged(EpicsChannel sender, ChannelStatus newStatus)
        {
            Console.WriteLine(sender.ChannelName + ": " + newStatus);
        }

        static void MonAll()
        {
            //PBCaGw.Services.Log.Enabled = false;
            Stopwatch sw = new Stopwatch();
            for (int loop = 0; loop < 10; loop++)
            {
                gateway = new Gateway();
                gateway.Configuration.GatewayName = "TESTGW";
                gateway.Configuration.LocalAddressSideA = ip + ":5555";
                //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
                gateway.Configuration.RemoteAddressSideA = remoteIp + ":5552";
                gateway.Configuration.LocalAddressSideB = ip + ":5888";
                //gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
                gateway.Configuration.RemoteAddressSideB = "172.25.60.67:5062";
                gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
                gateway.SaveConfig();
                gateway.Start();

                Console.WriteLine("Gateway up");
                //gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.BIDIRECTIONAL;

                Gateway.BufferedSockets = false;

                Thread.Sleep(1000);
                multiActionCountDown = new CountdownEvent(channelsToConnect.Count(row => !row.Contains("TRACE")));
                hadValue = new Dictionary<string, bool>();
                sw.Start();
                client = new EpicsClient();
                client.Configuration.WaitTimeout = 2000;
                client.Configuration.SearchAddress = ip + ":5555";
                List<EpicsChannel<ExtGraphic<string>>> channels = new List<EpicsChannel<ExtGraphic<string>>>();
                foreach (string i in channelsToConnect.Where(row => !row.Contains("TRACE")))
                {
                    EpicsChannel<ExtGraphic<string>> ch = client.CreateChannel<ExtGraphic<string>>(i);
                    ch.MonitorChanged += new EpicsDelegate<ExtGraphic<string>>(ch_MonitorChangedGotValue);
                    channels.Add(ch);
                }
                if (multiActionCountDown.Wait(5000) == false)
                    Console.WriteLine("Didn't got it...");
                client.Dispose();
                sw.Stop();
                gateway.Dispose();
            }

            Console.WriteLine("Time: " + sw.Elapsed.ToString());
            Console.ReadKey();
        }

        static CountdownEvent multiActionCountDown;
        static Dictionary<string, bool> hadValue;

        static void ch_MonitorChangedGotValue(EpicsChannel<ExtGraphic<string>> sender, ExtGraphic<string> newValue)
        {
            lock (hadValue)
            {
                if (!hadValue.ContainsKey(sender.ChannelName))
                {
                    multiActionCountDown.Signal();
                    hadValue.Add(sender.ChannelName, true);
                }
            }
        }

        static void ProscanWave()
        {
            PBCaGw.Services.Log.Enabled = false;
            gateway = new Gateway();
            gateway.Configuration.GatewayName = "TESTGW";
            gateway.Configuration.LocalAddressSideA = ip + ":5555";
            //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
            gateway.Configuration.RemoteAddressSideA = remoteIp + ":5552";
            gateway.Configuration.LocalAddressSideB = ip + ":5888";
            //gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
            gateway.Configuration.RemoteAddressSideB = "172.25.60.67:5062";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gateway.SaveConfig();
            gateway.Start();

            Console.WriteLine("Gateway up");
            //gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.BIDIRECTIONAL;

            client = new EpicsClient();
            client.Configuration.WaitTimeout = 2000;
            client.Configuration.SearchAddress = ip + ":5555";
            List<EpicsChannel<ExtGraphic<string>>> channels = new List<EpicsChannel<ExtGraphic<string>>>();
            foreach (string i in channelsToConnect)
            {
                EpicsChannel<ExtGraphic<string>> ch = client.CreateChannel<ExtGraphic<string>>(i);
                ch.MonitorChanged += new EpicsDelegate<ExtGraphic<string>>(ch_MonitorChanged);
                channels.Add(ch);
            }
            Thread.Sleep(2000);


            gateway.Dispose();
            PBCaGw.Services.Log.Enabled = true;
            //Thread.Sleep(5000);

            gateway = new Gateway();
            gateway.Configuration.LocalAddressSideA = ip + ":5555";
            //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
            gateway.Configuration.RemoteAddressSideA = remoteIp + ":5552";
            gateway.Configuration.LocalAddressSideB = ip + ":5888";
            //gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
            gateway.Configuration.RemoteAddressSideB = "172.25.60.67:5062";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            gateway.Start();

            Thread.Sleep(5000);

            foreach (var i in channels)
            {
                if (i.Status != ChannelStatus.CONNECTED)
                {
                    Console.WriteLine(i.ChannelName + " not connected.");
                }
            }
            //client.Dispose();

            Console.ReadKey();
            //Console.WriteLine(ch.Get<string>());
        }

        static void ch_MonitorChangedView(EpicsChannel<ExtGraphic<string>> sender, ExtGraphic<string> newValue)
        {
            Console.WriteLine(newValue.Value);
        }

        static void ch_MonitorChanged(EpicsChannel<ExtGraphic<string>> sender, ExtGraphic<string> newValue)
        {
            //Console.WriteLine(newValue.Value);
        }


        static void InternTest()
        {
            InitServer();

            gateway = new Gateway();
            gateway.Configuration.LocalAddressSideA = ip + ":5555";
            //gateway.Configuration.LocalAddressSideA = "129.129.130.87:5555";
            gateway.Configuration.RemoteAddressSideA = remoteIp + ":5552";
            gateway.Configuration.LocalAddressSideB = ip + ":5888";
            gateway.Configuration.RemoteAddressSideB = remoteIp + ":5777";
            gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.UNIDIRECTIONAL;
            //gateway.Configuration.ConfigurationType = PBCaGw.Configurations.ConfigurationType.BIDIRECTIONAL;
            gateway.Start();

            client = new EpicsClient();
            client.Configuration.SearchAddress = ip + ":5555";
            //client.Configuration.SearchAddress = "129.129.130.255:5555";
            //client.Configuration.SearchAddress = "127.0.0.1:5777";

            //MultiConnect();
            //Checks2();

            EpicsChannel ca = client.CreateChannel("PCT:INT-1");
            Console.WriteLine(ca.Get<string>());

            //GetDisconnect();

            Console.WriteLine("Press a key to continue...");
            Console.ReadKey();
        }

        private static void InitServer()
        {
            server = new CAServer(IPAddress.Parse(ip), 5777, 5777);
            for (int i = 0; i < intRecords.Length; i++)
            {
                intRecords[i] = server.CreateRecord<CAIntRecord>("PCT:INT-" + i);
                intRecords[i].Scan = CaSharpServer.Constants.ScanAlgorithm.HZ10;
                intRecords[i].PrepareRecord += new EventHandler(Program_PrepareRecord);
            }
        }

        static bool needToRun = true;
        private static void GetDisconnect()
        {
            EpicsChannel<int> intChannel = client.CreateChannel<int>("PCT:INT-0");
            intChannel.StatusChanged += IntChannelStatusChanged;
            client.Configuration.WaitTimeout = 500;

            Console.WriteLine("Wait for init");
            Thread.Sleep(2000);
            Console.WriteLine("Starting");

            Thread backgroundGet = new Thread(action =>
                                                {
                                                    for (int i = 0; i < 100 && needToRun == true; i++)
                                                    {
                                                        Console.WriteLine("Loop " + i);
                                                        try
                                                        {
                                                            Console.WriteLine(intChannel.Get());
                                                        }
                                                        catch
                                                        {
                                                        }
                                                        Thread.Sleep(100);
                                                    }
                                                });

            backgroundGet.Start();
            //Console.ReadKey();
            Thread.Sleep(4000);
            Console.WriteLine("Closing IOC");
            server.Dispose();
            Thread.Sleep(1000);
            Console.WriteLine("Starting IOC");
            InitServer();
            Thread.Sleep(8000);
            needToRun = false;
        }

        static void IntChannelStatusChanged(EpicsChannel sender, ChannelStatus newStatus)
        {
            Console.WriteLine("Channel status changed: " + newStatus);
        }

        static void MultiConnect()
        {
            EpicsChannel[] channels = new EpicsChannel[intRecords.Length];

            for (int i = 0; i < channels.Length; i++)
                channels[i] = client.CreateChannel("PCT:INT-" + i);

            client.MultiConnect(channels);
            client.Dispose();

            client = new EpicsClient();
            client.Configuration.SearchAddress = "127.0.0.1:5555";
            for (int i = 0; i < channels.Length; i++)
                channels[i] = client.CreateChannel("PCT:INT-" + i);
            client.MultiConnect(channels);

            Console.WriteLine("Done...");
            Dictionary<uint, string> sids = new Dictionary<uint, string>();
            foreach (EpicsChannel c in channels)
            {
                if (sids.ContainsKey(c.SID) && sids[c.SID] != c.ChannelName)
                {
                    Console.WriteLine("Reuse: " + c.SID);
                }
                else if (!sids.ContainsKey(c.SID))
                    sids.Add(c.SID, c.ChannelName);
            }

            client.Dispose();

        }

        static void Check2()
        {
            EpicsChannel[] channels = new EpicsChannel[intRecords.Length];
            for (int i = 0; i < channels.Length; i++)
                channels[i] = client.CreateChannel("PCT:INT-" + i);
            client.MultiConnect(channels);

            Dictionary<uint, string> sids = new Dictionary<uint, string>();
            Console.WriteLine("Connecting....");
            client.MultiConnect(channels);

            Console.WriteLine("Done...");
            foreach (EpicsChannel c in channels)
            {
                if (sids.ContainsKey(c.SID) && sids[c.SID] != c.ChannelName)
                {
                    Console.WriteLine("Reuse: " + c.SID);
                }
                else if (!sids.ContainsKey(c.SID))
                    sids.Add(c.SID, c.ChannelName);
            }

            for (int k = 0; k < 1000; k++)
            {
                List<EpicsChannel> myList = channels.Where(row => rnd.Next(0, 2) == 0).ToList();

                object[] res = client.MultiGet<int>(myList);
                int nbEmpty = 0;
                int nbFaults = 0;
                for (int i = 0; i < myList.Count; i++)
                {
                    if (res[i] == null)
                        nbEmpty++;
                    else
                    {
                        int v = (int)res[i];
                        int id = int.Parse(myList[i].ChannelName.Split('-')[1]);
                        if (id != v / 1000)
                            nbFaults++;
                    }
                }
                Console.WriteLine("Empty: " + nbEmpty + ", Faults: " + nbFaults + ", Asked: " + myList.Count);
                Thread.Sleep(rnd.Next(10, 100));
            }

            client.Dispose();

        }

        static void Program_MonitorChanged(EpicsChannel sender, object newValue)
        {
            Console.WriteLine("CID: " + sender.CID + ", SID: " + sender.SID + ", Value: " + newValue.ToString());
        }

        static void Program_PrepareRecord(object sender, EventArgs e)
        {
            CAIntRecord intRecord = (CAIntRecord)sender;
            int id = int.Parse(intRecord.Name.Split('-')[1]);
            intRecord.Value = id * 1000 + rnd.Next(0, 999);
        }

        readonly static string[] channelsToConnect ={
"MMAP10Y:CMODE:1",
"MMAP10Y:CNSAMPLES:1",
"MMAP10Y:CPOSTSAMPLES:1",
"MMAP10Y:CREGS:1",
"MMAP10Y:CSTOPMODE:1",
"MMAP10Y:DESC:CMT",
"MMAP10Y:DESC:SEC",
"MMAP10Y:DESC:SYS",
"MMAP10Y:DESC:TYP",
"MMAP10Y:IB01:1",
"MMAP10Y:IB01:2",
"MMAP10Y:IB01:3",
"MMAP10Y:IB02:1",
"MMAP10Y:IB02:2",
"MMAP10Y:IB02:3",
"MMAP10Y:IB03:1",
"MMAP10Y:IB03:2",
"MMAP10Y:IB03:3",
"MMAP10Y:IB04:1",
"MMAP10Y:IB04:2",
"MMAP10Y:IB04:3",
"MMAP10Y:IB05:1",
"MMAP10Y:IB05:2",
"MMAP10Y:IB05:3",
"MMAP10Y:IB06:1",
"MMAP10Y:IB06:2",
"MMAP10Y:IB06:3",
"MMAP10Y:IB07:1",
"MMAP10Y:IB07:2",
"MMAP10Y:IB07:3",
"MMAP10Y:IB08:1",
"MMAP10Y:IB08:2",
"MMAP10Y:IB08:3",
"MMAP10Y:IB09:1",
"MMAP10Y:IB09:2",
"MMAP10Y:IB09:3",
"MMAP10Y:IB10:1",
"MMAP10Y:IB10:2",
"MMAP10Y:IB10:3",
"MMAP10Y:IB11:1",
"MMAP10Y:IB11:2",
"MMAP10Y:IB11:3",
"MMAP10Y:IB12:1",
"MMAP10Y:IB12:2",
"MMAP10Y:IB12:3",
"MMAP10Y:IB13:1",
"MMAP10Y:IB13:2",
"MMAP10Y:IB13:3",
"MMAP10Y:IB14:1",
"MMAP10Y:IB14:2",
"MMAP10Y:IB14:3",
"MMAP10Y:IB15:1",
"MMAP10Y:IB15:2",
"MMAP10Y:IB15:3",
"MMAP10Y:IB16:1",
"MMAP10Y:IB16:2",
"MMAP10Y:IB16:3",
"MMAP10Y:IIST:1",
"MMAP10Y:IIST:2",
"MMAP10Y:IL01:1",
"MMAP10Y:IL01:2",
"MMAP10Y:IL01:3",
"MMAP10Y:IL02:1",
"MMAP10Y:IL02:2",
"MMAP10Y:IL02:3",
"MMAP10Y:IL03:1",
"MMAP10Y:IL03:2",
"MMAP10Y:IL03:3",
"MMAP10Y:IL04:1",
"MMAP10Y:IL04:2",
"MMAP10Y:IL04:3",
"MMAP10Y:IL05:1",
"MMAP10Y:IL05:2",
"MMAP10Y:IL05:3",
"MMAP10Y:IL06:1",
"MMAP10Y:IL06:2",
"MMAP10Y:IL06:3",
"MMAP10Y:IL07:1",
"MMAP10Y:IL07:2",
"MMAP10Y:IL07:3",
"MMAP10Y:IL08:1",
"MMAP10Y:IL08:2",
"MMAP10Y:IL08:3",
"MMAP10Y:IL09:1",
"MMAP10Y:IL09:2",
"MMAP10Y:IL09:3",
"MMAP10Y:IL10:1",
"MMAP10Y:IL10:2",
"MMAP10Y:IL10:3",
"MMAP10Y:IL11:1",
"MMAP10Y:IL11:2",
"MMAP10Y:IL11:3",
"MMAP10Y:IL12:1",
"MMAP10Y:IL12:2",
"MMAP10Y:IL12:3",
"MMAP10Y:IL13:1",
"MMAP10Y:IL13:2",
"MMAP10Y:IL13:3",
"MMAP10Y:IL14:1",
"MMAP10Y:IL14:2",
"MMAP10Y:IL14:3",
"MMAP10Y:IL15:1",
"MMAP10Y:IL15:2",
"MMAP10Y:IL15:3",
"MMAP10Y:IL16:1",
"MMAP10Y:IL16:2",
"MMAP10Y:IL16:3",
"MMAP10Y:ILKP:2",
"MMAP10Y:ISTA:1",
"MMAP10Y:PPOS:1",
"MMAP10Y:PPOS:2",
"MMAP10Y:PROF:1",
"MMAP10Y:PROF:1:P",
"MMAP10Y:PROF:1:W",
"MMAP10Y:PROF:2",
"MMAP10Y:PROF:2:N",
"MMAP10Y:PROF:2:P",
"MMAP10Y:PROF:2:gs",
"MMAP10Y:PROF:2:gs2",
"MMAP10Y:PROL:1",
"MMAP10Y:PSIZ:1",
"MMAP10Y:PSIZ:2",
"MMAP10Y:PSUM:1",
"MMAP10Y:PSUM:2",
"MMAP10Y:RIDX:1",
"MMAP10Y:RVAL:1",
"MMAP10Y:TRACE:1",
"MMAP10Y:TRACE:1:PPT",
"MMAP10Y:TRACE:1:SA",
"MMAP10Y:SIGB:2",
"MMAP10Y:SIGL:2",
"MMAP10Y:SMODE:1",
"MMAP10Y:SNSAMPLES:1",
"MMAP10Y:SPB:2",
"MMAP10Y:SPL:2",
"MMAP10Y:SPOINTER:1",
"MMAP10Y:SREGS:1",
"MMAP10Y:STR:2",
"MMAP10Y:STR:3",
"MMAP10Y:TCOM:1",
"MMAP10Y:TCOM:2",
"MMAP10Y:TMOD:1",
"MMAP10Y:TMODRB:1",
"MMAP10Y:TNT:1",
"MMAP10Y:TNTRB:1",
"MMAP10Y:TPT:1",
"MMAP10Y:TPTRB:1",
"MMAP10Y:TR2:2",
"MMAP10Y:TSR:1",
"MMAP10Y:TSRRB:1",
"MMAP10Y:TSTA:1",
"MMAP10Y:WIDX:1",
"MMAP10Y:WVAL:1"};
    }
}
