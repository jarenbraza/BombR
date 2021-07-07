using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BombermanAspNet.Data
{
    public class Explosion : IComparable<Explosion>
    {
        public Explosion(long durationInMilliseconds, int row, int col)
        {
            Expiration = new DateTime();
            Expiration.AddMilliseconds(durationInMilliseconds);
            Row = row;
            Col = col;
        }

        public DateTime Expiration { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        public bool IsExpired()
        {
            return Expiration >= new DateTime();
        }

        public int CompareTo(Explosion other)
        {
            return Expiration.CompareTo(other.Expiration);
        }
    }
}
