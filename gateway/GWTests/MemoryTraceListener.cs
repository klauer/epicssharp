using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWTests
{
    class MemoryTraceListener : ConsoleTraceListener
    {
        public List<string> messages = new List<string>();

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            messages.Add(message);
        }

    }
}
