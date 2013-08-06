using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace PBCaGw.Configurations
{
    [Serializable]
    public class IPFilter : SecurityFilter
    {
        [XmlElement("IP")]
        public string IP { get; set; }

        Regex exp = null;
        bool reverse = false;
        public override bool Applies(string username, string hostname, string ip)
        {
            if (exp.IsMatch(ip))
                return !reverse;
            return reverse;
        }

        public override void Init()
        {
            if (IP.StartsWith("!"))
            {
                exp = new Regex("^" + SecurityRule.FixPattern(IP.Substring(1)) + "$");
                reverse = true;
            }
            exp = new Regex("^" + SecurityRule.FixPattern(IP) + "$");
        }
    }
}
