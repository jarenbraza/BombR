namespace BombermanAspNet.Data
{
    public static class GameConstants
    {
        // Board constants
        public const int Empty = 0;
        public const int BreakableWall = 1;
        public const int UnbreakableWall = 2;
        public const int Bomb = 3;
        public const int BrokenWall = 4;

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
    }
}
