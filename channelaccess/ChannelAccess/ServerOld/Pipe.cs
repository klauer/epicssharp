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
using System.Threading;

namespace EpicsSharp.ChannelAccess.ServerOld
{
    internal class Pipe : IDisposable
    {
        bool isDisposing = false;
        AutoResetEvent waitForNewData = new AutoResetEvent(false);
        AutoResetEvent waitForRead = new AutoResetEvent(false);

        Semaphore dataSemaphore = new Semaphore(1, 1);
        byte[] data;

        int readPosition = 0;
        int writePosition = 0;


        public Pipe()
        {
            data = new byte[32768];
        }
        public Pipe(int size)
        {
            data = new byte[size];
        }
        public void Write(byte[] Data)
        {
            Write(Data, 0, Data.Length);
        }

        public void Write(byte[] Data, int offset, int length)
        {
            if (isDisposing)
                return;

            int spare = 0;

            dataSemaphore.WaitOne();
            //trying to write more than there is space?!
            while ((writePosition > readPosition && data.Length - writePosition + readPosition - 1 < length) ||
                   (writePosition < readPosition && readPosition - writePosition - 1 < length) ||
                   (length > data.Length))
            {
                //calculate free space
                if (writePosition < readPosition)
                    spare = readPosition - writePosition - 1;
                else
                    spare = data.Length - writePosition + readPosition - 1;


                dataSemaphore.Release();
                Write(Data, offset, spare);
                dataSemaphore.WaitOne();

                offset += spare;
                length -= spare;
            }
            dataSemaphore.Release();
            Thread.Sleep(0);
            dataSemaphore.WaitOne();

            //check if there is enough space to write 
            if (data.Length - writePosition == length)
            {
                Buffer.BlockCopy(Data, offset, data, writePosition, length);
                writePosition = 0;
            }
            else if (data.Length - writePosition > length)
            {
                Buffer.BlockCopy(Data, offset, data, writePosition, length);
                writePosition += length;
            }
            else
            {
                spare = data.Length - writePosition;
                Buffer.BlockCopy(Data, offset, data, writePosition, spare);
                Buffer.BlockCopy(Data, offset + spare, data, 0, length - spare);
                writePosition = length - spare;
            }

            dataSemaphore.Release();

            waitForNewData.Set();
        }

        public long AvailableBytes
        {
            get
            {
                long realLength = 0;
                dataSemaphore.WaitOne();
                if (writePosition < readPosition)
                    realLength = (data.Length - readPosition) + writePosition;
                else
                    realLength = (writePosition - readPosition);
                dataSemaphore.Release();

                return realLength;
            }
        }

        public byte[] Read(int size)
        {
            if (size == 0 || isDisposing)
                return new byte[0];

            int spare = 0;
            byte[] result = new byte[size];
            long realLength = 0;

            //if the readposition and the write osition is at the same position do
            //wait for new data
            dataSemaphore.WaitOne();
            if (writePosition == readPosition)
            {
                dataSemaphore.Release();
                waitForNewData.WaitOne();
            }
            else
                dataSemaphore.Release();

            //be sure we have enough data
            do
            {
                if (isDisposing)
                    return new byte[0];

                dataSemaphore.WaitOne();
                if (writePosition < readPosition)
                    realLength = (data.Length - readPosition) + writePosition;
                else
                    realLength = (writePosition - readPosition);
                dataSemaphore.Release();

                //if there is not enough data to read, it will wait till new arrives or
                // one second passed. because it could be, that due to some really bad luck
                // the new data just happened between the lock open and the wait.
                if (size > realLength)
                    waitForNewData.WaitOne();

            }
            while (size > realLength);

            if (isDisposing)
                return new byte[0];

            dataSemaphore.WaitOne();
            //they are on the same loop
            if (writePosition > readPosition)
            {
                Buffer.BlockCopy(data, readPosition, result, 0, size);
                readPosition += size;
            }
            else
            {
                if ((data.Length - readPosition) >= size)
                {
                    Buffer.BlockCopy(data, readPosition, result, 0, size);
                    readPosition += size;
                }
                else
                {
                    spare = (data.Length - readPosition);
                    Buffer.BlockCopy(data, readPosition, result, 0, spare);
                    Buffer.BlockCopy(data, 0, result, spare, (size - spare));
                    readPosition = (size - spare);
                }
            }
            dataSemaphore.Release();

            waitForRead.Set();
            return result;
        }

        public int ReadInt()
        {
            return BitConverter.ToInt32(Read(4), 0);
        }

        public void Flush()
        {
            dataSemaphore.WaitOne();
            readPosition = 0;
            writePosition = 0;
            dataSemaphore.Release();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (isDisposing)
                return;
            else
                isDisposing = true;

            dataSemaphore.WaitOne();
            data = null;
            readPosition = 0;
            writePosition = 0;
            dataSemaphore.Release();

            //may trigger a waiting 
            waitForNewData.Set();
        }

        #endregion
    }
}
