using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;

namespace NameServer
{
    class NameEntry
    {
        SemaphoreSlim locker = new SemaphoreSlim(1, 1);

        public string Name { get; set; }
        public IPEndPoint Destination { get; set; }

        public List<WaitingClient> waitingList = null;
        DateTime lastSearch = DateTime.Now;
        private NameServer nameServer;
        internal uint SearchId;

        public NameEntry(NameServer nameServer)
        {
            this.nameServer = nameServer;
        }

        internal void AnswerTo(IPEndPoint iPEndPoint, uint searchId)
        {
            locker.Wait();
            try
            {
                // Known destination
                if (Destination != null)
                {
                    DataPacket newPacket = DataPacket.Create(8 + 16);
                    newPacket.Command = (ushort)CommandID.CA_PROTO_SEARCH;
                    newPacket.SetIPAddress(8, Destination.Address);
                    newPacket.Parameter2 = searchId;
                    newPacket.DataType = (ushort)Destination.Port;
                    newPacket.DataCount = 0;
                    newPacket.SetUInt16(16, NameServer.CA_PROTO_VERSION);
                    newPacket.Destination = iPEndPoint;
                    nameServer.Send(newPacket);

                    Console.WriteLine("Sending back known channel");
                }
                else
                {
                    // Some are already waiting on this.
                    // Add the latest request
                    if (waitingList != null)
                    {
                        CleanOldSearches();

                        if ((DateTime.Now - lastSearch).TotalSeconds > 1)
                        {
                            Console.WriteLine("Search again as a long time passed");
                            ForwardSearch();
                        }
                        if (!waitingList.Any(row => row.Destination != iPEndPoint && row.SearchId != row.SearchId))
                        {
                            waitingList.Add(new WaitingClient { Destination = iPEndPoint, SearchId = searchId });
                            Console.WriteLine("Adding to the waiting list");
                        }
                        return;
                    }

                    // Build a new list
                    // And send the search
                    waitingList = new List<WaitingClient>();
                    waitingList.Add(new WaitingClient { Destination = iPEndPoint, SearchId = searchId });

                    Console.WriteLine("Searching for the first time");

                    ForwardSearch();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                locker.Release();
            }
        }

        private void CleanOldSearches()
        {
            waitingList.RemoveAll(row => (DateTime.Now - row.CreatedOn).TotalSeconds > 2);
        }

        private void ForwardSearch()
        {
            nameServer.IdCache.Store(this);

            DataPacket SearchPacket = DataPacket.Create(16 + this.Name.Length + DataPacket.Padding(this.Name.Length));
            SearchPacket.Command = (ushort)CommandID.CA_PROTO_SEARCH;
            SearchPacket.DataType = 5;
            SearchPacket.DataCount = NameServer.CA_PROTO_VERSION;
            SearchPacket.Parameter1 = this.SearchId;
            SearchPacket.Parameter2 = this.SearchId;
            SearchPacket.SetDataAsString(this.Name);
            nameServer.SendSearch(SearchPacket);
        }

        internal void GotAnswer(DataPacket packet)
        {
            nameServer.IdCache.Release(this);
            locker.Wait();
            try
            {
                CleanOldSearches();
                Destination = new IPEndPoint(packet.Sender.Address, packet.DataType);

                DataPacket newPacket = DataPacket.Create(8 + 16);
                newPacket.Command = (ushort)CommandID.CA_PROTO_SEARCH;
                newPacket.SetIPAddress(8, Destination.Address);
                newPacket.DataType = (ushort)Destination.Port;
                newPacket.DataCount = 0;
                newPacket.SetUInt16(16, NameServer.CA_PROTO_VERSION);

                foreach (var i in waitingList)
                {
                    newPacket.Parameter2 = i.SearchId;
                    newPacket.Destination = i.Destination;
                    nameServer.Send(newPacket);
                }
                waitingList = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                locker.Release();
            }
        }
    }
}
