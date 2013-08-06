using Security.RuleElements;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace PBCaGwTest
{


    [TestClass()]
    public class UserGroupRuleElementTest
    {

        UserGroupRuleElement groupRule;
        List<UserRuleElement> users;

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
            users = new List<UserRuleElement>();

            groupRule = new UserGroupRuleElement(users);
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
        public void EmptyGroupMatchTest()
        {
            Assert.IsFalse(groupRule.Match("user"));
        }

        [TestMethod()]
        public void OneUserGroupMatchTest()
        {
            AddUserToTheGroup("user");

            Assert.IsTrue(groupRule.Match("user"));
        }

        [TestMethod()]
        public void TwoUsersGroupMatchTest()
        {
            AddUserToTheGroup("user1");
            AddUserToTheGroup("user2");

            Assert.IsTrue(groupRule.Match("user1"));
            Assert.IsTrue(groupRule.Match("user2"));
        }

        [TestMethod()]
        public void TwoUsersGroupNonMatchingMatchTest()
        {
            AddUserToTheGroup("user1");
            AddUserToTheGroup("user2");

            Assert.IsFalse(groupRule.Match("user"));
        }

        private void AddUserToTheGroup(string username)
        {
            UserRuleElement user = new UserRuleElement(username);

            users.Add(user);
        }
    }
}
