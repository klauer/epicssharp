using System;
using System.ComponentModel;

namespace PSI.EpicsClient2
{
    public delegate void EpicsDelegate<TType>(EpicsChannel<TType> sender, TType newValue);

    public class EpicsChannel<TType> : EpicsChannel
    {
        internal EpicsChannel(EpicsClient client, string channelName)
            : base(client, channelName)
        {
            MonitoredType = typeof(TType);
        }

        private event EpicsDelegate<TType> PrivMonitorChanged;

        public TType Get()
        {
            return base.Get<TType>();
        }

        public void Put(TType newValue)
        {
            base.Put<TType>(newValue);
        }

        /// <summary>
        /// Event-Monitor which calls as soon a change on the channel happened which fits into the defined
        /// Monitormask (channel.MonitorMask).<br />The properties channel.MonitorMask and channel.MonitorDataCount
        /// do touch the behavior of this event and can't be changed when a monitor is already connected.
        /// <example>
        /// EpicsClient client = new EpicsClient();<br/>
        /// EpicsChannel channel=clien.tCreateChannel("SEILER_C:CPU");<br/>
        /// channel.MonitorMask = MonitorMask.VALUE;<br/>
        /// channel.MonitorDataCount = 1;<br/>
        /// channel.MonitorChanged += new EpicsDelegate(channel_MonitorChanged);
        /// </example>
        /// </summary>
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public new event EpicsDelegate<TType> MonitorChanged
        {
            add
            {
                if (PrivMonitorChanged == null)
                {
                    AfterConnect(action =>
                                     {
                                         DataPacket p = DataPacket.Create(16 + 16);
                                         p.Command = (ushort)CommandID.CA_PROTO_EVENT_ADD;
                                         Type t = typeof(TType);
                                         if (t.IsArray)
                                             t = t.GetElementType();
                                         p.DataType = (ushort)TypeHandling.Lookup[t];
                                         p.DataCount = ChannelDataCount;
                                         p.Parameter1 = SID;
                                         p.Parameter2 = CID;

                                         p.SetUInt16(12 + 16, (ushort)MonitorMask);

                                         if(ioc != null)
                                            ioc.Send(p);
                                     });
                }
                PrivMonitorChanged += value;
            }
            remove
            {
                PrivMonitorChanged -= value;

                if (PrivMonitorChanged == null)
                {
                    DataPacket p = DataPacket.Create(16);
                    p.Command = (ushort)CommandID.CA_PROTO_EVENT_CANCEL;
                    p.DataType = (ushort)TypeHandling.Lookup[typeof(TType)];
                    p.DataCount = ChannelDataCount;
                    p.Parameter1 = SID;
                    p.Parameter2 = CID;
                    if (ioc != null)
                        ioc.Send(p);
                }
            }
        }

        internal override void Disconnect()
        {
            if (Disposed)
                return;
            if (ioc != null)
                ioc.RemoveChannel(this);
            lock (ConnectionLock)
            {
                Status = ChannelStatus.DISCONNECTED;
                ioc = null;
                SID = 0;

                if (PrivMonitorChanged != null)
                {
                    AfterConnect(action =>
                                     {
                                         //Console.WriteLine("Sending new event add");
                                         DataPacket p = DataPacket.Create(16 + 16);
                                         p.Command = (ushort)CommandID.CA_PROTO_EVENT_ADD;
                                         Type t = typeof(TType);
                                         if (t.IsArray)
                                             t = t.GetElementType();
                                         p.DataType = (ushort)TypeHandling.Lookup[t];
                                         p.DataCount = ChannelDataCount;
                                         p.Parameter1 = SID;
                                         p.Parameter2 = CID;

                                         p.SetUInt16(12 + 16, (ushort)MonitorMask);

                                         if (ioc != null)
                                             ioc.Send(p);
                                     });
                }
            }
        }

        internal override void UpdateMonitor(DataPacket packet)
        {
            if (Client.Configuration.DebugTiming)
            {
                lock (ElapsedTimings)
                {
                    if (!ElapsedTimings.ContainsKey("MonitorUpdate"))
                        ElapsedTimings.Add("MonitorUpdate", Stopwatch.Elapsed);
                }
            }
            RawData = packet;
            if (PrivMonitorChanged != null)
            {
                PrivMonitorChanged(this, DecodeData<TType>(MonitoredElements));
            }
        }
    }
}
