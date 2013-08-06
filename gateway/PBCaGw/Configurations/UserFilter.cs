using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PBCaGw.Configurations
{
    [Serializable]
    public class UserFilter : SecurityFilter
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        public override bool Applies(string username, string hostname, string ip)
        {
            if (username == null)
                return false;
            return String.Compare(username, Name, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override void Init()
        {
        }
    }
}
