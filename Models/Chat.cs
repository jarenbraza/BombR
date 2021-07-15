using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BombermanAspNet.Models
{
    // TODO: Refactor so that only a single collection of connection contexts is needed between lobby/chat.
    public class Chat
    {
        public Dictionary<string, ConnectionContext> ConnectionContexts { get; set; } = new();
    }
}
