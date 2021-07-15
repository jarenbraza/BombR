namespace BombermanAspNet.Models
{
    /// <summary>
    /// Represents details required for tracking a connection.
    /// </summary>
    public class ConnectionContext
    {
        /// <summary>The name of the room associated with the connection.</summary>
        public string RoomName { get; set; }

        /// <summary>The name of the player associated with the connection.</summary>
        public string PlayerName { get; set; }
    }
}
