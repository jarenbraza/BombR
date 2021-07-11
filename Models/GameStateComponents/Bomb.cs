using System;

namespace BombermanAspNet.Models
{
	public class Bomb : IComparable<Bomb>
    {
        private const long BombDurationInMilliseconds = 3000;

        public Bomb(Player player)
        {
            Expiration = DateTime.Now;
            Expiration = Expiration.AddMilliseconds(BombDurationInMilliseconds);
            Player = player;
        }

        public DateTime Expiration { get; set; }
        public Player Player { get; set; }
        public int Row { get { return Player.Row; } }
        public int Col { get { return Player.Col; } }
        public int ExplosionDistance { get { return Player.ExplosionDistance; } }

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
