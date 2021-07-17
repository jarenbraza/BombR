namespace BombermanAspNet.Models
{
    /// <summary>Represents a player in the game.</summary>
    public class Player
    {
        internal static int previousRow = GameState.RowCount - 2;
        internal static int previousCol = 1;

        /// <summary>The name of the <see cref="Player"/>.</summary>
        public string Name { get; set; }

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

        // TODO: Refactor into utility
        internal static int GetNextRow()
        {
            var nextRow = (previousCol == 1) ? 1 :GameState.RowCount - 2;
            previousRow = nextRow;
            return nextRow;
        }

        // TODO: Refactor into utility
        internal static int GetNextCol()
        {
            var nextCol = (previousRow == 1) ? GameState.ColCount - 2 : 1;
            previousCol = nextCol;
            return nextCol;
        }
    }
}