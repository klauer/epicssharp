using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace GWLogViewer
{
    public delegate bool LogEntryDelegate(LogEntry row);

    public class Log : List<LogEntry>
    {
        public static Log ReadLog(string fileName)
        {
            return ReadLog(fileName, row => true);
        }

        public static Log ReadLog(string fileName, LogEntryDelegate condition)
        {
            Log result = new Log();
            using (StreamReader streamReader = new StreamReader(fileName))
            {
                while (!streamReader.EndOfStream)
                {
                    LogEntry current = new LogEntry();
                    string line = streamReader.ReadLine();
                    using (StringReader stringReader = new StringReader(line))
                    {
                        XmlReader reader = XmlReader.Create(stringReader);
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "entry")
                            {
                                if (condition(current))
                                    result.Add(current);
                            }
                            if (reader.NodeType != XmlNodeType.Element)
                                continue;
                            switch (reader.Name)
                            {
                                case "entry":
                                    current = new LogEntry();
                                    current.Date = DateTime.Parse(reader.GetAttribute("date") + " " + reader.GetAttribute("time"));
                                    current.EventType = (System.Diagnostics.TraceEventType)Enum.Parse(typeof(System.Diagnostics.TraceEventType), reader.GetAttribute("type"));
                                    current.ChainId = int.Parse(reader.GetAttribute("chainId"));
                                    break;
                                case "sender":
                                    current.Sender = reader.GetAttribute("class") + ":" + reader.GetAttribute("line");
                                    break;
                                case "message":
                                    reader.Read();
                                    current.Message = reader.Value;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
