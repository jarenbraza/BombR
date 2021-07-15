using System;

namespace BombermanAspNet.Models
{
    public class Explosion : IComparable<Explosion>
    {
        /// <summary>The <see cref="DateTime"/> when the <see cref="Explosion"/> will expire.</summary>
        public DateTime Expiration { get; set; }

        /// <summary>The row of the <see cref="Explosion"/> on the game board.</summary>
        public int Row { get; set; }

        /// <summary>The column of the <see cref="Explosion"/> on the game board.</summary>
        public int Col { get; set; }

        /// <summary>
        /// Checks if the expiration time of the <see cref="Explosion"/> has exceeded
        /// <see cref="DateTime.Now"/>.
        /// </summary>
        /// <returns>true if the <see cref="Explosion"/> has expired; otherwise, false.</returns>
        public bool IsExpired()
        {
            return Expiration <= DateTime.Now;
        }

        /// <summary>
        /// Compares the current instance with another <see cref="Explosion"/> and returns
        /// an integer that indicates whether the current instance precedes, follows, or
        /// occurs in the same position in the sort order as the other object.
        /// 
        /// <para>The expiration time of each instance is used for the comparison.</para>
        /// </summary>
        /// <inheritdoc/>
        public int CompareTo(Explosion other)
        {
            return Expiration.CompareTo(other.Expiration);
        }
    }
}
