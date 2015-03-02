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
        SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        Dictionary<string, TcpLink> cache = new Dictionary<string, TcpLink>();
        readonly private NameServer nameServer;

        public ServerCache(NameServer nameServer)
        {
            this.nameServer = nameServer;
        }

        public TcpLink this[string key]
        {
            get
            {
                locker.Wait();
                try
                {
                    if (!cache.ContainsKey(key))
                    {
                        var link = new TcpLink(key, this.nameServer);
                        if (link.IsConnected)
                            cache.Add(key, link);
                        else
                        {
                            return null;
                        }
                        cache[key].LostConnection += ServerCache_LostConnection;
                    }
                    return cache[key];
                }
                catch (Exception ex)
                {
                    Log.Write(System.Diagnostics.TraceEventType.Critical, "Error in ServerCache: " + ex.ToString() + "\r\n" + ex.StackTrace);
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
                IPEndPoint ep = ((TcpLink)sender).EndPoint;
                cache[ep.Address.ToString() + ":" + ep.Port].LostConnection -= ServerCache_LostConnection;
                cache.Remove(ep.Address.ToString() + ":" + ep.Port);
            }
            catch (Exception ex)
            {
                Log.Write(System.Diagnostics.TraceEventType.Critical, "Error in ServerCache: " + ex.ToString() + "\r\n" + ex.StackTrace);
            }
            finally
            {
                locker.Release();
            }

        }

        public void StopAll()
        {
            List<TcpLink> toDispose = new List<TcpLink>();
            locker.Wait();
            try
            {
                toDispose = cache.Values.ToList();
                cache.Clear();
            }
            catch
            {

            }
            finally
            {
                locker.Release();
            }

            foreach (var i in toDispose)
            {
                try
                {
                    i.Dispose();
                }
                catch
                {

                }
            }
        }
    }
}
