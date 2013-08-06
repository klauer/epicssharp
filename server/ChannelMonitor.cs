using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaSharpServer.Constants;

namespace CaSharpServer
{
    internal class ChannelMonitor : IDisposable
    {
        CARecord Record;
        string Property;
        CAServerChannel Channel;
        EpicsType Type;
        int DataCount = 1;
        MonitorMask MonitorMask;
        int SubscriptionId;
        object lastValue = null;

        internal ChannelMonitor(CARecord record, string property, CAServerChannel channel,
                                    EpicsType type, int dataCount, MonitorMask monitorMask, int subscriptionId)
        {
            Record = record;
            Property = property;
            Channel = channel;
            Type = type;
            DataCount = dataCount;
            MonitorMask = monitorMask;
            SubscriptionId = subscriptionId;

            try
            {
                object val = Record[Property.ToString()];
                if (val == null)
                    val = 0;
                byte[] realData = val.ToByteArray(Type, Record);
                Channel.sendMonitorChange(SubscriptionId, Type, DataCount, EpicsTransitionStatus.ECA_NORMAL, realData);
                lastValue = Record[Property];
                Record.RecordProcessed += new EventHandler(Record_RecordProcessed);
            }
            catch (Exception e)
            {
                Channel.sendMonitorChange(SubscriptionId, Type, DataCount, EpicsTransitionStatus.ECA_ADDFAIL, new byte[0]);
            }
        }

        void Record_RecordProcessed(object sender, EventArgs e)
        {
            object newValue = Record[Property];
#warning need to implement deadband
            /*if (newValue is short || newValue is int || newValue is float || newValue is double)
            {
                Record.m
            }*/

            /*if ("" + newValue != "" + lastValue)
            {*/
                Channel.sendMonitorChange(SubscriptionId, Type, DataCount, EpicsTransitionStatus.ECA_NORMAL,
                                          newValue.ToByteArray(Type, Record));
                newValue = lastValue;
            //}
        }

        public void Dispose()
        {
            Record.RecordProcessed -= new EventHandler(Record_RecordProcessed);
            Channel.sendMonitorClose(SubscriptionId, Type);
        }
    }
}
