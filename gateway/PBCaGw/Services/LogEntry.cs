using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PBCaGw.Services
{
    public class LogEntry
    {
        public TraceEventType EventType;
        public int Id;
        public string Message;
        public string Source;
    }
}
