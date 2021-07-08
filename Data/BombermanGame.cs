using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using BombermanAspNet.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BombermanAspNet.Data
{
    // TODO: This file is not well-designed. Too many reference changes to GameState and its members.
    public class BombermanGame
    {
        private const double IntervalInMilliseconds = 33; // 30 FPS
        private const long BombDurationInMilliseconds = 3000;

        // Associated rooms to game state.
        private readonly ConcurrentDictionary<string, GameState> gameStateOfRoom = new();
        private readonly Timer timer;
        private readonly IHubContext<GameHub> gameHub;

        // Tracks current game state of room during execution
        private GameState currentState;
        
        public BombermanGame(IHubContext<GameHub> gameHub)
        {
            this.gameHub = gameHub;
            timer = new Timer(IntervalInMilliseconds);
            timer.Elapsed += HandleBombsInAllRooms;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        public ICollection<string> GetRoomNames()
        {
            return gameStateOfRoom.Keys;
        }

        public GameState GetGameState(string roomName)
        {
            if (!gameStateOfRoom.TryGetValue(roomName, out var state))
            {
                throw new ArgumentException("Failed to get game state for room " + roomName);
            }

            return state;
        }

        public void JoinRoom(string roomName, string playerName)
        {
            // TODO: Potentially lock here, as game state is being obtained and updated
            currentState = gameStateOfRoom.GetOrAdd(roomName, new GameState());
            currentState.Players.Add(playerName, new Player());
            SaveGameState(roomName);
        }

        public void HandleMove(string roomName, string playerName, int keyCode)
        {
            // TODO: Potentially lock here, as game state is being obtained and updated
            currentState = GetGameState(roomName);
            Player player = currentState.Players[playerName];
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
                currentState.Bombs.Add(new Bomb(BombDurationInMilliseconds, player));
                hasUpdatedState = true;
            }

            if (hasUpdatedState)
			{
                // TODO: Check if the player has moved into the explosion.
                SaveGameState(roomName);
			}

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
        }

        private async void HandleBombsInAllRooms(object sender, ElapsedEventArgs e)
        {
            foreach (var roomName in GetRoomNames())
            {
                // TODO: Potentially lock here, as game state is being obtained and updated
                currentState = GetGameState(roomName);

                if (HandleBombs())
                {
                    await gameHub.Clients.Group(roomName).SendAsync("ReceiveGameState", currentState);
                    SaveGameState(roomName);
                }
            }
        }

        private bool HandleBombs()
        {
            if (currentState.Bombs.Count <= 0)
            {
                return false;
            }

            bool hasExplodedBomb = false;
            Bomb nextBombToExpire = currentState.Bombs.Min;

            while (nextBombToExpire != null && nextBombToExpire.IsExpired())
            {
                ExplodeBomb(nextBombToExpire);
                hasExplodedBomb = true;
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
            Debug.WriteLine("Blowing up bomb at (" + bomb.Col + ", " + bomb.Row + ")");
            currentState.Bombs.Remove(bomb);

            // TODO: Add explosion here

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

                    // TODO: Add explosion here
                }
            }
        }

        private bool AbleToExpandExplosion(int row, int col)
        {
            if (!IsInBounds(row, col))
            {
                return false;
            }

            if (IsBreakable(row, col))
            {
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
