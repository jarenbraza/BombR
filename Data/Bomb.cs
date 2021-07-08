using System;

namespace BombermanAspNet.Data
{
	public class Bomb : IComparable<Bomb>
    {
        private const long BombDurationInMilliseconds = 3000;

        public Bomb(Player player)
        {
            Expiration = DateTime.Now;
            Expiration = Expiration.AddMilliseconds(BombDurationInMilliseconds);
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
