using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace GWLogViewer
{
    public class LogEntry
    {
        public DateTime Date;
        public string Sender;
        public int ChainId;
        public TraceEventType EventType;
        public string Message;
    }
}
