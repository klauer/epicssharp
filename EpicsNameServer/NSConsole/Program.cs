using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NSConsole
{
    class Program
    {
        static bool stop = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Ctrl + C to stop");
            Console.CancelKeyPress += Console_CancelKeyPress;           
            NameServer.NameServer ns = new NameServer.NameServer();
            ns.Start();
            while (!stop)
                Console.ReadKey();
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            stop = true;
        }
    }
}
