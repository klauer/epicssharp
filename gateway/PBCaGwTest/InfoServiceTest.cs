using PBCaGw.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;

namespace PBCaGwTest
{
    [TestClass()]
    public class InfoServiceTest
    {
        IPEndPoint endPoint;

        private TestContext testContextInstance;

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
        [TestInitialize()]
        public void MyTestInitialize()
        {
            endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1);
        }
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /*[TestMethod()]
        [Ignore]
        public void StoreOnePacketTest()
        {
            uint cidGw = InfoService.StoreChannelCid(1, endPoint);

            Assert.AreEqual((uint)1, cidGw);
        }

        [TestMethod()]
        public void StoreOnePacketAndGetItBackTest()
        {
            uint cidGw = InfoService.StoreChannelCid(1, endPoint);

            CidEndPointEntry result = InfoService.GetChannelCid(cidGw);
            Assert.AreEqual((uint)1, result.Cid);
            Assert.AreEqual("127.0.0.1:1", result.EndPoint.ToString());
        }

        [TestMethod()]
        [Ignore]
        public void StoreTwoPacketsTest()
        {
            uint cidGw1 = InfoService.StoreChannelCid(1, endPoint);
            uint cidGw2 = InfoService.StoreChannelCid(1, endPoint);

            Assert.AreEqual((uint)1, cidGw1);
            Assert.AreEqual((uint)2, cidGw2);
        }

        [TestMethod()]
        public void StoreTwoPacketsAndGetThemBackTest()
        {
            uint cidGw1 = InfoService.StoreChannelCid(1, endPoint);
            uint cidGw2 = InfoService.StoreChannelCid(2, endPoint);

            CidEndPointEntry result1 = InfoService.GetChannelCid(cidGw1);
            CidEndPointEntry result2 = InfoService.GetChannelCid(cidGw2);

            Assert.AreEqual((uint)1, result1.Cid);
            Assert.AreEqual((uint)2, result2.Cid);
        }

        [TestMethod()]
        public void StoreTwoPacketsAndGetThemBackInReverseOrderTest()
        {
            uint cidGw1 = InfoService.StoreChannelCid(1, endPoint);
            uint cidGw2 = InfoService.StoreChannelCid(2, endPoint);

            CidEndPointEntry result2 = InfoService.GetChannelCid(cidGw2);
            CidEndPointEntry result1 = InfoService.GetChannelCid(cidGw1);

            Assert.AreEqual((uint)1, result1.Cid);
            Assert.AreEqual((uint)2, result2.Cid);
        }*/
    }
}
