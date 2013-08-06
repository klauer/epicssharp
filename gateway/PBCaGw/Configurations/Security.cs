using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Concurrent;

namespace PBCaGw.Configurations
{
    [Serializable]
    public class Security
    {
        [XmlArray("Groups")]
        [XmlArrayItem(ElementName = "Group")]
        public List<Group> Groups = new List<Group>();

        [XmlArray("RulesSideA")]
        [XmlArrayItem(ElementName = "Rule")]
        public List<SecurityRule> RulesSideA = new List<SecurityRule>();

        [XmlArray("RulesSideB")]
        [XmlArrayItem(ElementName = "Rule")]
        public List<SecurityRule> RulesSideB = new List<SecurityRule>();

        public SecurityAccess EvaluateSideA(string channel, string username, string hostname, string ip)
        {
            string rule = channel + "/" + username + "/" + hostname + "/" + ip;

            SecurityAccess result = SecurityAccess.ALL;
            foreach (var i in RulesSideA)
            {
                if (i.Applies(channel, username, hostname, ip))
                    result = i.Access;
            }
            return result;
        }

        public SecurityAccess EvaluateSideB(string channel, string username, string hostname, string ip)
        {
            string rule = channel + "/" + username + "/" + hostname + "/" + ip;

            SecurityAccess result = SecurityAccess.ALL;
            foreach (var i in RulesSideB)
            {
                if (i.Applies(channel, username, hostname, ip))
                    result = i.Access;
            }
            return result;
        }

        public void Init()
        {
            foreach (var i in RulesSideA)
            {
                i.Security = this;
                i.Init();
            }

            foreach (var i in RulesSideB)
            {
                i.Security = this;
                i.Init();
            }
        }
    }
}
