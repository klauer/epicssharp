using CaSharpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace StressServer
{
    class Program
    {

        static CAIntRecord[] intRecs;
        static CAIntRecord singleInt;

        static void Main(string[] args)
        {
            CaSharpServer.CAServer server = new CaSharpServer.CAServer(IPAddress.Parse("127.0.0.1"), int.Parse(args[0]), int.Parse(args[0]), 100+ int.Parse(args[0]));

            singleInt = server.CreateRecord<CaSharpServer.CAIntRecord>("STRESS:INT");
            singleInt.Scan = CaSharpServer.Constants.ScanAlgorithm.ON_CHANGE;
            singleInt.Value = 1234;

            intRecs = new CAIntRecord[int.Parse(args[2])];

            for (int i = 0; i < int.Parse(args[2]); i++)
            {
                intRecs[i] = server.CreateRecord<CaSharpServer.CAIntRecord>("STRESS:INT:" + (i + int.Parse(args[1])));
                intRecs[i].Scan = CaSharpServer.Constants.ScanAlgorithm.ON_CHANGE;
                intRecs[i].Value = (i + int.Parse(args[1]));
                //intRecs[i].Value = 1234 - i;
            }

            /*Thread produceThread = new Thread(new ThreadStart(ProduceData));
            produceThread.IsBackground = true;
            produceThread.Start();*/

            //Console.WriteLine("Server ready " + args[0] + " " + args[1] + " " + args[2]);

            while (true)
                Console.ReadKey();

        }

        static void ProduceData()
        {
            int loop = 0;
            while (true)
            {
                for (int i = 0; i < intRecs.Length; i++)
                    intRecs[i].Value = loop % intRecs.Length;
                loop++;
                Thread.Sleep(10);
            }
        }
    }
}
