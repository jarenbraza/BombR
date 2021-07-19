using BombermanAspNet.Constants;
using BombermanAspNet.Models;
using BombermanAspNet.Servers;
using BombermanAspNet.Utilities;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BombermanAspNet.Hubs
{
    /// <summary>The SignalR <see cref="Hub"/> for exposing event-based game interactions.</summary>
    public class GameHub : Hub
    {
        private readonly GameServer gameServer;
        private readonly LobbyUtils lobby;

        public GameHub(GameServer gameServer, LobbyUtils lobby)
        {
            this.gameServer = gameServer;
            this.lobby = lobby;
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            var context = await lobby.PopConnectionContext(Context.ConnectionId);

            if (context != null)
            {
                await gameServer.LeaveRoom(context.RoomName, context.PlayerName);
                await RefreshGameState(context.RoomName);
                await lobby.UpdateLobbyForRoom(context.RoomName);
            }
        }

        public async Task JoinGameRoom(string roomName, string playerName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            if (string.IsNullOrEmpty(playerName))
            {
                throw new ArgumentException(nameof(playerName));
            }

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
                await gameServer.JoinRoom(roomName, playerName);
                await lobby.AddConnectionContext(Context.ConnectionId, new ConnectionContext
                {
                    RoomName = roomName,
                    PlayerName = playerName
                });
                await lobby.UpdateLobbyForRoom(roomName);
            }
            catch
            {
                Debug.WriteLine("Unable to join room " + roomName + " for player " + playerName);
                throw;
            }
        }

        public async Task RefreshGameState(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            var state = await gameServer.GetGameState(roomName);

            if (state == null)
            {
                Debug.WriteLine("Unable to update game state for room " + roomName);
            }
            else
            {
                await Clients.Group(roomName).SendAsync("ReceiveGameState", state);
            }
        }

        public async Task SendPlayerMove(string roomName, string playerName, int keyCode)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            if (string.IsNullOrEmpty(playerName))
            {
                throw new ArgumentException(nameof(playerName));
            }

            if (!GameConstants.ValidKeyCodes.Contains(keyCode))
            {
                throw new ArgumentException(nameof(keyCode));
            }

            try
            {
                await gameServer.HandleAction(roomName, playerName, keyCode);
                var state = await gameServer.GetGameState(roomName);
                await Clients.Group(roomName).SendAsync("ReceiveGameState", state);
            }
            catch
            {
                Debug.WriteLine("Unable to update game state for room " + roomName + " and player " + playerName);
                throw;
            }
        }
    }
}
