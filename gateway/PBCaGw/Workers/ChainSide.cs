using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBCaGw.Workers
{
    /// <summary>
    /// Used to store the information on which direction the chain is going
    /// </summary>
    public enum ChainSide
    {
        SIDE_A,
        SIDE_B,
        UDP_RESP_SIDE_A,
        UDP_RESP_SIDE_B,
        SERVER_CONN,
        DEBUG_PORT,
    }
}
