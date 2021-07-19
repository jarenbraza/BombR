using BombermanAspNet.Constants;
using BombermanAspNet.Extensions;
using BombermanAspNet.Hubs;
using BombermanAspNet.Models;
using BombermanAspNet.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace BombermanAspNet.Servers
{
    public class GameServer
    {
        private const double IntervalInMilliseconds = 33;  // 30 FPS

        // Associated rooms to game state.
        private readonly IDistributedCache cache;
        private readonly System.Timers.Timer timer;
        private readonly IHubContext<GameHub> gameHub;
        private readonly SemaphoreSlim gameStateLock = new(1, 1);

        // Used to get information about all rooms
        private readonly LobbyUtils lobby;
        
        public GameServer(IDistributedCache cache, IHubContext<GameHub> gameHub, LobbyUtils lobby)
        {
            this.cache = cache;
            this.gameHub = gameHub;
            this.lobby = lobby;
            timer = new(IntervalInMilliseconds);
            timer.Elapsed += HandleBombsInAllRooms;
            timer.Elapsed += HandleExplosionsInAllRooms;
            timer.Elapsed += HandleWinnersInAllRooms;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        /// <summary>
        /// Gets the game state associated with the specified room name./>.
        /// </summary>
        /// <param name="roomName">The room name of the game state to get.</param>
        /// <returns>The game state at the specified room name.</returns>
        public async Task<GameState> GetGameState(string roomName)
        {
            return await cache.GetRecordAsync<GameState>(GameUtils.GetGameKey(roomName));
        }

        private async Task SaveGameState(string roomName, GameState state)
        {
            await cache.SetRecordAsync(GameUtils.GetGameKey(roomName), state);
        }

        /// <summary>
        /// Attempts to get the <see cref="GameState"/> for the room, creating it if it does not exist.
        /// Then, the player is added to it.
        /// Finally, the timer is started for this room.
        /// </summary>
        /// <param name="roomName">The name of the room for the player to join.</param>
        /// <param name="playerName">The name of the player to add to the room.</param>
        public async Task JoinRoom(string roomName, string playerName)
        {
            await gameStateLock.WaitAsync();

            try
            {
                var state = await GetGameState(roomName) ?? new GameState();

                state.Players.Add(GameUtils.CreatePlayer(playerName));

                await SaveGameState(roomName, state);

                // TODO: Start timer for this room
            }
            finally
            {
                gameStateLock.Release();
            }
        }

        /// <summary>
        /// Attempts to get the <see cref="GameState"/> for the room.
        /// If it exists, the player is removed from it.
        /// Finally, the timer is started for this room.
        /// </summary>
        /// <param name="roomName">The name of the room for the player to join.</param>
        /// <param name="playerName">The name of the player to add to the room.</param>
        public async Task LeaveRoom(string roomName, string playerName)
        {
            await gameStateLock.WaitAsync();

            try
            {
                var state = await GetGameState(roomName);

                if (state != null)
                {
                    state.Players.RemoveAll(player => player.Name.Equals(playerName));
                }

                // TODO: When no more players left, delete associated lock and timer

                await SaveGameState(roomName, state);
            }
            finally
            {
                gameStateLock.Release();
            }
        }

        /// <summary>
        /// <para>Handles the action of a player and updates the game state of the room if it was successful.</para>
        /// <para>The action is handled differently based on the key pressed.</para>
        /// </summary>
        /// <param name="roomName">The room name of the game state to get.</param>
        /// <param name="playerName">The name of the player to handle the move for.</param>
        /// <param name="keyCode">The corresponding value of the physical key pressed on the keyboard by the player.</param>
        public async Task HandleAction(string roomName, string playerName, int keyCode)
        {
            await gameStateLock.WaitAsync();

            try
            {
                var state = await GetGameState(roomName);
                bool placedBombSuccessfully = false;
                Player player = state.Players.Find(player => player.Name.Equals(playerName));

                // Do not allow dead players to perform any actions
                if (!player.IsAlive)
                {
                    return;
                }

                bool hasUpdatedState = false;

                if (GameUtils.IsPlaceBomb(keyCode))
                {
                    if (GameUtils.GetBomb(player.Row, player.Col, state) == null)
                    {
                        state.Bombs.Add(new Bomb
                        {
                            Expiration = DateTime.Now.AddMilliseconds(GameConstants.BombDurationInMilliseconds),
                            Row = player.Row,
                            Col = player.Col,
                            ExplosionDistance = player.ExplosionDistance
                        });
                        placedBombSuccessfully = true;
                        hasUpdatedState = true;
                    }
                }
                else
                {
                    int rowAfterMove = player.Row;
                    int colAfterMove = player.Col;

                    if (GameUtils.IsUpMove(keyCode))
                    {
                        rowAfterMove--;
                    }
                    else if (GameUtils.IsDownMove(keyCode))
                    {
                        rowAfterMove++;
                    }
                    else if (GameUtils.IsLeftMove(keyCode))
                    {
                        colAfterMove--;
                    }
                    else if (GameUtils.IsRightMove(keyCode))
                    {
                        colAfterMove++;
                    }

                    if (GameUtils.IsValidMove(rowAfterMove, colAfterMove, player, state))
                    {
                        player.Row = rowAfterMove;
                        player.Col = colAfterMove;
                        hasUpdatedState = true;
                    }
                }

                if (hasUpdatedState && GameUtils.IsExplosion(player.Row, player.Col, state))
                {
                    player.IsAlive = false;
                    Debug.WriteLine("Player " + playerName + " has died x_x");
                }

                if (placedBombSuccessfully)
                {
                    await gameHub.Clients.Group(roomName).SendAsync("PlaySoundForPlacingBomb");
                }

                await SaveGameState(roomName, state);
            }
            finally
            {
                gameStateLock.Release();
            }
        }

        private async void HandleExplosionsInAllRooms(object sender, ElapsedEventArgs e)
        {
            await gameStateLock.WaitAsync();

            try
            {
                // Improve this by having a gameState lock for each room
                foreach (var roomName in await lobby.GetRoomNames())
                {
                    var state = await GetGameState(roomName);

                    if (GameUtils.HandleExplosions(state))
                    {
                        await SaveGameState(roomName, state);
                        await gameHub.Clients.Group(roomName).SendAsync("ReceiveGameState", state);
                    }
                }
            }
            finally
            {
                gameStateLock.Release();
            }
        }

        private async void HandleBombsInAllRooms(object sender, ElapsedEventArgs e)
        {
            await gameStateLock.WaitAsync();

            try
            {
                // TODO: Can make more efficient by using a game state lock specific to the room.
                foreach (var roomName in await lobby.GetRoomNames())
                {
                    var state = await GetGameState(roomName);

                    if (GameUtils.HandleBombs(state))
                    {
                        await SaveGameState(roomName, state);
                        await gameHub.Clients.Group(roomName).SendAsync("ReceiveGameState", state);
                    }
                }
            }
            finally
            {
                gameStateLock.Release();
            }

        }

        // TODO: Probably some better way to handle logic here
        private async void HandleWinnersInAllRooms(object sender, ElapsedEventArgs e)
        {
            await gameStateLock.WaitAsync();

            try
            {
                foreach (var roomName in await lobby.GetRoomNames())
                {
                    var state = await GetGameState(roomName);

                    // Skip on finished games.
                    // Allow all bombs and explosions to expire to avoid instant-wins when draws should occur.
                    // For example, last player alive is trapped by a bomb.
                    if (state.HasWinner || state.Bombs.Count > 0 || state.Explosions.Count > 0)
                    {
                        continue;
                    }

                    var livingPlayers = state.Players.Where(player => player.IsAlive).ToList();

                    if (livingPlayers.Count == 1 && state.Players.Count > 1)
                    {
                        await gameHub.Clients.Group(roomName).SendAsync("ReceiveWinner", livingPlayers[0].Name);
                        state.HasWinner = true;
                    }
                    else if (livingPlayers.Count == 0)
                    {
                        state.HasWinner = true;

                        if (state.Players.Count > 1)
                        {
                            await gameHub.Clients.Group(roomName).SendAsync("ReceiveTie");
                        }
                        else
                        {
                            await gameHub.Clients.Group(roomName).SendAsync("ReceiveEmbarrassment");
                        }
                    }
                }
            }
            finally
            {
                gameStateLock.Release();
            }
        }
    }
}
