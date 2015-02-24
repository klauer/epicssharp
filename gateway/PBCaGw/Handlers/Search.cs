using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using PBCaGw.Services;
using PBCaGw.Configurations;
using PBCaGw.Workers;

namespace PBCaGw.Handlers
{
    /// <summary>
    /// 6 (0x06) CA_PROTO_SEARCH
    /// </summary>
    class Search : CommandHandler
    {
        bool CompareNetC(IPAddress a, IPAddress b)
        {
            var ba = a.GetAddressBytes();
            var bb = b.GetAddressBytes();
            for (int i = 0; i < ba.Length - 1; i++)
            {
                if (ba[i] != bb[i])
                    return false;
            }
            return true;
        }

        public override void DoRequest(DataPacket packet, PBCaGw.Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            // From ourself? Skip it.
            if (packet.Sender.Equals(chain.ClientEndPoint))
                return;

            /*// Not coming from one of the allowed destination
            if (chain.Destinations.Any(i => CompareNetC(i.Address, packet.Sender.Address)))
                return;*/

            if (chain.Destinations == null)
                return;

            if (packet.Sender == null)
                return;

            if (Log.WillDisplay(TraceEventType.Verbose))
                Log.TraceEvent(System.Diagnostics.TraceEventType.Verbose, chain.ChainId, "Search from: " + packet.Sender + " " + packet.GetDataAsString());


            // It's a response
            if (packet.PayloadSize == 8)
            {
                DoResponse(packet, chain, sendData);
                return;
            }

            DiagnosticServer.NbSearches++;
            DataPacket newPacket = (DataPacket)packet.Clone();
            string channelName = packet.GetDataAsString();

            if (chain.Side == Workers.ChainSide.SIDE_A)
            {
                if (!chain.Gateway.Configuration.Security.EvaluateSideA(channelName, null, null, packet.Sender.Address.ToString()).Has(SecurityAccess.READ))
                    return;
            }
            else
            {
                if (!chain.Gateway.Configuration.Security.EvaluateSideB(channelName, null, null, packet.Sender.Address.ToString()).Has(SecurityAccess.READ))
                    return;
            }

            packet.Chain.Gateway.GotSearch(channelName,packet.Sender.Address.ToString());
            Record record;

            // Maybe this request is known.
            if (InfoService.ChannelEndPoint.Knows(channelName))
            {
                record = InfoService.ChannelEndPoint[channelName];
                bool knownChannel = false;
                if (record != null)
                {
                    if (chain.Side == ChainSide.SIDE_A && record.knownFromSideA == true)
                        knownChannel = true;
                    else if (chain.Side == ChainSide.SIDE_B && record.knownFromSideB == true)
                        knownChannel = true;
                }

                if (knownChannel)
                {
                    if (Log.WillDisplay(TraceEventType.Information))
                        Log.TraceEvent(TraceEventType.Information, chain.ChainId, "Cached search " + channelName);
                    newPacket = DataPacket.Create(8, packet.Chain);
                    newPacket.ReverseAnswer = true;
                    newPacket.Command = 6;
                    newPacket.Parameter1 = 0xffffffff;
                    newPacket.Parameter2 = packet.Parameter1;
                    if (chain.Side == Workers.ChainSide.SIDE_A)
                        newPacket.DataType = (UInt16)chain.Gateway.Configuration.LocalSideA.Port;
                    else
                        newPacket.DataType = (UInt16)chain.Gateway.Configuration.LocalSideB.Port;
                    newPacket.DataCount = 0;
                    newPacket.SetUInt16(16, Gateway.CA_PROTO_VERSION);

                    newPacket.Destination = packet.Sender;
                    sendData(newPacket);
                    return;
                }
            }

            if (chain.Side == ChainSide.SIDE_A)
                record = InfoService.SearchChannelEndPointA[channelName];
            else
                record = InfoService.SearchChannelEndPointB[channelName];

            // We have the info stored in the channel end point? Yes let's use it then.
            /*if (record == null
                && InfoService.ChannelEndPoint.Knows(channelName))
            {
                record = InfoService.ChannelEndPoint[channelName];
            }*/

            // First time, or never got answer, let's ask to the IOCS
            // Step 1
            if (record == null)
            {
                record = InfoService.SearchChannel.Create();
                // ReSharper disable PossibleInvalidOperationException
                uint gwcid = record.GWCID.Value;
                // ReSharper restore PossibleInvalidOperationException
                record.CID = packet.Parameter1;
                record.Client = packet.Sender;
                record.Channel = channelName;

                // Diagnostic search
                newPacket = (DataPacket)packet.Clone();
                newPacket.Parameter1 = gwcid;
                newPacket.Parameter2 = gwcid;
                newPacket.Destination = new IPEndPoint(chain.Gateway.Configuration.LocalSideB.Address, 7890);
                if (chain.Side == Workers.ChainSide.SIDE_B)
                    newPacket.ReverseAnswer = true;
                sendData(newPacket);

                foreach (IPEndPoint dest in chain.Destinations)
                {
                    newPacket = (DataPacket)packet.Clone();
                    newPacket.Parameter1 = gwcid;
                    newPacket.Parameter2 = gwcid;
                    newPacket.Destination = dest;
                    sendData(newPacket);
                }
            }
            // We have the info, therefore use the stored info to answer (cached)
            else
            {
                newPacket = DataPacket.Create(8, packet.Chain);
                newPacket.ReverseAnswer = true;
                newPacket.Command = 6;
                newPacket.Parameter1 = 0xffffffff;
                newPacket.Parameter2 = packet.Parameter1;
                if (chain.Side == Workers.ChainSide.SIDE_A)
                    newPacket.DataType = (UInt16)chain.Gateway.Configuration.LocalSideA.Port;
                else
                    newPacket.DataType = (UInt16)chain.Gateway.Configuration.LocalSideB.Port;
                newPacket.DataCount = 0;
                newPacket.SetUInt16(16, Gateway.CA_PROTO_VERSION);

                newPacket.Destination = packet.Sender;
                sendData(newPacket);
            }
        }

