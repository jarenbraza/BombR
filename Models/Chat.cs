using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BombermanAspNet.Models
{
    public class Chat
    {
        public Dictionary<string, ConnectionContext> ConnectionContexts { get; set; } = new();
    }
}
