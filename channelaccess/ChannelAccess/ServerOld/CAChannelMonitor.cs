/*
 *  EpicsSharp - An EPICS Channel Access library for the .NET platform.
 *
 *  Copyright (C) 2013 - 2015  Paul Scherrer Institute, Switzerland
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicsSharp.ChannelAccess.Constants;

namespace EpicsSharp.ChannelAccess.ServerOld
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
                using (Record.CreateAtomicChange(false))
                    Channel.TcpConnection.Send(Channel.Server.Filter.MonitorChangeMessage(SubscriptionId, Channel.ClientId, Type, DataCount, val.ToByteArray(Type, Record)));
                Record.RecordProcessed += new EventHandler(Record_RecordProcessed);
            }
            catch (Exception e)
            {
                Channel.TcpConnection.Send(Channel.Server.Filter.MonitorChangeMessage(SubscriptionId, Channel.ClientId, Type, DataCount, new byte[0]));
            }
        }

        void Record_RecordProcessed(object sender, EventArgs e)
        {
            //If the client was started before the IOC then it is possible
            //that the client will request a value before the first "scan"
            //In which case ignore the request
            if (Record[Property] == null)
            {
                return;
            }

#warning need to implement deadband
            /*if (newValue is short || newValue is int || newValue is float || newValue is double)
            {
                Record.m
            }*/

            if (Record.IsDirty)
            {
                using (Record.CreateAtomicChange(false))
                    Channel.TcpConnection.Send(Channel.Server.Filter.MonitorChangeMessage(SubscriptionId, Channel.ClientId, Type, DataCount, Record[Property].ToByteArray(Type, Record)));
            }
        }

        public void Dispose()
        {
            Record.RecordProcessed -= new EventHandler(Record_RecordProcessed);
            Channel.TcpConnection.Send(Channel.Server.Filter.MonitorCloseMessage(Type, Channel.ServerId, SubscriptionId));
        }
    }
}
