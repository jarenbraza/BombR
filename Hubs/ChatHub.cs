using BombermanAspNet.Data;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BombermanAspNet.Hubs
{
    public class ChatHub : Hub
    {
        private readonly Lobby lobby;

        public ChatHub(Lobby lobby)
        {
            this.lobby = lobby;
        }

        public async Task JoinChatRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task SendMessage(string roomName, string message, string sender)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(nameof(message));
            }

            if (string.IsNullOrEmpty(sender))
            {
                throw new ArgumentException(nameof(sender));
            }

            await Clients.Group(roomName).SendAsync("ReceiveMessage", message, sender).ConfigureAwait(false);
        }

        public List<string> GetPlayersInRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            if (!lobby.TryGetPlayerNames(roomName, out var playerNames))
            {
                throw new HubException("Room was not found");
            }

            return playerNames;
        }

        public async Task SendPlayerConnected(string roomName, string playerName)
        {
            await Clients.OthersInGroup(roomName).SendAsync("PlayerConnected", playerName).ConfigureAwait(false);
        }
    }
}
