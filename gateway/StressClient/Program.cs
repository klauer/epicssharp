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
        static EpicsChannel<string>[] channels;

        static void Main(string[] args)
        {
            using (EpicsClient client = new EpicsClient())
            {
                //System.Diagnostics.Debugger.Launch();
                client.Configuration.SearchAddress = "129.129.130.44:6789";
                client.Configuration.WaitTimeout = 5000;

                if (args.Length > 0 && args[0] == "-m")
                {
                    //Console.WriteLine("Running monitor mode");
                    channels = new EpicsChannel<string>[100];
                    for (int j = 0; j < channels.Length; j++)
                    {
                        channels[j] = client.CreateChannel<string>("STRESS:INT:" + j / 2);
                        channels[j].MonitorChanged += new EpicsDelegate<string>(Program_MonitorChanged);
                    }
                    client.MultiConnect(channels);
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
                        //Console.Beep();
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
            string id = sender.ChannelName.Split(new char[] { ':' }).Last();
            if (id != newValue)
            {
                try
                {
                    Console.WriteLine(sender.ChannelName + ": " + sender.CID+"/"+sender.SID);
                }
                catch
                {

                }
                try
                {
                    Console.WriteLine(channels[int.Parse(newValue) * 2].ChannelName + ": " + channels[int.Parse(newValue) * 2].CID+"/"+channels[int.Parse(newValue) * 2].SID);
                }
                catch
                {

                }
                Console.WriteLine("!!!! Wrong value for channel " + sender.ChannelName + " (" + newValue + ")");
            }
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
