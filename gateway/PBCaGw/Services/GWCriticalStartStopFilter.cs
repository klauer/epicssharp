using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PBCaGw.Services
{
    /// <summary>
    /// Shows only Critical, Errors, start and stop messages.
    /// </summary>
    public class GWCriticalStartStopFilter : TraceFilter
    {
        public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
        {
            return (eventType == TraceEventType.Critical || eventType == TraceEventType.Error || eventType == TraceEventType.Start || eventType == TraceEventType.Stop);
        }
    }
}
