using BombermanAspNet.Constants;
using BombermanAspNet.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BombermanAspNet.Utilities
{
    internal static class GameUtils
    {
        internal static bool HandleBombs(in GameState state)
        {
            bool hasExplodedBomb = false;
            Bomb nextBombToExpire = state.Bombs.Min;

            while (nextBombToExpire != null && nextBombToExpire.IsExpired())
            {
                hasExplodedBomb = true;
                ExplodeBomb(state, nextBombToExpire);
                nextBombToExpire = state.Bombs.Min;
            }

            if (hasExplodedBomb)
            {
                RemoveBrokenWalls(state);
            }

            return hasExplodedBomb;
        }

        internal static void ExplodeBomb(in GameState state, in Bomb bomb)
        {
            state.Bombs.Remove(bomb);

            state.Explosions.Add(new Explosion
            {
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

                    if (!AbleToExpandExplosion(state, explosionRow, explosionCol))
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

        internal static bool AbleToExpandExplosion(in GameState state, int row, int col)
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
                ExplodeBomb(state, bomb);
            }

            TryKillPlayerAtLocation(row, col, state);

            return true;
        }

        internal static void RemoveBrokenWalls(in GameState state)
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

        internal static bool HandleExplosions(in GameState state)
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

        internal static bool IsInBounds(int row, int col, in GameState state)
        {
            return (row >= 0)
                && (col >= 0)
                && (row < state.Board.Count)
                && (col < state.Board[row].Count);
        }

        internal static bool IsUpMove(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeW || keyCode == GameConstants.KeyCodeUp;
        }

        internal static bool IsLeftMove(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeA || keyCode == GameConstants.KeyCodeLeft;
        }

        internal static bool IsDownMove(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeS || keyCode == GameConstants.KeyCodeDown;
        }

        internal static bool IsRightMove(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeD || keyCode == GameConstants.KeyCodeRight;
        }

        internal static bool IsPlaceBomb(int keyCode)
        {
            return keyCode == GameConstants.KeyCodeSpace;
        }

        internal static string GetGameKey(string roomName)
        {
            return nameof(GameState) + "_" + roomName;
        }

        internal static Player CreatePlayer(string name)
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

        internal static bool TryKillPlayerAtLocation(int row, int col, in GameState state)
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

        internal static bool IsOtherPlayerAliveAtPlayerLocation(Player player, in GameState state)
        {
            // Do not consider the players themselves
            var otherPlayer = state.Players.Find(o => (o.Row == player.Row) && (o.Col == player.Col) && !o.Name.Equals(player.Name));

            return otherPlayer != null && otherPlayer.IsAlive;
        }

        internal static bool IsExplosion(int row, int col, in GameState gameState)
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

        internal static Bomb GetBomb(int row, int col, in GameState state)
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

        internal static bool IsBreakable(int row, int col, in GameState state)
        {
            int boardValue = state.Board[row][col];
            return boardValue == GameConstants.BreakableWall || boardValue == GameConstants.BrokenWall;
        }

        internal static bool IsUnbreakable(int row, int col, in GameState state)
        {
            return state.Board[row][col] == GameConstants.UnbreakableWall;
        }

        internal static bool IsValidMove(int row, int col, in Player player, in GameState state)
        {
            if (!IsInBounds(row, col, state))
            {
                Debug.WriteLine("bounds");
                return false;
            }

            if (IsOtherPlayerAliveAtPlayerLocation(player, state))
            {
                Debug.WriteLine("other p");
                return false;
            }

            if (GetBomb(row, col, state) != null)
            {
                Debug.WriteLine("bomb");
                return false;
            }

            return IsEmpty(row, col, state.Board);
        }

        internal static bool IsEmpty(int row, int col, in List<List<int>> board)
        {
            return board[row][col] == GameConstants.Empty;
        }
    }
}
