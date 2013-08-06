using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GatewayDebugData
{
    public interface IDebugDataAccess
    {
        int GetInt();
        string GetString();
        void Send(int data);
        void Send(string data);
        void Send(byte[] data);
    }
}
