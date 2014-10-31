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
        static void Main(string[] args)
        {
            //NameServer.NameServer ns = new NameServer.NameServer { BindingAddress = IPAddress.Parse("129.129.130.44"), Port = 5432, SearchAddress = "172.20.3.50:5062" };
            NameServer.NameServer ns = new NameServer.NameServer();
            ns.Start();
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();
        }
    }
}
