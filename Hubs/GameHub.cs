using BombermanAspNet.Data;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace BombermanAspNet.Hubs
{
    public class GameHub : Hub
    {
        private readonly BombermanGame game;

        public GameHub(BombermanGame game)
        {
            this.game = game;
        }

        public async Task<GameState> JoinGameRoom(string roomName, string playerName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            if (string.IsNullOrEmpty(playerName))
            {
                throw new ArgumentException(nameof(playerName));
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            GameState state = game.GetOrCreateGameState(roomName);
            game.AddPlayerToGame(playerName, ref state);
            return state;
        }

        public async Task GetGameState(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            if (game.TryGetGameState(roomName, out var state))
            {
                await Clients.Caller.SendAsync("ReceiveGameState", state);
            }
            else
            {
                Debug.WriteLine("Unable to find game state for room " + roomName + ". Attempting to generate room.");
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

            if (game.HandleMove(roomName, playerName, keyCode))
            {
                await Clients.Group(roomName).SendAsync("ReceiveGameState", state);
            }
        }

        public async Task RefreshGameStateForOthers(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            if (game.TryGetGameState(roomName, out var state))
            {
                await Clients.OthersInGroup(roomName).SendAsync("ReceiveGameState", state);
            }
            else
            {
                throw new HubException("Failed to refresh game state to others for room " + roomName);
            }
        }

    }
}
