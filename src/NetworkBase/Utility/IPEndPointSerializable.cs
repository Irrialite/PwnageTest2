using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetworkBase.Utility
{
    public class IPEndPointSerializable
    {
        public string Address
        {
            get;
            set;
        }
        public int Port
        {
            get;
            set;
        }

        public IPEndPointSerializable(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public static implicit operator IPEndPoint(IPEndPointSerializable obj)
        {
            return new IPEndPoint(IPAddress.Parse(obj.Address), obj.Port);
        }

        public static implicit operator IPEndPointSerializable(IPEndPoint obj)
        {
            return new IPEndPointSerializable(obj.Address.ToString(), obj.Port);
        }
    }
}
