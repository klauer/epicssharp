using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NameServer
{
    public class StartStopFilter : TraceFilter
    {
        public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
        {
            return (eventType == TraceEventType.Critical || eventType == TraceEventType.Error || eventType == TraceEventType.Start || eventType == TraceEventType.Stop);
        }
    }
}
