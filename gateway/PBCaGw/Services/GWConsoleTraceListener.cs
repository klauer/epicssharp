using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PBCaGw.Services
{
    /// <summary>
    /// A colored, and more informative TraceListener than the default ConsoleTraceListener
    /// </summary>
    public class GWConsoleTraceListener : ConsoleTraceListener
    {
        /// <summary>
        /// Setup the console, and write the header
        /// </summary>
        static GWConsoleTraceListener()
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Date: Time:    Debug:".PadRight(51, ' ') + "Message:".PadRight(Console.BufferWidth - 51));
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (!Log.ShowAll && this.Filter != null && !this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            StackTrace stackTrace = new StackTrace(true);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            string debugInfo = stackFrames[3].GetMethod().ReflectedType.Name + "." + stackFrames[3].GetMethod().Name + ":" + stackFrames[3].GetFileLineNumber();
            if (debugInfo.Length > 35)
                debugInfo = debugInfo.Substring(debugInfo.Length - 35);
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.Write(Gateway.Now.ToShortDateString().Substring(0, 5) + " " + Gateway.Now.ToLongTimeString() + " " + debugInfo.PadRight(35, ' '));
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
