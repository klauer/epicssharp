using System.Net;
using System;

namespace PBCaGw.Workers
{
    public delegate void ReceiveDataDelegate(DataPacket packet);

    /// <summary>
    /// Base class for all the workers of a processing chain.
    /// </summary>
    public abstract class Worker : IDisposable
    {
        public event ReceiveDataDelegate ReceiveData;

        public abstract void ProcessData(DataPacket packet);

        public WorkerChain Chain;

        public virtual IPEndPoint ServerEndPoint { get; set; }

        public virtual IPEndPoint ClientEndPoint { get; set; }

        /// <summary>
        /// Sends the DataPacket further in the chain
        /// </summary>
        /// <param name="packet"></param>
        public void SendData(DataPacket packet)
        {
            if (ReceiveData != null)
                ReceiveData(packet);
        }

        public virtual void Dispose()
        {
        }
    }
}
