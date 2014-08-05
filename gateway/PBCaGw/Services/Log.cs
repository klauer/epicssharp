using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace PBCaGw.Services
{
    public static class Log
    {
        static readonly TraceSwitch traceSwitch;
        public static readonly TraceSource traceSource;
        static readonly Dictionary<TraceEventType, bool> cachedWillDisplay = new Dictionary<TraceEventType, bool>();

// ReSharper disable MemberCanBePrivate.Global
        public static bool Enabled { get; set; }
// ReSharper restore MemberCanBePrivate.Global

        public static bool ShowAll { get; set; }

        static Log()
        {
            Enabled = true;
            ShowAll = false;
            try
            {
                traceSwitch = new TraceSwitch("GatewaySwitch", "Configuration level for the gateway trace level.");
                traceSource = new TraceSource("GatewaySource");
            }
            catch
            {
            }

            try
            {
// ReSharper disable UnusedVariable
                int v = (int)traceSwitch.Level;
// ReSharper restore UnusedVariable
            }
            catch
            {
                traceSwitch = null;
            }

            // Cache all the event types (enumerate the TraceEventType and store it inside a dictionary)
            foreach (TraceEventType i in Enum.GetValues(typeof(TraceEventType)))
            {
                // Due to the debug window
                if(i == TraceEventType.Critical || i == TraceEventType.Error || i == TraceEventType.Start || i == TraceEventType.Stop)
                    cachedWillDisplay.Add(i,true);
                else
                    cachedWillDisplay.Add(i, CalcWillDisplay(i));
            }
        }

        /// <summary>
        /// Checks if a log could be display with the define eventType. Useful to shaves the call to Log.TraceEvent function call.
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        public static bool WillDisplay(TraceEventType eventType)
        {
            if (ShowAll)
                return true;
            if (!Enabled)
                return false;

            return cachedWillDisplay[eventType];
            //return true;
        }

        static bool CalcWillDisplay(TraceEventType eventType)
        {
            if (traceSwitch != null && (int)traceSwitch.Level < (int)eventType)
                return false;

            foreach (TraceListener i in traceSource.Listeners)
            {
                if (i.Filter == null)
                    return true;
                if (i.Filter.ShouldTrace(null, "", eventType, 0, "", null, null, null))
                    return true;
            }
            return false;
        }

        public static void TraceEvent(TraceEventType eventType, int chainId, string message)
        {
            if (!Enabled)
                return;

            try
            {
                traceSource.TraceEvent(eventType, chainId, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                DebugTraceListener.TraceEvent(eventType, chainId, message);
            }
            catch
            {
            }
        }
    }
}
