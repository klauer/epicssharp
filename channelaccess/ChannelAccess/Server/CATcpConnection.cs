/*
 *  EpicsSharp - An EPICS Channel Access library for the .NET platform.
 *
 *  Copyright (C) 2013 - 2014  Paul Scherrer Institute, Switzerland
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
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace EpicsSharp.ChannelAccess.Server
{
    /// <summary>
    /// TCP connection used to communicate with the server and built by the client.
    /// </summary>
    internal class CATcpConnection : IDisposable
    {
        Socket Socket;
        CAServer Server;
        string remoteKey;
        byte[] buffer = new byte[1024];
        Pipe pipe = null;
        Thread processData;
        public event EventHandler Closing;

        internal CATcpConnection(Socket socket, CAServer server)
        {
            pipe = new Pipe();

            Socket = socket;

            processData = new Thread(new ThreadStart(BackgroundProcess));
            processData.IsBackground = true;
            processData.Start();

            Server = server;
            remoteKey = Socket.RemoteEndPoint.ToString();
            Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveData, null);

            // Send version
            Socket.Send(new byte[] { 0, 0, 0, 0, 0, 0, 0, 11, 0, 0, 0, 0, 0, 0, 0, 0 });

            Closed = false;
        }

        void ReceiveData(IAsyncResult ar)
        {
            int n = 0;
            try
            {
                n = Socket.EndReceive(ar);
            }
            catch
            {
                Dispose();
                return;
            }
            // Time to quit!
            if (n == 0)
            {
                Dispose();
                return;
            }

            try
            {
                if (n > 0)
                {
                    pipe.Write(buffer, 0, n);
                }
                Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveData, null);
            }
            catch
            {
                Dispose();
            }
        }

        void BackgroundProcess()
        {
            try
            {
                while (Socket.Connected)
                {
                    ProcessReceivedData(pipe);
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + "\r\n" + ex.StackTrace);
            }
        }

        protected void ProcessReceivedData(Pipe pipe)
        {
            try
            {
                Server.Filter.ProcessReceivedData(pipe, Socket.RemoteEndPoint, 0, false);
            }
            catch (NullReferenceException exc)
            {
                while (Server == null)
                    Thread.Sleep(10);

                try
                {
                    Server.Filter.ProcessReceivedData(pipe, Socket.RemoteEndPoint, 0, false);
                }
                catch
                {
                    this.Dispose();
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message + "\n\r" + e.StackTrace);
                this.Dispose();
            }
        }

        /// <summary>
        /// Send bytes to the socket.
        /// If the data is bigger than 1000 bytes it will be splitted in chunks of 1000 bytes.
        /// </summary>
        /// <param name="data"></param>
        internal void Send(byte[] data)
        {
            try
            {
                lock (Socket)
                {
                    if (data.Length <= 1000)
                        Socket.Send(data);
                    else
                    {
                        int nbRemaining = data.Length;
                        while (nbRemaining > 0)
                        {
                            byte[] buff;
                            if (nbRemaining > 1000)
                            {
                                buff = new byte[1000];
                                Array.Copy(data, data.Length - nbRemaining, buff, 0, 1000);
                                nbRemaining -= 1000;
                            }
                            else
                            {
                                buff = new byte[nbRemaining];
                                Array.Copy(data, data.Length - nbRemaining, buff, 0, nbRemaining);
                                nbRemaining = 0;
                            }
                            int n = Socket.Send(buff);
                        }
                    }
                }
            }
            catch
            {
                Dispose();
            }
        }

        public string Username { get; set; }

        public string Hostname { get; set; }

        public void Dispose()
        {
            if (Closed)
                return;
            Closed = true;
            try
            {
                if (Closing != null)
                    Closing(this, null);
            }
            catch
            {
            }
            try
            {
                Server.DropEpicsConnection(remoteKey);
            }
            catch
            {
            }
            try
            {
                Socket.Disconnect(false);
            }
            catch
            {
            }
            try
            {
                Socket.Close();
            }
            catch
            {
            }

            try
            {
                Socket.Dispose();
            }
            catch
            {
            }

            try
            {
                if (processData.ThreadState == System.Threading.ThreadState.Running)
                {
                    Thread.Sleep(10);
                    if (processData.ThreadState == System.Threading.ThreadState.Running)
                        processData.Abort();
                }
            }
            catch
            {
            }
        }

        public bool Closed { get; set; }

        public DateTime EchoLastSent = DateTime.Now;
    }
}
