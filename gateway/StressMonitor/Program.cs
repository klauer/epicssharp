using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using PSI.EpicsClient2;
using System.Threading;

namespace StressMonitor
{
    class Program
    {
        static int nbEvents = 0;
        static List<EpicsChannel> channels;

        static void Main(string[] args)
        {
            string[] channelNames = File.ReadAllLines(Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName + "\\channel_list.txt");

            EpicsClient client = new EpicsClient();
            //client.Configuration.SearchAddress = "129.129.130.219:5062";
            client.Configuration.SearchAddress = "fin-cagw02:5062";

            /*List<EpicsChannel<string>> channels = channelNames.Select(client.CreateChannel<string>).ToList();
            foreach(var i in channels)
                i.MonitorChanged += new EpicsDelegate<string>(Channel_MonitorChanged);

            channels = channelNames.Select(client.CreateChannel).ToList();
            foreach (var i in channels)
            {
                i.MonitorChanged += new EpicsDelegate(Channel_MonitorChangedObject);
                i.StatusChanged += new EpicsStatusDelegate(i_StatusChanged);
            }*/
            List<EpicsChannel<PSI.EpicsClient2.ExtControl<string>>> channelExt = channelNames
                .Where(row=>!row.Contains("WAVE") && !row.Contains("PICTURE"))
                .Select(client.CreateChannel<PSI.EpicsClient2.ExtControl<string>>).ToList();
            foreach (var i in channelExt)
                i.MonitorChanged += new EpicsDelegate<PSI.EpicsClient2.ExtControl<string>>(Channel_MonitorChangedExtObject);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            while (true)
            {
                //var res=client.MultiGet<string>(channels.Where(row => row.Status == ChannelStatus.CONNECTED));
                /*foreach (var i in channels.Where(row => row.Status == ChannelStatus.CONNECTED))
                {
                    var s = i.Get<string>();
                }*/
                Thread.Sleep(1000);
                //int nbConnected = channels.Where(row => row.Status == ChannelStatus.CONNECTED).Count() + channelExt.Where(row => row.Status == ChannelStatus.CONNECTED).Count();
                //int nbConnected = channels.Count(row => row.Status == ChannelStatus.CONNECTED);
                int nbConnected = channelExt.Count(row => row.Status == ChannelStatus.CONNECTED);
                Console.Write("Events per sec: " + nbEvents + " / Connected: " + nbConnected + " (" + channelExt .Count+ ")                \r");

                nbEvents = 0;
            }
        }

        static ChannelStatus previousState=ChannelStatus.CONNECTED;
        static void i_StatusChanged(EpicsChannel sender, ChannelStatus newStatus)
        {
            if(newStatus == ChannelStatus.DISCONNECTED && previousState != newStatus)
                Console.WriteLine("\nClosed!");
            previousState = newStatus;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
        }

        static void Channel_MonitorChanged(EpicsChannel<string> sender, string newValue)
        {
            nbEvents++;
        }

        static void Channel_MonitorChangedObject(EpicsChannel sender, object newValue)
        {
            nbEvents++;
        }

        static void Channel_MonitorChangedExtObject(EpicsChannel<PSI.EpicsClient2.ExtControl<string>> sender, PSI.EpicsClient2.ExtControl<string> newValue)
        {
            nbEvents++;
        }
    }
}
