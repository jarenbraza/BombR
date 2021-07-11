using System.Collections.Generic;

namespace BombermanAspNet.Models
{
	public class Lobby
	{
		public Dictionary<string, ConnectionContext> ConnectionContexts { get; set; } = new();
	}
}
