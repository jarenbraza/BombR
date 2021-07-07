using System.Collections.Generic;

namespace BombermanAspNet.Models
{
    public class RoomModel
    {
        public RoomModel(string roomName, List<string> playerNames)
        {
            RoomName = roomName;
            PlayerNames = new List<string>(playerNames);
        }

        public string RoomName { get; set; }
        public List<string> PlayerNames { get; set; }
    }
}
