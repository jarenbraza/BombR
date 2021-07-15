using System.Collections.Generic;

namespace BombermanAspNet.Models
{
    /// <summary>Represents a room.</summary>
    /// <remarks>This only contains details for displaying room previews in the lobby.</remarks>
    public class Room
    {
        /// <summary>The name of the room.</summary>
        public string RoomName { get; set; }

        /// <summary>The <see cref="List{T}"/> of players connected to the room.</summary>
        public List<string> PlayerNames { get; set; }
    }
}
