using Security.RuleElements;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PBCaGwTest
{


    [TestClass()]
    public class ChannelRuleElementTest
    {


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


        [TestMethod()]
        public void SimpleChannelMatchTest()
        {
            ChannelRuleElement channelRule = new ChannelRuleElement("CHANNEL"); 
            Assert.IsTrue(channelRule.Match("CHANNEL"));
        }

        [TestMethod()]
        public void SimpleChannelNonMatchingTest()
        {
            ChannelRuleElement channelRule = new ChannelRuleElement("CHANNEL1");
            Assert.IsFalse(channelRule.Match("CHANNEL2"));
        }

        [TestMethod()]
        public void RegexChannelMatchTest()
        {
            ChannelRuleElement channelRule = new ChannelRuleElement("CHANNEL*");
            Assert.IsTrue(channelRule.Match("CHANNEL11"));
            Assert.IsTrue(channelRule.Match("CHANNEL2"));
        }

        [TestMethod()]
        public void RegexChannelNonMatchingTest()
        {
            ChannelRuleElement channelRule = new ChannelRuleElement("CHANNEL[0-1]");
            Assert.IsTrue(channelRule.Match("CHANNEL0"));
            Assert.IsTrue(channelRule.Match("CHANNEL1"));
            Assert.IsFalse(channelRule.Match("CHANNEL2"));
        }
    }
}
