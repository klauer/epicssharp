using Security.RuleElements;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PBCaGwTest
{


    [TestClass()]
    public class UserRuleElementTest
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
        public void SimpleMatchTest()
        {
            UserRuleElement ruleElement = new UserRuleElement("user"); 
            Assert.IsTrue(ruleElement.Match("user"));
        }

        [TestMethod()]
        public void SimpleNonMatchTest()
        {
            UserRuleElement ruleElement = new UserRuleElement("user1");
            Assert.IsFalse(ruleElement.Match("user2"));
        }

        [TestMethod()]
        public void UpperCaseMatchLowerCaseTest()
        {
            UserRuleElement ruleElement = new UserRuleElement("user");
            Assert.IsTrue(ruleElement.Match("USER"));
        }

        [TestMethod()]
        public void LowerCaseMatchUpperCaseTest()
        {
            UserRuleElement ruleElement = new UserRuleElement("USER");
            Assert.IsTrue(ruleElement.Match("user"));
        }

        [TestMethod()]
        public void RegexMatchTest()
        {
            UserRuleElement ruleElement = new UserRuleElement("USER@129.129.130.[0-9]{3}");
            Assert.IsTrue(ruleElement.Match("user@129.129.130.181"));
        }
    }
}
