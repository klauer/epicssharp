using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PBCaGw.Configurations
{
    [Serializable]
    public class GroupFilter : SecurityFilter
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlIgnore]
        public List<SecurityFilter> Members = null;

        public override bool Applies(string username, string hostname, string ip)
        {
            foreach (var i in Members)
                if (i.Applies(username, hostname, ip))
                    return true;
            return false;
        }

        public override void Init()
        {
            Group g = Security.Groups.SingleOrDefault(row => row.Name.ToLower() == Name.ToLower());
            if (g != null)
                Members = g.Filters;

            foreach (var i in Members)
                i.Init();
        }
    }
}
