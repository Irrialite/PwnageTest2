using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkBase.Events.Args
{
    public struct GameInstanceCreated
    {
        public int game;
        public int id;
    }

    public struct GameServerList
    {
        public int[][] servers;
    }
}
