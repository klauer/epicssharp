using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PBCaGw.Services
{
    /// <summary>
    /// Logs data in a file with a XML like format (missing the header and footer such that the lines can be happened)
    /// </summary>
    public class GWFileTraceListener : TextWriterTraceListener
    {
        public GWFileTraceListener()
        {
        }

        public GWFileTraceListener(string fileName)
            : base(fileName)
        {
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            StackTrace stackTrace = new StackTrace(true);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            this.Writer.WriteLine("<entry date='" + Gateway.Now.ToShortDateString() + "' time='" + Gateway.Now.ToShortTimeString() + "' type='" +
                 eventType.ToString() + "' chainId='" + id + "'>" +
                "<sender class='" + stackFrames[3].GetMethod().ReflectedType.Name + "." + stackFrames[3].GetMethod().Name + "' line='" + stackFrames[3].GetFileLineNumber() + "' />" +
                "<message>" + message + "</message></entry>");
        }
    }
}
