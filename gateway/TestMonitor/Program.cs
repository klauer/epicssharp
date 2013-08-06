using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PSI.EpicsClient2;

namespace TestMonitor
{
    class Program
    {
        static EpicsChannel<string> channel;
        static bool monitored = true;

        static void Main(string[] args)
        {
            EpicsClient client = new EpicsClient();
            client.Configuration.SearchAddress = "129.129.130.87:5432";

            channel = client.CreateChannel<string>("ADG1:IST1:2");
            channel.StatusChanged += new EpicsStatusDelegate(channel_StatusChanged);
            channel.MonitorChanged += new EpicsDelegate<string>(channel_MonitorChanged);

            while (true)
            {
                switch (Console.ReadKey().KeyChar)
                {
                    case (char)27:
                        return;
                    case (char)13:
                        if (channel == null)
                        {
                            channel = client.CreateChannel<string>("ADG1:IST1:2");
                            channel.StatusChanged += new EpicsStatusDelegate(channel_StatusChanged);
                            channel.MonitorChanged += new EpicsDelegate<string>(channel_MonitorChanged);
                        }
                        else
                        {
                            channel.Dispose();
                            channel = null;
                        }
                        break;
                    case ' ':
                        monitored = !monitored;
                        if (monitored)
                            channel.MonitorChanged += new EpicsDelegate<string>(channel_MonitorChanged);
                        else
                            channel.MonitorChanged -= new EpicsDelegate<string>(channel_MonitorChanged);
                        break;
                    default:
                        break;
                }
            }
        }

        static void channel_MonitorChanged(EpicsChannel<string> sender, string newValue)
        {
            Console.WriteLine("Value: " + newValue);
        }

        static void channel_StatusChanged(EpicsChannel sender, ChannelStatus newStatus)
        {
            Console.WriteLine("Status: " + newStatus.ToString());
        }
    }
}
