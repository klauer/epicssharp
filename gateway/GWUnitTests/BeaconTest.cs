using System.Collections.Generic;
using System.Net;
using PBCaGw.Handlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using PBCaGw;
using PBCaGw.Workers;
using Moq;

namespace GWUnitTests
{


    /// <summary>
    ///This is a test class for BeaconTest and is intended
    ///to contain all BeaconTest Unit Tests
    ///</summary>
    [TestClass]
    public class BeaconTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Beacon Constructor
        ///</summary>
        [TestMethod]
        public void BeaconConstructorTest()
        {
            // ReSharper disable UnusedVariable
            Beacon target = new Beacon();
            // ReSharper restore UnusedVariable
        }

        [TestMethod]
        public void BeaconDoRequestLevel5()
        {
            Beacon target=new Beacon();

            Gateway gateway=new Gateway();
            gateway.Configuration.LocalAddressSideA = "127.0.0.1:1234";
            gateway.Configuration.LocalAddressSideB = "127.0.0.1:4321";

            var beaconB = new Mock<IBeaconResetter>();
            gateway.beaconB = beaconB.Object;

            WorkerChain chain=WorkerChain.UdpBeaconReceiver(gateway,ChainSide.SIDE_A,gateway.Configuration.LocalSideA, new List<IPEndPoint>(new IPEndPoint[] {gateway.Configuration.LocalSideA}) );

            DataPacket packet = DataPacket.Create(16,chain);
            packet.Parameter1 = 5;
            packet.Sender = PBCaGw.Configurations.Configuration.ParseAddress("168.1.1.2:1234");

            target.DoRequest(packet, chain, pk => { throw new Exception("Sent something!"); });
            beaconB .Verify(fw => fw.ResetBeacon(), Times.Once());
        }

        [TestMethod]
        public void BeaconDoRequestLevel1()
        {
            Beacon target = new Beacon();

            Gateway gateway = new Gateway();
            gateway.Configuration.LocalAddressSideA = "127.0.0.1:1234";
            gateway.Configuration.LocalAddressSideB = "127.0.0.1:4321";


            var beaconB = new Mock<IBeaconResetter>();
            gateway.beaconB = beaconB.Object;

            WorkerChain chain = WorkerChain.UdpBeaconReceiver(gateway, ChainSide.SIDE_A, gateway.Configuration.LocalSideA, new List<IPEndPoint>(new IPEndPoint[] { gateway.Configuration.LocalSideA }));

            DataPacket packet = DataPacket.Create(16, chain);
            packet.Parameter1 = 1;
            packet.Sender = PBCaGw.Configurations.Configuration.ParseAddress("168.1.1.2:1234");

            target.DoRequest(packet, chain, pk => { throw new Exception("Sent something!"); });
            beaconB.Verify(fw => fw.ResetBeacon(), Times.Never());
        }

        [TestMethod]
        public void BeaconDoResponse()
        {
        }
    }
}
