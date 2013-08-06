using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using PBCaGw.Workers;

namespace PBCaGw
{
    /// <summary>
    /// Handles messages between workers.
    /// Can contain either a TCP/UDP packet or an EPICS message
    /// </summary>
    public class DataPacket : ICloneable /*, IDisposable */
    {
        public DataPacketKind Kind = DataPacketKind.RAW;
        public byte[] Data;
        public WorkerChain Chain { get; private set; }
        //public bool NeedToFlush = false;

        public IPEndPoint Destination;
        public IPEndPoint Sender;

        /// <summary>
        /// Allows to change the sending rules
        /// </summary>
        public bool ReverseAnswer = false;

        bool? extendedMessage;
        /// <summary>
        /// Checks if it's an extended message or not.
        /// To check we look at the payload site as well as the datacount.
        /// </summary>
        public bool ExtendedMessage
        {
            get
            {
                if (!extendedMessage.HasValue)
                    extendedMessage = (GetUInt16(2 ) == 0xFFFF && GetUInt16(6 ) == 0x0000);
                return extendedMessage.Value;
            }
        }

        ushort? command;
        /// <summary>
        /// The ChannelAccess command
        /// </summary>
        public UInt16 Command
        {
            get
            {
                if (!command.HasValue)
                    command = GetUInt16(0 );
                return command.Value;
            }
            set
            {
                command = value;
                SetUInt16(0 , value);
            }
        }

        uint? payloadSize;
        /// <summary>
        /// Payload size either on bytes 2-4 or 16-20
        /// </summary>
        public UInt32 PayloadSize
        {
            get
            {
                if (!payloadSize.HasValue)
                {
                    payloadSize = ExtendedMessage ? GetUInt32(16 ) : GetUInt16(2 );
                }
                return payloadSize.Value;
            }
            set
            {
                this.SetUInt16(2 , (ushort)value);
            }
        }

        /// <summary>
        /// Data type on bytes 4-6
        /// </summary>
        public UInt16 DataType
        {
            get
            {
                return GetUInt16(4);
            }
            set
            {
                SetUInt16(4 , value);
            }
        }

        /// <summary>
        /// Data count either on bytes 6-8 or 20-24
        /// </summary>
        public UInt32 DataCount
        {
            get
            {
                if (ExtendedMessage)
                    return GetUInt32(20);
                return GetUInt16(6 );
            }
            set
            {
                if (ExtendedMessage)
                    SetUInt32(20 , value);
                else
                {
                    // Value is bigger than the limit, we should rebuild the message
                    if (value > 16000)
                    {
                        DataPacket oldPacket = (DataPacket)this.Clone();
                        Data = new byte[BufferSize + 8];
                        if (oldPacket.PayloadSize > 0)
                            Buffer.BlockCopy(oldPacket.Data, (int)oldPacket.HeaderSize, Data, 24, (int)oldPacket.PayloadSize);
                        this.Command = oldPacket.Command;
                        SetUInt32(16, oldPacket.PayloadSize); // extended payload
                        SetUInt16(2, 0xFFFF); // short payload
                        SetUInt16(6, 0x0000); // short datacount
                        this.Parameter1 = oldPacket.Parameter1;
                        this.Parameter2 = oldPacket.Parameter2;
                        SetUInt32(20, value); // extended datacount
                        DataType = oldPacket.DataType;
                    }
                    else
                        SetUInt16(6 , (UInt16)value);
                }
            }
        }

        /// <summary>
        /// Parameter 1 on bytes 8-12
        /// </summary>
        public UInt32 Parameter1
        {
            get
            {
                return GetUInt32(8 );
            }
            set
            {
                SetUInt32(8 , value);
            }
        }

        /// <summary>
        /// Paramter 2 on bytes 12-16
        /// </summary>
        public UInt32 Parameter2
        {
            get
            {
                return GetUInt32(12);
            }
            set
            {
                SetUInt32(12 , value);
            }
        }

        /// <summary>
        /// The full message size (header + payload).
        /// Can be either payload + 16 or payload + 24 in case of an extended message.
        /// </summary>
        public UInt32 MessageSize
        {
            get
            {
                return PayloadSize + HeaderSize;
            }
        }

        /// <summary>
        /// Returns the size of the header
        /// </summary>
        public UInt32 HeaderSize
        {
            get
            {
                return (UInt32)(ExtendedMessage ? 24 : 16);
            }
        }

