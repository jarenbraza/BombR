namespace BombermanAspNet.Models
{
    /// <summary>Represents a player in the game.</summary>
    public class Player
    {
        private static int previousRow = GameState.RowCount - 2;
        private static int previousCol = 1;

        /// <summary>The row of the <see cref="Player"/> on the game board.</summary>
        public int Row { get; set; }

        /// <summary>The row of the <see cref="Player"/> on the game board.</summary>
        public int Col { get; set; }

        /// <summary>The number of bombs the player is able to place at the current time.</summary>
        public int RemainingBombs { get; set; }

        /// <summary>The maximum explosion range after any <see cref="Bomb"/> placed by the <see cref="Player"/> expires.</summary>
        public int ExplosionDistance { get; set; }

        /// <summary>A flag of whether the player is alive or not.</summary>
        public bool IsAlive { get; set; }

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

        // TODO: Refactor into utility
        public int GetNextRow()
        {
            if (previousCol == 1)
            {
                return 1;
            }

            return GameState.RowCount - 2;
        }

        // TODO: Refactor into utility
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