        // We get back the answer from the IOC
        public override void DoResponse(DataPacket packet, Workers.WorkerChain chain, DataPacketDelegate sendData)
        {
            Record record = InfoService.SearchChannel[packet.Parameter2];
            if (record == null)
                return;

            // Let's create the channel in parallel. That should speedup the communication.
            string channelName = record.Channel;

            IPAddress iocAddress = packet.Sender.Address;
            if (packet.Parameter1 != 0xFFFFFFFF)
                iocAddress = IPAddress.Parse("" + packet.Data[8] + "." + packet.Data[8 + 1] + "." + packet.Data[8 + 2] + "." + packet.Data[8 + 3]);


            if (Gateway.AutoCreateChannel)
            {
                lock (CreateChannel.lockObject)
                {
                    if (!InfoService.ChannelEndPoint.Knows(channelName))
                    {
                        Record channelInfo = InfoService.ChannelEndPoint[channelName];
                        if (Log.WillDisplay(TraceEventType.Information))
                            Log.TraceEvent(TraceEventType.Information, chain.ChainId, "Pre-create the channel " + channelName);

                        UInt32 gwcid = CidGenerator.Next();

                        channelInfo = InfoService.ChannelEndPoint.Create(record.Channel);
                        //channelInfo.Destination = new IPEndPoint(packet.Sender.Address, packet.DataType);
                        channelInfo.Destination = new IPEndPoint(iocAddress, packet.DataType);
                        channelInfo.GWCID = gwcid;

                        record = InfoService.ChannelCid.Create(gwcid);
                        record.Channel = channelName;
                        record.GWCID = gwcid;
                        //record.Destination = new IPEndPoint(packet.Sender.Address, packet.DataType);
                        record.Destination = new IPEndPoint(iocAddress, packet.DataType);

                        DataPacket channelPacket = DataPacket.Create(16 + channelName.Length + DataPacket.Padding(channelName.Length));
                        channelPacket.PayloadSize = (ushort)(channelName.Length + DataPacket.Padding(channelName.Length));
                        channelPacket.DataType = 0;
                        channelPacket.DataCount = 0;
                        channelPacket.Command = 18;
                        channelPacket.Parameter1 = gwcid;
                        // Version
                        channelPacket.Parameter2 = Gateway.CA_PROTO_VERSION;
                        //IPEndPoint dest = new IPEndPoint(packet.Sender.Address, packet.DataType);
                        IPEndPoint dest = new IPEndPoint(iocAddress, packet.DataType);
                        channelPacket.Destination = dest;
                        //channelPacket.NeedToFlush = true;
                        channelPacket.SetDataAsString(channelName);

                        Gateway gw = packet.Chain.Gateway;

                        System.Threading.ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
                                TcpManager.SendIocPacket(gw, channelPacket);
                                //TcpManager.FlushBuffer(dest);
                            }
                            catch
                            {
                            }
                        });

                        record = InfoService.SearchChannel[packet.Parameter2];
                    }
                }
            }

            /*if (chain.Side == ChainSide.SIDE_B || chain.Side == ChainSide.UDP_RESP_SIDE_A)
                InfoService.SearchChannelEndPointA.Remove(channelName);
            else
                InfoService.SearchChannelEndPointB.Remove(channelName);*/

            //IPEndPoint destination = new IPEndPoint(packet.Sender.Address, packet.DataType);
            IPEndPoint destination = new IPEndPoint(iocAddress, packet.DataType);
            WorkerChain ioc = TcpManager.GetIocChain(chain.Gateway, destination);
            // We can't connect to the IOC...
            if (ioc == null)
                return;
            if (!ioc.Channels.Any(row => row == channelName))
                ioc.Channels.Add(channelName);
            Record channel = InfoService.ChannelEndPoint[channelName];
            if (channel == null)
                channel = InfoService.ChannelEndPoint.Create(channelName);
            channel.Server = destination;

            if (chain.Side == ChainSide.SIDE_B || chain.Side == ChainSide.UDP_RESP_SIDE_A)
                channel.knownFromSideA = true;
            else
                channel.knownFromSideA = false;


            if (record == null || record.CID == null)
                return;
            // Auto-creation of the channels after a restart
            if (record.CID == 0)
            {
                if (Log.WillDisplay(TraceEventType.Information))
                    Log.TraceEvent(TraceEventType.Information, chain.ChainId, "Recovered channel " + channelName);
                return;
            }

            DataPacket newPacket = (DataPacket)packet.Clone();

            if (packet.Chain.Gateway.Configuration.ConfigurationType == ConfigurationType.BIDIRECTIONAL)
            {
                if (chain.Side == Workers.ChainSide.SIDE_A)
                    newPacket.DataType = (UInt16)chain.Gateway.Configuration.LocalSideB.Port;
                else
                    newPacket.DataType = (UInt16)chain.Gateway.Configuration.LocalSideA.Port;
            }
            else if (chain.Side == Workers.ChainSide.UDP_RESP_SIDE_A)
                newPacket.DataType = (UInt16)chain.Gateway.Configuration.LocalSideA.Port;
            else
                newPacket.DataType = (UInt16)chain.Gateway.Configuration.LocalSideB.Port;
            newPacket.Parameter1 = 0xffffffff;
            newPacket.Parameter2 = record.CID.Value;
            newPacket.Destination = record.Client;
            newPacket.SetUInt16(16, Gateway.CA_PROTO_VERSION);

            sendData(newPacket);
        }
    }
}
