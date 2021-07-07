using System;

namespace BombermanAspNet.Data
{
    public class Player
    {
        private static int previousRow = GameState.RowCount - 2;
        private static int previousCol = 1;

        public Player()
        {
            RemainingBombs = 1;
            ExplosionDistance = 2;
            IsAlive = true;
            Row = GetNextRow();
            Col = GetNextCol();
            previousRow = Row;
            previousCol = Col;
        }

        public int Row { get; set; }
        public int Col { get; set; }
        public int RemainingBombs { get; set; }
        public int ExplosionDistance { get; set; }
        public bool IsAlive { get; set; }

        public int GetNextRow()
        {
            if (previousCol == 1)
            {
                return 1;
            }

            return GameState.RowCount - 2;
        }

        public int GetNextCol()
        {
            if (previousRow == 1)
            {
                return GameState.ColCount - 2;
            }

            return 1;
        }
    }
}