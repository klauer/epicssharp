using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PSI.EpicsClient2;
using System.Threading;

namespace StressClient
{
    class Program
    {
        static void Main(string[] args)
        {
            using (EpicsClient client = new EpicsClient())
            {
                client.Configuration.SearchAddress="129.129.130.87:6789";

                if (args.Length > 0 && args[0] == "-m")
                {
                    //Console.WriteLine("Running monitor mode");
                    EpicsChannel<string>[] channels = new EpicsChannel<string>[100];
                    for (int j = 0; j < channels.Length; j++)
                    {
                        channels[j] = client.CreateChannel<string>("STRESS:INT:" + j / 2);
                        channels[j].MonitorChanged += new EpicsDelegate<string>(Program_MonitorChanged);
                    }
                    Thread.Sleep(5000);
                    int nbNotConnected = 0;
                    for (int j = 0; j < channels.Length; j++)
                    {
                        if (channels[j].Status != ChannelStatus.CONNECTED)
                            nbNotConnected++;
                    }

                    if (nbNotConnected > 0)
                    {
                        Console.WriteLine("Channels not connected: " + nbNotConnected);
                        Console.Beep();
                        Thread.Sleep(10000);
                    }

                    for (int j = 0; j < channels.Length; j++)
                    {
                        channels[j].Dispose();
                    }
                }
                else
                {
                    for (int i = 0; i < 10; i++)
                    {
                        //Console.WriteLine("Create channel");
                        EpicsChannel<string> channel = client.CreateChannel<string>("STRESS:INT");
                        channel.StatusChanged += new EpicsStatusDelegate(channel_StatusChanged);
                        try
                        {
                            //Console.WriteLine("Get");
                            for (int j = 0; j < 10; j++)
                            {
                                string val = channel.Get();
                                if (val != "1234")
                                    Console.WriteLine("Wrong value!");
                                //Console.WriteLine("Got " + val);
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Didn't got back!");
                            //Console.Beep();
                        }
                        channel.Dispose();
                        Thread.Sleep(10);
                    }
                    //Console.WriteLine("Disposed");
                }
            }
        }

        static void Program_MonitorChanged(EpicsChannel<string> sender, string newValue)
        {
            //Console.WriteLine(newValue);
        }

        static void channel_StatusChanged(EpicsChannel sender, ChannelStatus newStatus)
        {
            //Console.WriteLine("Status: " + newStatus.ToString());
        }

        static void ExceptionContainer_ExceptionCaught(Exception caughtException)
        {
            //Console.WriteLine("Client error: " + caughtException.ToString());
        }
    }
}
