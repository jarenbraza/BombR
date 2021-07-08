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

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            try
			{
                game.JoinRoom(roomName, playerName);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unable to join room " + roomName);
                Debug.WriteLine(e);
			}
        }

        public async Task RefreshGameState(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
            {
                throw new ArgumentException(nameof(roomName));
            }

            try
            {
                await Clients.Group(roomName).SendAsync("ReceiveGameState", game.GetGameState(roomName));
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unable to update game state for room " + roomName);
                Debug.WriteLine(e);
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
                game.HandleMove(roomName, playerName, keyCode);
                await Clients.Group(roomName).SendAsync("ReceiveGameState", game.GetGameState(roomName));
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unable to update game state for room " + roomName + " and player " + playerName);
                Debug.WriteLine(e);
            }
        }
    }
}
