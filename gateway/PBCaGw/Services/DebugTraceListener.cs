using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PBCaGw.Services
{
    public delegate void DebugLogDelegate(string source, TraceEventType eventType, int chainId, string message);

    /// <summary>
    /// A colored, and more informative TraceListener than the default ConsoleTraceListener
    /// </summary>
    public class DebugTraceListener
    {
        public static event DebugLogDelegate LogEntry;
        static bool traceAll = false;
        public static event EventHandler TraceLevelChanged;
        public static bool TraceAll
        {
            get
            {
                return traceAll;
            }
            set
            {
                if (traceAll != value)
                {
                    traceAll = value;
                    if (TraceLevelChanged != null)
                        TraceLevelChanged(null, null);
                }
            }
        }

        internal static void DoLogEntry(string source, TraceEventType eventType, int chainId, string message)
        {
        }

        static List<LogEntry> lastEntries = new List<LogEntry>();

        public static LogEntry[] LastEntries
        {
            get
            {
                return lastEntries.ToArray();
            }
        }

        public static void TraceEvent(TraceEventType eventType, int id, string message)
        {
            if (TraceAll == false && !(eventType == TraceEventType.Critical || eventType == TraceEventType.Error || eventType == TraceEventType.Start || eventType == TraceEventType.Stop))
                return;

            StackTrace stackTrace = new StackTrace(true);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            string debugInfo = stackFrames[2].GetMethod().ReflectedType.Name + "." + stackFrames[2].GetMethod().Name + ":" + stackFrames[2].GetFileLineNumber();

            while (lastEntries.Count > 100)
                lastEntries.RemoveAt(0);
            lastEntries.Add(new LogEntry { Source=debugInfo, EventType = eventType, Id = id, Message = message });

            if (LogEntry != null)
                LogEntry(debugInfo, eventType, id, message);
        }
    }
}
