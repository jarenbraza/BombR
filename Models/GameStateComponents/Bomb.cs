using System;

namespace BombermanAspNet.Models
{
    /// <summary>
    /// Represents a bomb in the game.
    /// </summary>
	public class Bomb : IComparable<Bomb>
    {
        /// <summary>The <see cref="DateTime"/> when the <see cref="Bomb"/> will expire.</summary>
        public DateTime Expiration { get; set; }

        /// <summary>The row of the <see cref="Bomb"/> on the game board.</summary>
        public int Row { get; set; }

        /// <summary>The column of the <see cref="Bomb"/> on the game board.</summary>
        public int Col { get; set; }

        /// <summary>The maximum explosion range after the <see cref="Bomb"/> expires.</summary>
        public int ExplosionDistance { get; set; }

        /// <summary>
        /// Checks if the expiration time of the <see cref="Bomb"/> has exceeded
        /// <see cref="DateTime.Now"/>.
        /// </summary>
        /// <returns>true if the <see cref="Bomb"/> has expired; otherwise, false.</returns>
        public bool IsExpired()
        {
            return Expiration <= DateTime.Now;
        }

        /// <summary>
        /// Compares the current instance with another <see cref="Bomb"/> and returns
        /// an integer that indicates whether the current instance precedes, follows, or
        /// occurs in the same position in the sort order as the other object.
        /// 
        /// <para>The expiration time of each instance is used for the comparison.</para>
        /// </summary>
        /// <inheritdoc/>
        public int CompareTo(Bomb other)
        {
            return Expiration.CompareTo(other.Expiration);
        }
    }
}
