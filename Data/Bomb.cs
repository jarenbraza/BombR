using System;
using System.Diagnostics;
using System.Globalization;

namespace BombermanAspNet.Data
{
    public class Bomb : IComparable<Bomb>
    {
        public Bomb(long durationInMilliseconds, Player player)
        {
            Expiration = DateTime.Now;
            Expiration = Expiration.AddMilliseconds(durationInMilliseconds);
            Row = player.Row;
            Col = player.Col;
            ExplosionDistance = player.ExplosionDistance;
        }

        public DateTime Expiration { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public int ExplosionDistance { get; set; }

        public bool IsExpired()
        {
            return Expiration <= DateTime.Now;
        }

        public int CompareTo(Bomb other)
        {
            return Expiration.CompareTo(other.Expiration);
        }
    }
}
