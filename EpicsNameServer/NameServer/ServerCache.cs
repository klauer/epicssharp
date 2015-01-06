using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NameServer
{
    class ServerCache
    {
        SemaphoreSlim locker= new SemaphoreSlim(1, 1);
        Dictionary<IPEndPoint, TcpLink> cache = new Dictionary<IPEndPoint, TcpLink>();
        readonly private NameServer nameServer;

        public ServerCache(NameServer nameServer)
        {
            this.nameServer = nameServer;
        }
        
        public TcpLink this[IPEndPoint key]
        {
            get
            {
                locker.Wait();
                try
                {
                    if (!cache.ContainsKey(key))
                    { 
                        cache.Add(key, new TcpLink(key));
                        cache[key].LostConnection += ServerCache_LostConnection;     
                    }
                    return cache[key];
                }
                catch
                {

                }
                finally
                {
                    locker.Release();
                }
                return null;
            }
        }

        void ServerCache_LostConnection(object sender, EventArgs e)
        {
            locker.Wait();
            try
            {
                cache.Remove(((TcpLink)sender).EndPoint);
            }
            catch
            {

            }
            finally
            {
                locker.Release();
            }

        }
    }
}
