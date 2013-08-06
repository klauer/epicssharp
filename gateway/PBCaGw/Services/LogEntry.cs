using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PBCaGw.Services
{
    class LogEntry
    {
        public TraceEventType EventType;
        public int Id;
        public string Message;
        public string Source;
    }
}
