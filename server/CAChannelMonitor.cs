using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaSharpServer.Constants;

namespace CaSharpServer
{
    /// <summary>
    /// Monitor subscription and handling
    /// </summary>
    internal class CAChannelMonitor : IDisposable
    {
        CARecord Record;
        string Property;
        CAServerChannel Channel;
        EpicsType Type;
        int DataCount = 1;
        MonitorMask MonitorMask;
        int SubscriptionId;
        object lastValue = null;

        internal CAChannelMonitor(CARecord record, string property, CAServerChannel channel,
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
                Channel.TcpConnection.Send(Channel.Server.Filter.MonitorChangeMessage(SubscriptionId, Channel.ClientId, Type, DataCount, val.ToByteArray(Type, Record)));
                lastValue = Record[Property];
                Record.RecordProcessed += new EventHandler(Record_RecordProcessed);
            }
            catch (Exception e)
            {
                Channel.TcpConnection.Send(Channel.Server.Filter.MonitorChangeMessage(SubscriptionId, Channel.ClientId, Type, DataCount, new byte[0]));
            }
        }

        void Record_RecordProcessed(object sender, EventArgs e)
        {
            object newValue = Record[Property];
            object newValueCheck = newValue;

            //If the client was started before the IOC then it is possible
            //that the client will request a value before the first "scan"
            //In which case ignore the request
            if (Record[Property] == null)
            {
                return;
            }

            if (Record[Property].GetType().IsArray)
            {
                string full = "";
                foreach (object i in (System.Collections.IEnumerable)newValue)
                    full += i.GetHashCode() + ";";
                newValueCheck = full;
            }

#warning need to implement deadband
            /*if (newValue is short || newValue is int || newValue is float || newValue is double)
            {
                Record.m
            }*/

            if ("" + newValueCheck != "" + lastValue || Record.IsDirty)
            {
                Channel.TcpConnection.Send(Channel.Server.Filter.MonitorChangeMessage(SubscriptionId, Channel.ClientId, Type, DataCount, newValue.ToByteArray(Type, Record)));
                lastValue = newValueCheck;
            }
        }

        public void Dispose()
        {
            Record.RecordProcessed -= new EventHandler(Record_RecordProcessed);
            Channel.TcpConnection.Send(Channel.Server.Filter.MonitorCloseMessage(Type, Channel.ServerId, SubscriptionId));
        }
    }
}