        /// <summary>
        /// Checks (by checking the buffer size) if we have the full header or not.
        /// </summary>
        public bool HasCompleteHeader
        {
            get
            {
                if (BufferSize < 16 || ExtendedMessage && BufferSize < 24)
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Retreives the payload as string.
        /// </summary>
        /// <returns></returns>
        public string GetDataAsString()
        {
            // If data is smaller than what it should... return empty string
            if ((ExtendedMessage ? 24 : 16) + (int)PayloadSize > BufferSize)
                return "";
            string ret = Encoding.ASCII.GetString(Data, (ExtendedMessage ? 24 : 16) + Offset, (int)PayloadSize);
            int indexOf = ret.IndexOf('\0');
            if (indexOf != -1)
                ret = ret.Substring(0, indexOf);
            return ret;
        }

        public void SetDataAsString(string str)
        {
            byte[] b = Encoding.ASCII.GetBytes(str);
            Array.Clear(Data, 16 + Offset, BufferSize - 16);
            Buffer.BlockCopy(b, 0, Data, 16 + Offset, b.Length);
        }

        private DataPacket()
        {
        }

        /// <summary>
        /// Returns an UInt16 at a given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public UInt16 GetUInt16(int position)
        {
            return (UInt16)(((uint)Data[position + Offset] << 8)
                | Data[position + 1 + Offset]);
        }

        /// <summary>
        /// Returns an UInt32 at a given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public UInt32 GetUInt32(int position)
        {
            return ((uint)Data[position + 0 + Offset] << 24)
                | ((uint)Data[position + 1 + Offset] << 16)
                | ((uint)Data[position + 2 + Offset] << 8)
                | Data[position + 3 + Offset];
        }

        /// <summary>
        /// Writes an UInt16 at a given position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public void SetUInt16(int position, UInt16 value)
        {
            Data[position + Offset] = (byte)((value & 0xFF00u) >> 8);
            Data[position + 1 + Offset] = (byte)((value) & 0xFFu);
        }

        public void SetBytes(int position, byte[] buff)
        {
            Buffer.BlockCopy(buff, Offset, Data, position, buff.Length);
        }

        /// <summary>
        /// Writes an UInt32 at a given position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="value"></param>
        public void SetUInt32(int position, UInt32 value)
        {
            Data[position + 0 + Offset] = (byte)((value & 0xFF000000u) >> 24);
            Data[position + 1 + Offset] = (byte)((value & 0x00FF0000u) >> 16);
            Data[position + 2 + Offset] = (byte)((value & 0x0000FF00u) >> 8);
            Data[position + 3 + Offset] = (byte)(value & 0x000000FFu);
        }

        /// <summary>
        /// Skips a given size from the data block
        /// </summary>
        /// <param name="size"></param>
        public DataPacket SkipSize(UInt32 size, bool reuse = false)
        {
            if (reuse)
            {
                DataPacket p = new DataPacket();
                p.Sender = this.Sender;
                p.Destination = this.Destination;
                p.Kind = this.Kind;
                p.Chain = this.Chain;
                p.bufferSize = BufferSize - (int)size;
                p.Offset = Offset + (int)size;
                p.Data = Data;
                return p;
            }
            else
            {
                DataPacket p = DataPacket.Create(BufferSize - (int)size);
                p.Sender = this.Sender;
                p.Destination = this.Destination;
                p.Kind = this.Kind;
                p.Chain = this.Chain;
                Buffer.BlockCopy(this.Data, (int)size, p.Data, 0, this.BufferSize - (int)size);
                return p;
            }
        }

        /// <summary>
        /// Clone this packet, creating an exact copy.
        /// As the clone function is an implementation of IClonable it must return an object.
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            DataPacket p = DataPacket.Create(this.BufferSize);
            Buffer.BlockCopy(this.Data, this.Offset, p.Data, 0, this.BufferSize);
            p.bufferSize = this.bufferSize;
            p.Sender = this.Sender;
            p.Destination = this.Destination;
            p.Kind = this.Kind;
            p.Chain = this.Chain;
            return p;
        }

        static readonly Dictionary<int, Stack<DataPacket>> storedPackets = new Dictionary<int, Stack<DataPacket>>();

        /*public void Dispose()
        {
            if (this.Data.Length < 200)
            {
                this.Chain = null;
                this.Sender = null;
                this.Destination = null;
                this.command = null;
                this.extendedMessage = null;
                this.payloadSize = null;
                this.NeedToFlush = false;

                lock (storedPackets)
                {
                    if (!storedPackets.ContainsKey(this.Data.Length))
                        storedPackets.Add(this.Data.Length, new Stack<DataPacket>());
                    DiagnosticServer.NbPooledPacket++;
                    storedPackets[this.Data.Length].Push(this);
                }
            }
        }*/

        public static void ShowPools()
        {
            lock (storedPackets)
            {
                foreach (var i in storedPackets.OrderBy(row => row.Key))
                {
                    Console.WriteLine("" + i.Key + ": " + i.Value.Count);
                }
            }
        }

        public static DataPacket Create(int size)
        {
            DataPacket p;
            /*if (size < 200)
            {
                lock (storedPackets)
                {
                    if (storedPackets.ContainsKey(size))
                    {
                        if (storedPackets[size].Count > 0)
                        {
                            p = storedPackets[size].Pop();
                            p.disposedBy = null;
                            DiagnosticServer.NbPooledPacket--;
                            return p;
                        }
                    }
                }
            }*/

            DiagnosticServer.NbNewData++;
            p = new DataPacket();
            p.Data = new byte[size];
            return p;
        }

        public static DataPacket Create(byte[] buff)
        {
            DataPacket p = DataPacket.Create(buff.Length);
            Buffer.BlockCopy(buff, 0, p.Data, 0, buff.Length);
            return p;
        }

        /// <summary>
        /// Creates a new message with the given payload size and sets the payload size correctly.
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <param name="chain"> </param>
        public static DataPacket Create(int payloadSize, WorkerChain chain)
        {
            DataPacket p = Create(payloadSize + 16);
            p.Chain = chain;
            p.SetUInt16(2, (UInt16)payloadSize);
            return p;
        }

        public int Offset { get; private set; }

        int bufferSize = 0;
        public int BufferSize
        {
            get
            {
                return bufferSize == 0 ? Data.Length : bufferSize;
            }
        }

        /// <summary>
        /// Creates a new message based on the byte buffer however use only the first "size" byte for it.
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="size"></param>
        /// <param name="chain"> </param>
        /// <param name="reuseBuffer"></param>
        public static DataPacket Create(byte[] buff, int size, WorkerChain chain, bool reuseBuffer = false)
        {
            if (reuseBuffer)
            {
                DataPacket p = new DataPacket();
                p.Data = buff;
                p.Chain = chain;
                p.bufferSize = size;
                return p;
            }
            else
            {
                DataPacket p = Create(size);
                p.Chain = chain;
                Buffer.BlockCopy(buff, 0, p.Data, 0, size);
                return p;
            }
        }

        /*/// <summary>
        /// Creates a new message based on the byte buffer however use only the first "size" byte for it.
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="size"></param>
        /// <param name="chain"> </param>
        public static DataPacket Create(byte[] buff, int size, WorkerChain chain)
        {
            DataPacket p = Create(size);
            p.Chain = chain;
            Buffer.BlockCopy(buff, 0, p.Data, 0, size);
            return p;
        }*/

        /// <summary>
        /// Creates a new message based on an existing packed and use the "size" to extract only the first part.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="size"></param>
        public static DataPacket Create(DataPacket packet, UInt32 size, bool reuse=false)
        {
            if (reuse)
            {
                DataPacket p = new DataPacket();
                p.Kind = packet.Kind;
                p.Chain = packet.Chain;
                p.Sender = packet.Sender;
                p.Destination = packet.Destination;
                p.Offset = packet.Offset;
                p.bufferSize = (int)size;
                p.Data = packet.Data;
                return p;
            }
            else
            {
                DataPacket p = Create((int)size);
                p.Kind = packet.Kind;
                p.Chain = packet.Chain;
                p.Sender = packet.Sender;
                p.Destination = packet.Destination;
                if (size > packet.BufferSize)
                    Buffer.BlockCopy(packet.Data, packet.Offset, p.Data, 0, packet.BufferSize);
                else
                    Buffer.BlockCopy(packet.Data, packet.Offset, p.Data, 0, (int)size);
                return p;
            }
        }

        /// <summary>
        /// Merges 2 packets together
        /// </summary>
        /// <param name="remaining"></param>
        /// <param name="newPacket"></param>
        public static DataPacket Create(DataPacket remaining, DataPacket newPacket)
        {
            DataPacket p = Create(remaining.BufferSize + newPacket.BufferSize);
            p.Sender = remaining.Sender;
            p.Destination = remaining.Destination;
            p.Chain = remaining.Chain;
            p.Kind = remaining.Kind;
            Buffer.BlockCopy(remaining.Data, remaining.Offset, p.Data, 0, remaining.BufferSize);
            Buffer.BlockCopy(newPacket.Data, newPacket.Offset, p.Data, remaining.BufferSize, newPacket.BufferSize);
            return p;
        }

        public static int Padding(int size)
        {
            if (size % 8 == 0)
                return 8;
            return (8 - (size % 8));
        }
    }
}
