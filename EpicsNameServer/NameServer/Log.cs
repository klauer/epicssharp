using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NameServer
{
    static class Log
    {
        static readonly TraceSwitch traceSwitch;
        public static readonly TraceSource traceSource;

        static Log()
        {
            try
            {
                traceSwitch = new TraceSwitch("NSSwitch", "Configuration level for the gateway trace level.");
                traceSource = new TraceSource("NSSource");
            }
            catch
            {
            }
        }

        public static void Write(TraceEventType eventType, string message, [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        {
            sourceFilePath = sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1);

            try
            {
                traceSource.TraceEvent(eventType, 0, message, new object[] { memberName, sourceFilePath, sourceLineNumber });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}
