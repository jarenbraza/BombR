using BombermanAspNet.Constants;
using BombermanAspNet.Extensions;
using BombermanAspNet.Hubs;
using BombermanAspNet.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace BombermanAspNet.Utilities
{
    public class GameUtils
    {
        private const double IntervalInMilliseconds = 33;  // 30 FPS

        // Associated rooms to game state.
        private readonly object gameStateLock = new();
        private readonly IDistributedCache cache;
        private readonly Timer timer;
        private readonly IHubContext<GameHub> gameHub;

        // Used to get information about all rooms
        private readonly LobbyUtils lobby;
        
        public GameUtils(IDistributedCache cache, IHubContext<GameHub> gameHub, LobbyUtils lobby)
        {
            this.cache = cache;
            this.gameHub = gameHub;
            this.lobby = lobby;
            timer = new Timer(IntervalInMilliseconds);
            timer.Elapsed += HandleBombsInAllRooms;
            timer.Elapsed += HandleExplosionsInAllRooms;
            timer.Elapsed += HandleWinnersInAllRooms;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        /// <summary>
        /// Attempts to get the game state associated with the specified room name./>.
        /// </summary>
        /// <param name="roomName">The room name of the game state to get.</param>
        /// <returns>The game state at the specified room name.</returns>
        public async Task<GameState> GetGameState(string roomName)
        {
            return await cache.GetRecordAsync<GameState>(GetGameKey(roomName));
        }

        private async Task SaveGameState(string roomName, GameState state)
        {
            await cache.SetRecordAsync(GetGameKey(roomName), state);
        }

        /// <summary>
        /// <para>
        /// Attempts to get the game state associated with the specified room name from the <see cref="ConcurrentDictionary{string, GameState}"/>.
        /// If it does not exist, a new game state is created and associated with the room name.
        /// Then, the player is added to the game state.
        /// The game is started, if it hasn't been already.
        /// </para>
        /// This method is atomic.
        /// </summary>
        /// <param name="roomName">The room name of the game state to get or create.</param>
        /// <param name="playerName">The name of the player to add to the room.</param>
        public async Task JoinRoom(string roomName, string playerName)
        {
            var state = await GetGameState(roomName);

            lock (gameStateLock)
            {
                if (state == null)
                {
                    state = new GameState();
                }

                state.Players.Add(CreatePlayer(playerName));
            }

            await SaveGameState(roomName, state);
        }

        public async Task LeaveRoom(string roomName, string playerName)
        {
            var state = await GetGameState(roomName);

            lock (gameStateLock)
            {
                if (state != null)
                {
                    state.Players.RemoveAll(player => player.Name.Equals(playerName));
                }
            }

            await SaveGameState(roomName, state);
        }

        /// <summary>
        /// <para>Handles the move of a player and updates the game state of the room if it was successful.</para>
        /// <para>The way the move is handled differs based on the key pressed.</para>
        /// </summary>
        /// <param name="roomName">The room name of the game state to get.</param>
        /// <param name="playerName">The name of the player to handle the move for.</param>
        /// <param name="keyCode">The corresponding value of the physical key pressed on the keyboard by the player.</param>
        public async Task HandleMove(string roomName, string playerName, int keyCode)
        {
            var state = await GetGameState(roomName);
            bool placedBombSuccessfully = false;

            lock (gameStateLock)
            {
                Player player = state.Players.Find(player => player.Name.Equals(playerName));

                // Do not allow dead players to move
                if (!player.IsAlive)
                {
                    return;
                }

                bool hasUpdatedState = false;

                if (IsUpMove(keyCode) && IsValidMove(player.Row - 1, player.Col, playerName, ref state))
                {
                    player.Row -= 1;
                    hasUpdatedState = true;
                }
                else if (IsLeftMove(keyCode) && IsValidMove(player.Row, player.Col - 1, playerName, ref state))
                {
                    player.Col -= 1;
                    hasUpdatedState = true;
                }
                else if (IsDownMove(keyCode) && IsValidMove(player.Row + 1, player.Col, playerName, ref state))
                {
                    player.Row += 1;
                    hasUpdatedState = true;
                }
                else if (IsRightMove(keyCode) && IsValidMove(player.Row, player.Col + 1, playerName, ref state))
                {
                    player.Col += 1;
                    hasUpdatedState = true;
                }
                else if (IsPlaceBomb(keyCode) && IsValidMove(player.Row, player.Col, playerName, ref state))
                {
                    state.Bombs.Add(new Bomb {
                        Expiration = DateTime.Now.AddMilliseconds(GameConstants.BombDurationInMilliseconds),
                        Row = player.Row,
                        Col = player.Col,
                        ExplosionDistance = player.ExplosionDistance
                    });
                    hasUpdatedState = true;
                    placedBombSuccessfully = true;
                }

                if (hasUpdatedState && IsExplosion(player.Row, player.Col, state))
                {
                    player.IsAlive = false;
                    Debug.WriteLine("Player " + playerName + " has died x_x");
                }
            }

            await SaveGameState(roomName, state);

            if (placedBombSuccessfully)
            {
                await gameHub.Clients.Group(roomName).SendAsync("PlaySoundForPlacingBomb");
            }
        }

        private async void HandleExplosionsInAllRooms(object sender, ElapsedEventArgs e)
        {
            foreach (var roomName in await lobby.GetRoomNames())
            {
                bool hasExplosionCleared = false;

                // TODO: Can make more efficient by using a game state lock specific to the room.
                var state = await GetGameState(roomName);

                lock (gameStateLock)
                {
                    hasExplosionCleared = HandleExplosions(ref state);
                }

                if (hasExplosionCleared)
                {
                    await SaveGameState(roomName, state);
                    await gameHub.Clients.Group(roomName).SendAsync("ReceiveGameState", state);
                }
            }
        }

        private bool HandleExplosions(ref GameState state)
        {
            bool hasExplosionCleared = false;
            Explosion nextExplosionToExpire = state.Explosions.Min;

            while (nextExplosionToExpire != null && nextExplosionToExpire.IsExpired())
            {
                hasExplosionCleared = true;
                state.Explosions.Remove(nextExplosionToExpire);
                nextExplosionToExpire = state.Explosions.Min;
            }

            return hasExplosionCleared;
        }

        private async void HandleBombsInAllRooms(object sender, ElapsedEventArgs e)
        {
            foreach (var roomName in await lobby.GetRoomNames())
            {
                bool hasExplosionOccurred = false;
                var state = await GetGameState(roomName);

                // TODO: Can make more efficient by using a game state lock specific to the room.
                lock (gameStateLock)
                {
                    hasExplosionOccurred = HandleBombs(ref state);
                }

                if (hasExplosionOccurred)
                {
                    await SaveGameState(roomName, state);
                    await gameHub.Clients.Group(roomName).SendAsync("ReceiveGameState", state);
                }
            }
        }

        private bool HandleBombs(ref GameState state)
        {
            bool hasExplodedBomb = false;
            Bomb nextBombToExpire = state.Bombs.Min;

            while (nextBombToExpire != null && nextBombToExpire.IsExpired())
            {
                hasExplodedBomb = true;
                ExplodeBomb(ref state, nextBombToExpire);
                nextBombToExpire = state.Bombs.Min;
            }

            if (hasExplodedBomb)
            {
                RemoveBrokenWalls(ref state);
            }

            return hasExplodedBomb;
        }

        private void ExplodeBomb(ref GameState state, in Bomb bomb)
        {
            state.Bombs.Remove(bomb);

            state.Explosions.Add(new Explosion {
                Expiration = DateTime.Now.AddMilliseconds(GameConstants.ExplosionDurationInMilliseconds),
                Row = bomb.Row,
                Col = bomb.Col
            });

            for (int i = 0; i < GameConstants.Directions.GetLength(0); i++)
            {
                for (int d = 1; d <= bomb.ExplosionDistance; d++)
                {
                    int explosionRow = bomb.Row + GameConstants.Directions[i, 0] * d;
                    int explosionCol = bomb.Col + GameConstants.Directions[i, 1] * d;

                    if (!AbleToExpandExplosion(ref state, explosionRow, explosionCol))
                    {
                        break;
                    }

                    state.Explosions.Add(new Explosion
                    {
                        Expiration = DateTime.Now.AddMilliseconds(GameConstants.ExplosionDurationInMilliseconds),
                        Row = explosionRow,
                        Col = explosionCol
                    });
                }
            }
        }

        private bool AbleToExpandExplosion(ref GameState state, int row, int col)
        {
            if (!IsInBounds(row, col, state) || IsUnbreakable(row, col, state))
            {
                return false;
            }

            if (IsBreakable(row, col, state))
            {
                state.Explosions.Add(new Explosion
                {
                    Expiration = DateTime.Now.AddMilliseconds(GameConstants.ExplosionDurationInMilliseconds),
                    Row = row,
                    Col = col
                });
                state.Board[row][col] = GameConstants.BrokenWall;
                return false;
            }

            var bomb = GetBomb(row, col, state);

            if (bomb != null)
            {
                ExplodeBomb(ref state, bomb);
            }

            TryKillPlayerAtLocation(ref state, row, col);

            return true;
        }

        private void RemoveBrokenWalls(ref GameState state)
        {
            for (int r = 0; r < state.Board.Count; r++)
            {
                for (int c = 0; c < state.Board[r].Count; c++)
                {
                    if (state.Board[r][c] == GameConstants.BrokenWall)
                    {
                        state.Board[r][c] = GameConstants.Empty;
                    }
                }
            }
        }

        // TODO: Probably some better way to handle logic here
        private async void HandleWinnersInAllRooms(object sender, ElapsedEventArgs e)
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

                int livingPlayers = 0;
                string nameOfLastLivingPlayer = "";

                lock (gameStateLock)
                {
                    livingPlayers = state.Players.Where(player => player.IsAlive).Count();

                    if (livingPlayers == 1 && state.Players.Count > 1)
                    {
                        nameOfLastLivingPlayer = state.Players.Find(player => player.IsAlive).Name;
                        state.HasWinner = true;
                    }
                    else if (livingPlayers == 0)
                    {
                        nameOfLastLivingPlayer = "Tie!";
                        state.HasWinner = true;
                    }
                }

                if (state.HasWinner && livingPlayers == 1 && state.Players.Count > 1)
                {
                    await SaveGameState(roomName, state);
                    await gameHub.Clients.Group(roomName).SendAsync("ReceiveWinner", nameOfLastLivingPlayer);
                }
                else if (state.HasWinner && livingPlayers == 0)
                {
                    await SaveGameState(roomName, state);

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

        private bool IsAnotherPlayerAliveAtPosition(string playerName, int row, int col, ref GameState state)
        {
            // Do not consider the players themselves
            var player = state.Players.Find(p => (p.Row == row) && (p.Col == col) && !p.Name.Equals(playerName));

            return player != null && player.IsAlive;
        }

        private bool IsExplosion(int row, int col, in GameState gameState)
        {
            foreach (var explosion in gameState.Explosions)
            {
                if (explosion.Row == row && explosion.Col == col)
                {
                    return true;
                }
            }

            return false;
        }

        private Bomb GetBomb(int row, int col, in GameState state)
        {
            foreach (var bomb in state.Bombs)
            {
                if (bomb.Row == row && bomb.Col == col)
                {
                    return bomb;
                }
            }

            return null;
        }

        private static bool IsInBounds(int row, int col, in GameState state)
        {
            return (row >= 0)
                && (col >= 0)
                && (row < state.Board.Count)
                && (col < state.Board[row].Count);
        }

        private bool IsBreakable(int row, int col, in GameState state)
        {
            int boardValue = state.Board[row][col];
            return boardValue == GameConstants.BreakableWall || boardValue == GameConstants.BrokenWall;
        }

        private bool IsUnbreakable(int row, int col, in GameState state)
        {
            return state.Board[row][col] == GameConstants.UnbreakableWall;
        }

        private static bool IsUpMove(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeW || keyCode == GameConstants.KeyCodeUp;
        }

        private static bool IsLeftMove(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeA || keyCode == GameConstants.KeyCodeLeft;
        }

        private static bool IsDownMove(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeS || keyCode == GameConstants.KeyCodeDown;
        }

        private static bool IsRightMove(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeD || keyCode == GameConstants.KeyCodeRight;
        }

        private static bool IsPlaceBomb(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeSpace;
        }

        private static string GetGameKey(string roomName)
        {
            return nameof(GameState) + "_" + roomName;
        }

        private bool IsValidMove(int row, int col, string playerName, ref GameState state)
        {
            if (!IsInBounds(row, col, state))
            {
                return false;
            }

            if (IsAnotherPlayerAliveAtPosition(playerName, row, col, ref state))
            {
                return false;
            }

            if (GetBomb(row, col, state) != null)
            {
                return false;
            }

            return state.Board[row][col] == GameConstants.Empty;
        }

        private static Player CreatePlayer(string name)
        {
            return new Player
            {
                Name = name,
                RemainingBombs = 1,
                ExplosionDistance = 2,
                IsAlive = true,
                Row = Player.GetNextRow(),
                Col = Player.GetNextCol()
            };
        }

        // TODO: Check if state actually changed
        private static bool TryKillPlayerAtLocation(ref GameState state, int row, int col)
        {
            var player = state.Players.Find(p => (p.Row == row) && (p.Col == col));

            if (player == null)
            {
                return false;
            }

            Debug.WriteLine("Player " + player.Name + " has died x_x");
            player.IsAlive = false;
            return true;
        }
    }
}
