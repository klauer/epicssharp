using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NameServer
{
    class WaitingClient
    {
        public readonly DateTime CreatedOn = DateTime.Now;

        public System.Net.IPEndPoint Destination { get; set; }

        public uint SearchId { get; set; }
    }
}
