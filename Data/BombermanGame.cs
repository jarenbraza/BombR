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
            timer.Elapsed += UpdateBombStatus;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void UpdateBombStatus(object sender, ElapsedEventArgs e)
        {
            foreach (var roomName in GetRoomNames())
            {
                if (!gameStateOfRoom.TryGetValue(roomName, out var state))
                {
                    throw new 
                }

                currentState = GetGameState(roomName);

                if (HandleBombs(ref state))
                {
                    Debug.WriteLine("Bombs have blown up successfully.");
                    gameHub.Clients.Group(roomName).SendAsync("ReceiveGameState", state);
                }
            }
        }

        public ICollection<string> GetRoomNames()
        {
            return gameStateOfRoom.Keys;
        }

        public GameState GetOrCreateGameState(string roomName)
        {
            return gameStateOfRoom.GetOrAdd(roomName, new GameState());
        }

        public void AddPlayerToGame(string roomName, string playerName, ref GameState state)
        {
            state.Players.Add(playerName, new Player());
        }

        public bool HandleBombs(ref GameState state)
        {
            if (state.Bombs.Count <= 0)
            {
                return false;
            }

            bool hasExplodedBomb = false;
            Bomb nextBombToExpire = state.Bombs.Min;

            while (nextBombToExpire != null && nextBombToExpire.IsExpired())
            {
                ExplodeBomb(ref state, nextBombToExpire);
                hasExplodedBomb = true;
                nextBombToExpire = state.Bombs.Min;
            }

            if (hasExplodedBomb)
            {
                RemoveBrokenWalls(ref state);
            }

            return hasExplodedBomb;
        }

        private static void ExplodeBomb(ref GameState state, in Bomb bomb)
        {
            state.Bombs.Remove(bomb);

            for (int d = 1; d <= bomb.ExplosionDistance; d++)
            {
                if (!HandleExplosion(ref state, bomb.Row, bomb.Col - d))
                {
                    break;
                }
            }

            for (int d = 1; d <= bomb.ExplosionDistance; d++)
            {
                if (!HandleExplosion(ref state, bomb.Row, bomb.Col + d))
                {
                    break;
                }
            }

            for (int d = 1; d <= bomb.ExplosionDistance; d++)
            {
                if (!HandleExplosion(ref state, bomb.Row - d, bomb.Col))
                {
                    break;
                }
            }

            for (int d = 1; d <= bomb.ExplosionDistance; d++)
            {
                if (!HandleExplosion(ref state, bomb.Row + d, bomb.Col))
                {
                    break;
                }
            }
        }

        private static bool HandleExplosion(ref GameState state, int row, int col)
        {
            if (IsInBounds(state.Board, row, col))
            {
                if (IsBreakable(state.Board[row][col]))
                {
                    state.Board[row][col] = GameConstants.BrokenWall;
                    return false;
                }

                var bomb = GetBomb(state, row, col);

                if (bomb != null)
                {
                    ExplodeBomb(ref state, bomb);
                    return true;
                }

                var playerName = GetPlayerName(state, row, col);

                if (playerName != null)
                {
                    state.Players[playerName].IsAlive = false;
                    Debug.WriteLine("Player " + playerName + " has died x_x");
                    return true;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public bool HandleMove(string roomName, string playerName, int keyCode)
        {
            GameState state = GetGameState(roomName);
            currentState = state.Clone();
            Player player = state.Players[playerName];
            bool hasUpdatedState = false;

            if (IsUpMove(keyCode) && CanMoveIntoPosition(state, playerName, player.Row - 1, player.Col))
            {
                player.Row -= 1;
                hasUpdatedState = true;
            }
            else if (IsLeftMove(keyCode) && CanMoveIntoPosition(state, playerName, player.Row, player.Col - 1))
            {
                player.Col -= 1;
                hasUpdatedState = true;
            }
            else if (IsDownMove(keyCode) && CanMoveIntoPosition(state, playerName, player.Row + 1, player.Col))
            {
                player.Row += 1;
                hasUpdatedState = true;
            }
            else if (IsRightMove(keyCode) && CanMoveIntoPosition(state, playerName, player.Row, player.Col + 1))
            {
                player.Col += 1;
                hasUpdatedState = true;
            }
            else if (IsPlaceBomb(keyCode) && CanMoveIntoPosition(state, playerName, player.Row, player.Col))
            {
                state.Bombs.Add(new Bomb(BombDurationInMilliseconds, player));
                hasUpdatedState = true;
            }

            return hasUpdatedState;
        }

        private static void RemoveBrokenWalls(ref GameState state)
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

        private static bool CanMoveIntoPosition(in GameState state, string playerName, int row, int col)
        {
            if (!IsInBounds(state.Board, row, col))
            {
                return false;
            }

            if (IsLivingPlayingAtPosition(state.Players, playerName, row, col))
            {
                return false;
            }

            if (GetBomb(state, row, col) != null)
            {
                return false;
            }

            return state.Board[row][col] == GameConstants.Empty;
        }

        private static string GetPlayerName(in GameState state, int row, int col)
        {
            foreach (var playerName in state.Players.Keys)
            {
                var player = state.Players[playerName];

                if (player.Row == row && player.Col == col)
                {
                    return playerName;
                }
            }

            return null;
        }

        private static bool IsLivingPlayingAtPosition(in Dictionary<string, Player> players, string playerName, int row, int col)
        {
            string playerNameAtPosition = GetPlayerName(state, row, col);

            if (playerNameAtPosition == null)
            {
                return false;
            }

            // Do not consider the player themselves
            if (playerNameAtPosition.Equals(playerName))
            {
                return false;
            }

            return players[playerNameAtPosition].IsAlive;
        }

        private static Bomb GetBomb(in GameState state, int row, int col)
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

        private static bool IsInBounds(in List<List<int>> board, int row, int col)
        {
            return (row >= 0) && (col >= 0) && (row < board.Count) && (col < board[row].Count);
        }

        private static bool IsBreakable(int boardValue)
        {
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

        private GameState GetGameState(string roomName)
        {
            if (!gameStateOfRoom.TryGetValue(roomName, out var state))
            {
                throw new ArgumentException("Failed to get game state for room " + roomName);
            }

            return state;
        }

        private GameState UpdateGameState(string roomName)
        {
            if (!gameStateOfRoom.TryUpdate())
        }
    }
}
