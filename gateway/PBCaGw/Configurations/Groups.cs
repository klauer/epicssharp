using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PBCaGw.Configurations
{
    [Serializable]
    public class Group
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlArray("Filters")]
        [XmlArrayItem("Filter")]
        public List<SecurityFilter> Filters = new List<SecurityFilter>();
    }
}
