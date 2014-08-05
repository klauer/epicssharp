using System.Linq;
using System;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Cuts TCP or UDP packet is EPICS messages
    /// </summary>
    public class PacketSplitter : PacketWorker
    {
        DataPacket remainingPacket = null;
        uint dataMissing = 0;
        int currentPos = 0;

        public override void ProcessData(DataPacket packet)
        {
            // It's a debug first request
            if (remainingPacket == null && dataMissing == 0 && packet.Data[0] == 126)
            {
                // Let's rebuild the chain
                while (this.Chain.Count > 1)
                    this.Chain.RemoveLast();
                Worker w = new DebugPortWorker(Chain, this.ClientEndPoint, this.ServerEndPoint, packet);
                this.Chain.Add(w);
                this.Chain.Side = ChainSide.DEBUG_PORT;
                w.ProcessData(packet);
                return;
            }

            while (packet.BufferSize != 0)
            {
                // We had an incomplete packet, let's try to add the missing piece now
                if (dataMissing != 0)
                {
                    //Console.WriteLine("\n\rData missing...");
                    // The new packet is smaller than the missing piece
                    // Therefore send the whole as "BODY" and quit the splitter
                    if (packet.BufferSize < dataMissing)
                    {
                        dataMissing -= (uint)packet.BufferSize;
                        packet.Kind = DataPacketKind.BODY;
                        this.SendData(packet);
                        return;
                    }
                    // The new packet is bigger or equal than the missing piece
                    DataPacket p = DataPacket.Create(packet, dataMissing);
                    p.Kind = DataPacketKind.TAIL;
                    DiagnosticServer.NbMessages++;
                    this.SendData(p);
                    DataPacket newPacket = packet.SkipSize(dataMissing);
                    //packet.Dispose();
                    packet = newPacket;
                    dataMissing = 0;
                    continue;
                }

                // We had some left over, join with the current packet
                if (remainingPacket != null)
                {
                    //Console.WriteLine("\n\rJoining left over...");
                    if (currentPos != 0)
                    {
                        // With the new block we have more than enough
                        if (packet.BufferSize + currentPos >= remainingPacket.BufferSize)
                        {
                            int s = remainingPacket.BufferSize - currentPos;
                            Buffer.BlockCopy(packet.Data, 0, remainingPacket.Data, currentPos, s);
                            remainingPacket.Kind = DataPacketKind.COMPLETE;
                            this.SendData(remainingPacket);

                            remainingPacket = null;
                            currentPos = 0;

                            // Got all.
                            if (s == packet.BufferSize)
                            {
                                //packet.Dispose();
                                return;
                            }

                            remainingPacket = packet.SkipSize((uint)s, true);
                            //packet.Dispose();
                            packet = remainingPacket;
                            remainingPacket = null;
                            continue;
                        }
                        // Just add the missing piece
                        else
                        {
                            Buffer.BlockCopy(packet.Data, 0, remainingPacket.Data, currentPos, packet.BufferSize);
                            currentPos += packet.BufferSize;
                            return;
                        }
                    }
                    else
                    {
                        packet = DataPacket.Create(remainingPacket, packet);
                        remainingPacket = null;
                    }
                }

                // We don't even have a complete header, stop
                if (!packet.HasCompleteHeader)
                {
                    //Console.WriteLine("\r\nIncomplete packet...");
                    //remainingPacket = packet;
                    remainingPacket = DataPacket.Create(packet, (uint)packet.BufferSize, false);
                    return;
                }
                // Full packet, send it.
                if (packet.MessageSize == packet.BufferSize)
                {
                    //Console.WriteLine("\r\nComplete packet...");
                    packet.Kind = DataPacketKind.COMPLETE;
                    DiagnosticServer.NbMessages++;
                    this.SendData(packet);
                    /*DataPacket p = DataPacket.Create(packet, (uint)packet.BufferSize);
                    this.SendData(p);*/
                    return;
                }

                // More than one message in the packet, split and continue
                if (packet.MessageSize < packet.BufferSize)
                {
                    //Console.WriteLine("\n\rSplitting...");
                    DataPacket p = DataPacket.Create(packet, packet.MessageSize, true);
                    p.Kind = DataPacketKind.COMPLETE;
                    DiagnosticServer.NbMessages++;
                    this.SendData(p);
                    DataPacket newPacket = packet.SkipSize(packet.MessageSize, true);
                    //packet.Dispose();
                    packet = newPacket;
                }
                // Message bigger than packet.
                // Cannot be the case on UDP!
                else
                {
                    //Console.WriteLine("\n\rMissing some...");

                    remainingPacket = packet;
                    if (packet.HasCompleteHeader)
                    {
                        currentPos = packet.BufferSize;
                        if (remainingPacket == null)
                            return;
                        packet = DataPacket.Create(remainingPacket, remainingPacket.MessageSize);
                        remainingPacket = packet;
                    }
                    else
                    {
                        remainingPacket = (DataPacket)packet.Clone();
                    }
                    return;
                }
            }
        }

        public void Reset()
        {
            remainingPacket = null;
            dataMissing = 0;
            currentPos = 0;
        }
    }
}
