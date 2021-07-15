using System.Collections.Generic;

namespace BombermanAspNet.Constants
{
    public static class GameConstants
    {
        // Board constants
        public const int Empty = 0;
        public const int BreakableWall = 1;
        public const int UnbreakableWall = 2;
        public const int Bomb = 3;
        public const int BrokenWall = 4;

        // Game logic
        public static readonly int[,] Directions = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };
        public const int BombDurationInMilliseconds = 3000;
        public const long ExplosionDurationInMilliseconds = 1000;

        // Key codes used in game
        public const int KeyCodeW = 87;
        public const int KeyCodeA = 65;
        public const int KeyCodeS = 83;
        public const int KeyCodeD = 68;

        public const int KeyCodeUp = 38;
        public const int KeyCodeLeft = 37;
        public const int KeyCodeDown = 40;
        public const int KeyCodeRight = 39;

        public const int KeyCodeSpace = 32;

        public static HashSet<int> ValidKeyCodes = new()
        {
            KeyCodeW,
            KeyCodeA,
            KeyCodeS,
            KeyCodeD,
            KeyCodeUp,
            KeyCodeLeft,
            KeyCodeDown,
            KeyCodeRight,
            KeyCodeSpace
        };
    }
}
