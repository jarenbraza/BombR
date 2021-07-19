using BombermanAspNet.Models;
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
        private readonly ChatUtils chat;

        public ChatHub(ChatUtils chat)
        {
            this.chat = chat;
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            var context = await chat.PopConnectionContext(Context.ConnectionId);

            if (context != null)
            {
                await Clients.OthersInGroup(context.RoomName).SendAsync("PlayerDisconnected", context.PlayerName);
            }
            else
            {
                Debug.WriteLine("Could not find context for " + Context.ConnectionId);
            }
        }

        public async Task<List<string>> GetPlayersInRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            var playerNames = await chat.GetPlayerNamesInRoom(roomName);

            if (playerNames == null)
            {
                throw new HubException("Players in " + roomName + " were not found");
            }

            return playerNames;
        }

        // TODO: During separation of chat and lobby connection contexts, persist player name to the chat context.
        public async Task JoinChatRoom(string roomName, string playerName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            await chat.AddConnectionContext(Context.ConnectionId, new ConnectionContext
            {
                RoomName = roomName,
                PlayerName = playerName
            });
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

            await Clients.OthersInGroup(roomName).SendAsync("PlayerConnected", playerName);
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

            await Clients.Group(roomName).SendAsync("ReceiveMessage", message, sender);
        }
    }
}
