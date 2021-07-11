using BombermanAspNet.Utilities;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BombermanAspNet.Hubs
{
    public class ChatHub : Hub
    {
        private readonly LobbyUtils lobby;

        public ChatHub(LobbyUtils lobby)
        {
            this.lobby = lobby;
        }

        // TODO: This will (almost) never work.
        // GameHub MUST remove connection context to notify lobby on disconnect.
        // Otherwise, we would never be able to resolve a broken connection to the game room.
        // To resolve, separate chat and lobby connection contexts under a different key.
        public async override Task OnDisconnectedAsync(Exception exception)
        {
            var context = await lobby.GetConnectionContext(Context.ConnectionId);

            if (context != null)
            {
                await Clients.OthersInGroup(context.RoomName).SendAsync("PlayerDisconnected", context.PlayerName).ConfigureAwait(false);
            }
            else
            {
                Debug.WriteLine("Could not find context for " + Context.ConnectionId);
            }
        }

        // TODO: During separation of chat and lobby connection contexts, persist player name to the chat context.
        public async Task JoinChatRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task SendPlayerConnected(string roomName, string playerName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            if (string.IsNullOrEmpty(playerName))
            {
                throw new ArgumentException(nameof(playerName));
            }

            await Clients.OthersInGroup(roomName).SendAsync("PlayerConnected", playerName).ConfigureAwait(false);
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

        public async Task<List<string>> GetPlayersInRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            var playerNames = await lobby.GetPlayerNames();

            if (playerNames == null)
            {
                throw new HubException("Players in " + roomName + " were not found");
            }

            return playerNames;
        }
    }
}
