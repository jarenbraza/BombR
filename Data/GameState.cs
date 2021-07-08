using System;
using System.Collections.Generic;

namespace BombermanAspNet.Data
{
    public class GameState
    {
        public const int RowCount = 13;
        public const int ColCount = 15;
        private readonly Random random = new Random();

        public GameState()
        {
            Players = new();
            Board = new();
            Bombs = new();
            Explosions = new();

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
                    else if (InPlayerSafePosition(r, c) && random.Next(0, 100) < 60)
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

        public Dictionary<string, Player> Players { get; set; }
        public List<List<int>> Board { get; set; }
        public SortedSet<Bomb> Bombs { get; set; }
        public SortedSet<Explosion> Explosions { get; set; }

        public GameState Clone()
        {
            GameState clonedState = new GameState();

            // TODO: Check if the internal class (Player, List<int>, Bomb, Explosion) actually get deep copied or not.
            clonedState.Players = new Dictionary<string, Player>(Players);
            clonedState.Board = new List<List<int>>(Board);
            clonedState.Bombs = new SortedSet<Bomb>(Bombs);
            clonedState.Explosions = new SortedSet<Explosion>(Explosions);

            return clonedState;
        }

        // TODO: Figure out better way not to hard code safely placing breakable walls
        private bool InPlayerSafePosition(int row, int col)
        {
            return (row != 1 || (col != 1 && col != 2 && col != ColCount - 2 && col != ColCount - 3))
                && (row != 2 || (col != 1 && col != ColCount - 2))
                && (row != RowCount - 3 || (col != 1 && col != ColCount - 2))
                && (row != RowCount - 2 || (col != 1 && col != 2 && col != ColCount - 2 && col != ColCount - 3));
        }
    }
}
