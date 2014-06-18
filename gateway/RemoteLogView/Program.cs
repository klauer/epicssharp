using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PSI.EpicsClient2;

namespace RemoteLogView
{
    class Program
    {
        static void Main(string[] args)
        {
            EpicsClient client = new EpicsClient();
            /*client.Config.ServerList.Add("129.129.130.87:5432");
            EpicsChannel<sbyte[]> screen = client.CreateChannel<sbyte[]>("PBGW:LOGVIEW");*/
            client.Configuration.SearchAddress="172.22.200.117:5062";
            EpicsChannel<sbyte[]> screen = client.CreateChannel<sbyte[]>("HIPA-TEST-GW:LOGVIEW");
            //screen.MonitorChanged += new EpicsDelegate<sbyte[]>(screen_MonitorChanged);

            Console.WindowWidth = 200;
            Console.WindowHeight = 100;
            Console.Clear();

            Console.ReadKey();
        }

        static void ExceptionContainer_ExceptionCaught(Exception caughtException)
        {
        }

        static void screen_MonitorChanged(EpicsChannel<sbyte[]> sender, sbyte[] newValue)
        {
            Console.Clear();
            for (int y = 0; y < 99; y++)
            {
                List<Byte> bytes = new List<byte>();
                for (int x = 0; x < 200; x++)
                {
                    if (newValue[x + y * 200] == 0)
                        break;
                    bytes.Add((byte)newValue[x + y * 200]);
                }
                string line = Encoding.ASCII.GetString(bytes.ToArray());
                Console.CursorLeft = 0;
                Console.CursorTop = y;
                Console.Write(line);
            }
        }
    }
}
