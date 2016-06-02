using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkBase.Events.Args
{
    [JsonObject(MemberSerialization.OptOut)]
    public struct TestArg
    {
        public int x;
        public int y;
        public TestArgSub sub;
        public string s;

        public struct TestArgSub
        {
            public long z;
            public long w;
        }
    }
}
