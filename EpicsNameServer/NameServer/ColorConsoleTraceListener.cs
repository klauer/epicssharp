using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NameServer
{
    class ColorConsoleTraceListener : ConsoleTraceListener
    {
        static ColorConsoleTraceListener()
        {
            Console.WindowWidth = 100;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message, object[] objs)
        {
            if (this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            string debugInfo = objs[0] + "@" + objs[1] + ":" + objs[2];
            if (debugInfo.Length > 35)
                debugInfo = debugInfo.Substring(debugInfo.Length - 35);
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.Write(DateTime.Now.ToShortDateString().Substring(0, 5) + " " + DateTime.Now.ToLongTimeString() + " " + debugInfo.PadRight(35, ' '));
            //Console.Write(DateTime.Now.ToString("hh:mm:ss.ffffff") + " " + debugInfo.PadRight(35, ' '));

            Console.BackgroundColor = ConsoleColor.Black;
            switch (eventType)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case TraceEventType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case TraceEventType.Information:
                case TraceEventType.Verbose:
                case TraceEventType.Resume:
                case TraceEventType.Suspend:
                case TraceEventType.Transfer:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case TraceEventType.Stop:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case TraceEventType.Start:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    break;
            }
            Console.WriteLine(" " + message);

            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
