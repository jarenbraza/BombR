using BombermanAspNet.Constants;
using System;
using System.Collections.Generic;

namespace BombermanAspNet.Models
{
	public class GameState
    {
        public const int RowCount = 13;
        public const int ColCount = 15;
        private readonly Random random = new();

        public Dictionary<string, Player> Players { get; set; }
        public List<List<int>> Board { get; set; }
        public SortedSet<Bomb> Bombs { get; set; }
        public SortedSet<Explosion> Explosions { get; set; }
        public bool HasWinner { get; set; }

        public GameState()
        {
            Players = new();
            Board = new();
            Bombs = new();
            Explosions = new();
            HasWinner = false;

            for (int r = 0; r < RowCount; r++)
            {
                Board.Add(new List<int>());

                for (int c = 0; c < ColCount; c++)
                {
                    if (r == 0 || c == 0 || r == RowCount - 1 || c == ColCount - 1)
                    {
                        Board[r].Add(GameConstants.UnbreakableWall);
                    }
                    else if (r % 2 == 0 && c % 2 == 0)
                    {
                        Board[r].Add(GameConstants.UnbreakableWall);
                    }
                    else if (IsSafeWallPlacement(r, c) && random.Next(0, 100) < 60)
                    {
                        Board[r].Add(GameConstants.BreakableWall);
                    }
                    else
                    {
                        Board[r].Add(GameConstants.Empty);
                    }
                }
            }
        }

        // Hard coded safe placements of walls
        private static bool IsSafeWallPlacement(int row, int col)
        {
            return (row != 1 || (col > 2 && col < ColCount - 3))
                && (row != 2 || (col > 1 && col < ColCount - 2))
                && (row != RowCount - 3 || (col > 1 && col < ColCount - 2))
                && (row != RowCount - 2 || (col > 2 && col < ColCount - 3));
        }
    }
}
