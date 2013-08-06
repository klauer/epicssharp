using Security.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Security;
using Moq;
using System.Collections.Generic;
using Security.RuleElements;

namespace PBCaGwTest
{


    [TestClass()]
    public class SecurityServiceTest
    {
        SecurityService securityService;
        List<SecurityRule> rules;
        

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
            securityService = new SecurityService();

            Mock<ISecurityRulesProvider> securityRuleProviderMock = new Mock<ISecurityRulesProvider>();

            rules = new List<SecurityRule>();

            securityRuleProviderMock.Setup(provider => provider.GetAll())
                .Returns(rules);

            securityService.securityRulesProvider = securityRuleProviderMock.Object;
        }

        private SecurityRule CreateRule(string permission)
        {
            SecurityRule simpleRule;

            Mock<IRuleElement> plainUser = new Mock<IRuleElement>();
            Mock<IRuleElement> plainChannel = new Mock<IRuleElement>();

            plainUser.Setup(rule => rule.Match("{USER}"))
               .Returns(true);

            plainChannel.Setup(rule => rule.Match("{CHANNEL}"))
                .Returns(true);

            simpleRule = new SecurityRule();

            simpleRule.subject = plainUser.Object;
            simpleRule.target = plainChannel.Object;
            simpleRule.permission = permission;

            return simpleRule;
        }
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        [TestMethod()]
        public void GetPermission_NoRuleTest()
        {            

            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
        }


        [TestMethod()]
        public void GetPermission_SimpleReadRuleTest()
        {   
            rules.Add(CreateRule("R"));

            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
        }

        [TestMethod()]
        public void GetPermission_SimpleWriteRuleTest()
        {
            rules.Add(CreateRule("W"));

            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_SimpleReadWriteRuleTest()
        {
            rules.Add(CreateRule("RW"));

            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        
        [TestMethod()]
        public void GetPermission_AddAndRemoveReadRuleTest()
        {
            rules.Add(CreateRule("R"));
            rules.Add(CreateRule("-R"));

            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_AddAndRemoveWriteRuleTest()
        {
            rules.Add(CreateRule("W"));
            rules.Add(CreateRule("-W"));

            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_AddReadWriteAndRemoveReadRuleTest()
        {
            rules.Add(CreateRule("RW"));
            rules.Add(CreateRule("-W"));

            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_AddReadWriteAndRemoveWriteRuleTest()
        {
            rules.Add(CreateRule("RW"));
            rules.Add(CreateRule("-R"));

            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_AddReadWriteAndRemoveReadWriteRuleTest()
        {
            rules.Add(CreateRule("RW"));
            rules.Add(CreateRule("-RW"));

            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_AddReadWriteSeparatelyAndRemoveReadWriteRuleTest()
        {
            rules.Add(CreateRule("R"));
            rules.Add(CreateRule("W"));
            rules.Add(CreateRule("-RW"));

            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_RemoveReadWriteRuleTest()
        {
            rules.Add(CreateRule("-RW"));

            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_AddReadWriteLowerCaseRuleTest()
        {
            rules.Add(CreateRule("rw"));

            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_AddReadLowerCaseRuleTest()
        {
            rules.Add(CreateRule("r"));

            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }

        [TestMethod()]
        public void GetPermission_AddWriteLowerCaseRuleTest()
        {
            rules.Add(CreateRule("w"));

            Assert.IsTrue(securityService.GetPermission("{USER}", "{CHANNEL}").HasWritePermission());
            Assert.IsFalse(securityService.GetPermission("{USER}", "{CHANNEL}").HasReadPermission());
        }
    }
}
