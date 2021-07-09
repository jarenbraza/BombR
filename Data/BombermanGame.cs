using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using BombermanAspNet.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BombermanAspNet.Data
{
    public class BombermanGame
    {
        private const double IntervalInMilliseconds = 33; // 30 FPS

        // Associated rooms to game state.
        private readonly ConcurrentDictionary<string, GameState> gameStateOfRoom = new();
        private readonly Timer timer;
        private readonly IHubContext<GameHub> gameHub;
        private readonly object gameStateLock = new();

        // Tracks current game state of room during execution
        private GameState currentState;
        
        public BombermanGame(IHubContext<GameHub> gameHub)
        {
            this.gameHub = gameHub;
            timer = new Timer(IntervalInMilliseconds);
            timer.Elapsed += HandleBombsInAllRooms;
            timer.Elapsed += HandleExplosionsInAllRooms;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        /// <summary>
        /// Gets a collection containing the room names in the <see cref="ConcurrentDictionary{string, GameState}"/>.
        /// </summary>
        /// <returns>A collection of room name strings.</returns>
        public ICollection<string> GetRoomNames()
        {
            return gameStateOfRoom.Keys;
        }

        /// <summary>
        /// Attempts to get the game state associated with the specified room name from the <see cref="ConcurrentDictionary{string, GameState}"/>.
        /// </summary>
        /// <param name="roomName">The room name of the game state to get.</param>
        /// <returns>The game state at the specified room name.</returns>
        /// <exception cref="ArgumentException">The room name was not found.</exception>
        public GameState GetGameState(string roomName)
        {
            if (!gameStateOfRoom.TryGetValue(roomName, out var state))
            {
                throw new ArgumentException("Failed to get game state for room " + roomName);
            }

            return state;
        }

        /// <summary>
        /// <para>
        /// Attempts to get the game state associated with the specified room name from the <see cref="ConcurrentDictionary{string, GameState}"/>.
        /// If it does not exist, a new game state is created and associated with the room name.
        /// Then, the player is added to the game state.
        /// </para>
        /// This method is atomic.
        /// </summary>
        /// <param name="roomName">The room name of the game state to get or create.</param>
        /// <param name="playerName">The name of the player to add to the room.</param>
        public void JoinRoom(string roomName, string playerName)
        {
            // TODO: Can make more efficient by using a game state lock specific to the room.
            lock (gameStateLock)
            {
                currentState = gameStateOfRoom.GetOrAdd(roomName, new GameState());
                currentState.Players.Add(playerName, new Player());
                SaveGameState(roomName);
            }
        }

        /// <summary>
        /// <para>Handles the move of a player and updates the game state of the room if it was successful.</para>
        /// <para>The way the move is handled differs based on the key pressed.</para>
        /// </summary>
        /// <param name="roomName">The room name of the game state to get.</param>
        /// <param name="playerName">The name of the player to handle the move for.</param>
        /// <param name="keyCode">The corresponding value of the physical key pressed on the keyboard by the player.</param>
        public void HandleMove(string roomName, string playerName, int keyCode)
        {
            bool IsValidMove(int row, int col)
            {
                if (!IsInBounds(row, col))
                {
                    return false;
                }

                if (IsAnotherPlayerAliveAtPosition(playerName, row, col))
                {
                    return false;
                }

                if (GetBomb(row, col) != null)
                {
                    return false;
                }

                return currentState.Board[row][col] == GameConstants.Empty;
            }

            // TODO: Can make more efficient by using a game state lock specific to the room.
            lock (gameStateLock)
            {
                currentState = GetGameState(roomName);
                Player player = currentState.Players[playerName];

                // Do not allow dead players to move
                if (!player.IsAlive)
				{
                    return;
				}

                bool hasUpdatedState = false;

                if (IsUpMove(keyCode) && IsValidMove(player.Row - 1, player.Col))
                {
                    player.Row -= 1;
                    hasUpdatedState = true;
                }
                else if (IsLeftMove(keyCode) && IsValidMove(player.Row, player.Col - 1))
                {
                    player.Col -= 1;
                    hasUpdatedState = true;
                }
                else if (IsDownMove(keyCode) && IsValidMove(player.Row + 1, player.Col))
                {
                    player.Row += 1;
                    hasUpdatedState = true;
                }
                else if (IsRightMove(keyCode) && IsValidMove(player.Row, player.Col + 1))
                {
                    player.Col += 1;
                    hasUpdatedState = true;
                }
                else if (IsPlaceBomb(keyCode) && IsValidMove(player.Row, player.Col))
                {
                    currentState.Bombs.Add(new Bomb(player));
                    hasUpdatedState = true;
                }

                if (hasUpdatedState)
                {
                    if (IsExplosion(player.Row, player.Col))
					{
                        player.IsAlive = false;
                        Debug.WriteLine("Player " + playerName + " has died x_x");
                    }

                    SaveGameState(roomName);
                }
            }
        }

        private async void HandleExplosionsInAllRooms(object sender, ElapsedEventArgs e)
        {
            foreach (var roomName in GetRoomNames())
            {
                bool hasExplosionCleared = false;

                // TODO: Can make more efficient by using a game state lock specific to the room.
                lock (gameStateLock)
                {
                    currentState = GetGameState(roomName);
                    hasExplosionCleared = HandleExplosions();

                    if (hasExplosionCleared)
                    {
                        SaveGameState(roomName);
                    }
                }

                if (hasExplosionCleared)
                {
                    await gameHub.Clients.Group(roomName).SendAsync("ReceiveGameState", currentState);
                }
            }
        }

        private bool HandleExplosions()
        {
            bool hasExplosionCleared = false;
            Explosion nextExplosionToExpire = currentState.Explosions.Min;

            while (nextExplosionToExpire != null && nextExplosionToExpire.IsExpired())
            {
                hasExplosionCleared = true;
                currentState.Explosions.Remove(nextExplosionToExpire);
                nextExplosionToExpire = currentState.Explosions.Min;
            }

            return hasExplosionCleared;
        }

        private async void HandleBombsInAllRooms(object sender, ElapsedEventArgs e)
        {
            foreach (var roomName in GetRoomNames())
            {
                bool hasExplosionOccurred = false;

                // TODO: Can make more efficient by using a game state lock specific to the room.
                lock (gameStateLock)
                {
                    currentState = GetGameState(roomName);
                    hasExplosionOccurred = HandleBombs();

                    if (hasExplosionOccurred)
                    {
                        SaveGameState(roomName);
                    }
                }

                if (hasExplosionOccurred)
				{
                    await gameHub.Clients.Group(roomName).SendAsync("ReceiveGameState", currentState);
                }
            }
        }

        private bool HandleBombs()
        {
            bool hasExplodedBomb = false;
            Bomb nextBombToExpire = currentState.Bombs.Min;

            while (nextBombToExpire != null && nextBombToExpire.IsExpired())
            {
                hasExplodedBomb = true;
                ExplodeBomb(nextBombToExpire);
                nextBombToExpire = currentState.Bombs.Min;
            }

            if (hasExplodedBomb)
            {
                RemoveBrokenWalls();
            }

            return hasExplodedBomb;
        }

        private void ExplodeBomb(in Bomb bomb)
        {
            currentState.Bombs.Remove(bomb);
            currentState.Explosions.Add(new Explosion(bomb.Row, bomb.Col));

            for (int i = 0; i < GameConstants.Directions.GetLength(0); i++)
            {
                for (int d = 1; d <= bomb.ExplosionDistance; d++)
                {
                    int explosionRow = bomb.Row + GameConstants.Directions[i, 0] * d;
                    int explosionCol = bomb.Col + GameConstants.Directions[i, 1] * d;

                    if (!AbleToExpandExplosion(explosionRow, explosionCol))
                    {
                        break;
                    }

                    currentState.Explosions.Add(new Explosion(explosionRow, explosionCol));
                }
            }
        }

        private bool AbleToExpandExplosion(int row, int col)
        {
            if (!IsInBounds(row, col) || IsUnbreakable(row, col))
            {
                return false;
            }

            if (IsBreakable(row, col))
            {
                currentState.Explosions.Add(new Explosion(row, col));
                currentState.Board[row][col] = GameConstants.BrokenWall;
                return false;
            }

            Debug.WriteLine("Explosion on (" + col + ", " + row + ")");

            var bomb = GetBomb(row, col);

            if (bomb != null)
            {
                ExplodeBomb(bomb);
            }

            var playerName = GetPlayerName(row, col);

            if (playerName != null)
            {
                currentState.Players[playerName].IsAlive = false;
                Debug.WriteLine("Player " + playerName + " has died x_x");
            }

            return true;
        }

        private void RemoveBrokenWalls()
        {
            for (int r = 0; r < currentState.Board.Count; r++)
            {
                for (int c = 0; c < currentState.Board[r].Count; c++)
                {
                    if (currentState.Board[r][c] == GameConstants.BrokenWall)
                    {
                        currentState.Board[r][c] = GameConstants.Empty;
                    }
                }
            }
        }

        private string GetPlayerName(int row, int col)
        {
            foreach (var playerName in currentState.Players.Keys)
            {
                var player = currentState.Players[playerName];

                if (player.Row == row && player.Col == col)
                {
                    return playerName;
                }
            }

            return null;
        }

        private bool IsAnotherPlayerAliveAtPosition(string playerName, int row, int col)
        {
            string playerNameAtPosition = GetPlayerName(row, col);

            // Do not consider the player themselves
            if (playerNameAtPosition == null || playerNameAtPosition.Equals(playerName))
            {
                return false;
            }

            return currentState.Players[playerNameAtPosition].IsAlive;
        }

        private bool IsExplosion(int row, int col)
		{
            foreach (var explosion in currentState.Explosions)
			{
                if (explosion.Row == row && explosion.Col == col)
				{
                    return true;
				}
			}

            return false;
		}

        private Bomb GetBomb(int row, int col)
        {
            foreach (var bomb in currentState.Bombs)
            {
                if (bomb.Row == row && bomb.Col == col)
                {
                    return bomb;
                }
            }

            return null;
        }

        private bool IsInBounds(int row, int col)
        {
            return (row >= 0)
                && (col >= 0)
                && (row < currentState.Board.Count)
                && (col < currentState.Board[row].Count);
        }

        private bool IsBreakable(int row, int col)
        {
            int boardValue = currentState.Board[row][col];
            return boardValue == GameConstants.BreakableWall || boardValue == GameConstants.BrokenWall;
        }

        private bool IsUnbreakable(int row, int col)
        {
            return currentState.Board[row][col] == GameConstants.UnbreakableWall;
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

        private void SaveGameState(string roomName)
		{
            GameState previousState = GetGameState(roomName);

            if (!gameStateOfRoom.TryUpdate(roomName, currentState, previousState))
            {
                throw new ArgumentException("Failed to update game state for room " + roomName);
            }
        }
    }
}
