using System.Collections.Generic;

namespace BombermanAspNet.Models
{
    // TODO: Refactor so that only a single collection of connection contexts is needed between lobby/chat.
    public class Lobby
    {
        public Dictionary<string, ConnectionContext> ConnectionContexts { get; set; } = new();
    }
}
