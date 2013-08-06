using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using PBCaGw;

namespace GwService
{
    public partial class GatewayService : ServiceBase
    {
        Gateway gateway;
 
        public GatewayService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            gateway = new Gateway();
            gateway.LoadConfig();
            gateway.Start();
        }

        protected override void OnStop()
        {
            if(gateway != null)
                gateway.Dispose();
            gateway = null;
        }
    }
}
