using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NameServer
{
    class EpicsChannel
    {
        public string Name { get; set; }

        public Action CallBack { get; set; }

        public uint CID { get; set; }
    }
}
