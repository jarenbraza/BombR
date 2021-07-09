using System;

namespace BombermanAspNet.Data
{
	public class Explosion : IComparable<Explosion>
    {
        private const long ExplosionDurationInMilliseconds = 1000;

        public Explosion(int row, int col)
        {
            Expiration = DateTime.Now;
            Expiration = Expiration.AddMilliseconds(ExplosionDurationInMilliseconds);
            Row = row;
            Col = col;
        }

        public DateTime Expiration { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        public bool IsExpired()
        {
            return Expiration <= DateTime.Now;
        }

        public int CompareTo(Explosion other)
        {
            return Expiration.CompareTo(other.Expiration);
        }
    }
}
