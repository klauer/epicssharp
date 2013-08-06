using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Deals with UDP or TCP receivers.
    /// Don't need the ProcessData as the worker will not receive
    /// any data from previous workers being the first of the chain.
    /// </summary>
    public abstract class ReceiverWorker : Worker, IDisposable
    {
        public override sealed void ProcessData(DataPacket packet)
        {
            throw new NotImplementedException();
        }
    }
}
