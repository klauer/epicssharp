using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace PBCaGw.Configurations
{
    [Serializable]
    public class SecurityRule
    {
        private Security security = null;
        [XmlIgnore]
        public Security Security
        {
            get
            {
                return security;
            }
            set
            {
                security = value;
                Filter.Security = security;
            }
        }

        public SecurityRule()
        {
            ChannelPattern = "*";
            Access = SecurityAccess.READ | SecurityAccess.WRITE;
            Filter = new AllFilter();
        }

        [XmlAttribute("Channel")]
        public string ChannelPattern { get; set; }
        [XmlAttribute("Access")]
        public SecurityAccess Access { get; set; }
        [XmlElement("Filter")]
        public SecurityFilter Filter { get; set; }

        Regex exp = null;
        bool reverse = false;
        public bool Applies(string channel, string username, string hostname, string ip)
        {
            if (!Filter.Applies(username, hostname, ip))
                return false;

            if (exp.IsMatch(channel))
                return !reverse;
            return reverse;
        }

        public static string FixPattern(string pattern)
        {
            /*Regex exp = new Regex("(^|[^\\.])\\*");
            return exp.Replace(pattern, "$1.*");*/
            return pattern.Replace(".", "\\.").Replace("*", ".*");
        }

        public void Init()
        {
            if (ChannelPattern.StartsWith("!"))
            {
                reverse = true;
                exp = new Regex("^" + FixPattern(ChannelPattern.Substring(1)) + "$");
            }
            else
                exp = new Regex("^" + FixPattern(ChannelPattern) + "$");

            Filter.Init();
        }
    }
}
