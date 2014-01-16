using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaSharpServer;
using System.Net;

namespace LittleServer
{
    class Program
    {
        static int counter = 0;
        static CAIntRecord intRecord;
        static CAIntArrayRecord intArray;
        static CAStringRecord strRecord;

        static void Main(string[] args)
        {
            CAServer server = new CAServer(IPAddress.Parse("129.129.130.118"), 5062, 5062);
            intRecord = server.CreateRecord<CAIntRecord>("PCTOTO2:INT");
            intRecord.PrepareRecord += new EventHandler(intRecord_PrepareRecord);
            intRecord.Scan = CaSharpServer.Constants.ScanAlgorithm.HZ10;

            intArray = server.CreateArrayRecord<CAIntArrayRecord>("PCTOTO2:ARR",1000);
            for (int i = 0; i < intArray.Length; i++)
                intArray.Value[i] = i;

            strRecord = server.CreateRecord<CAStringRecord>("PCTOTO2:STR");
            strRecord.Value = "Default";

            Console.ReadLine();
        }

        static void intRecord_PrepareRecord(object sender, EventArgs e)
        {
            counter++;
            intRecord.Value = counter;
        }
    }
}
