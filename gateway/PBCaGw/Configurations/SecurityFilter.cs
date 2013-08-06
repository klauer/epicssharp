using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PBCaGw.Configurations
{
    [Serializable]
    [XmlInclude(typeof(AllFilter))]
    [XmlInclude(typeof(HostFilter))]
    [XmlInclude(typeof(IPFilter))]
    [XmlInclude(typeof(UserFilter))]
    [XmlInclude(typeof(GroupFilter))]
    public abstract class SecurityFilter
    {
        public abstract bool Applies(string username, string hostname, string ip);

        [XmlIgnore]
        public Security Security { get; set; }

        public abstract void Init();
    }
}
