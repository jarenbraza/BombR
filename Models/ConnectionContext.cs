namespace BombermanAspNet.Models
{
	public class ConnectionContext
	{
		public string RoomName { get; set; }
		public string PlayerName { get; set; }

		public ConnectionContext(string roomName, string playerName)
		{
			RoomName = roomName;
			PlayerName = playerName;
		}
	}
}
