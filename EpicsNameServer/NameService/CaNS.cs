using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace NameService
{
    public partial class CaNS : ServiceBase
    {
        NameServer.NameServer ns;

        public CaNS()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ns = new NameServer.NameServer();
            ns.Start();
        }

        protected override void OnStop()
        {
            ns.Stop();
        }
    }
}
