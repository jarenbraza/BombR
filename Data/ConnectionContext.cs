using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BombermanAspNet.Data
{
    public class ConnectionContext
    {
        public ConnectionContext(string roomName, string playerName)
        {
            RoomName = roomName;
            PlayerName = playerName;
        }

        public string RoomName { get; set; }
        public string PlayerName { get; set; }
    }
}
