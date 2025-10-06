namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Threading;
    using Core.Extension;

    /// <summary>
    /// Represents a unique handle to an applied effect instance.
    /// EffectHandles are used to track and remove specific effect applications, especially for non-instant effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When an effect with <see cref="ModifierDurationType.Duration"/> or <see cref="ModifierDurationType.Infinite"/>
    /// is applied, an EffectHandle is created. This handle uniquely identifies that specific application,
    /// allowing it to be removed independently from other instances of the same effect.
    /// </para>
    /// <para>
    /// Each EffectHandle contains:
    /// - A unique ID for tracking
    /// - A reference to the AttributeEffect that was applied
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// AttributeEffect speedBoost = ...;
    /// EffectHandle? handle = player.ApplyEffect(speedBoost);
    ///
    /// // Later, remove this specific instance:
    /// if (handle.HasValue)
    /// {
    ///     player.RemoveEffect(handle.Value);
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    public readonly struct EffectHandle
        : IEquatable<EffectHandle>,
            IComparable<EffectHandle>,
            IComparable
    {
        internal static long Id;

        /// <summary>
        /// The AttributeEffect associated with this handle.
        /// </summary>
        public readonly AttributeEffect effect;

        /// <summary>
        /// The unique identifier for this effect instance.
        /// This ID is globally unique and monotonically increasing.
        /// </summary>
        public readonly long id;

        /// <summary>
        /// Creates a new EffectHandle instance with a unique ID for the specified effect.
        /// </summary>
        /// <param name="effect">The AttributeEffect to create a handle for.</param>
        /// <returns>A new EffectHandle with a unique ID.</returns>
        public static EffectHandle CreateInstance(AttributeEffect effect)
        {
            return new EffectHandle(Interlocked.Increment(ref Id), effect);
        }

        private EffectHandle(long id, AttributeEffect effect)
        {
            this.id = id;
            this.effect = effect;
        }

        /// <summary>
        /// Compares this handle to another handle based on their IDs.
        /// </summary>
        /// <param name="other">The handle to compare with.</param>
        /// <returns>
        /// A value less than 0 if this handle's ID is less than <paramref name="other"/>;
        /// 0 if they are equal;
        /// a value greater than 0 if this handle's ID is greater than <paramref name="other"/>.
        /// </returns>
        public int CompareTo(EffectHandle other)
        {
            return id.CompareTo(other.id);
        }

        /// <summary>
        /// Compares this handle to an object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>
        /// The comparison result if <paramref name="obj"/> is an EffectHandle; otherwise, -1.
        /// </returns>
        public int CompareTo(object obj)
        {
            if (obj is EffectHandle other)
            {
                return CompareTo(other);
            }

            return -1;
        }

        /// <summary>
        /// Determines whether this handle equals the specified object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> if the object is an EffectHandle with the same ID; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is EffectHandle other && Equals(other);
        }

        /// <summary>
        /// Determines whether this handle equals another handle by comparing their IDs.
        /// </summary>
        /// <param name="other">The handle to compare with.</param>
        /// <returns><c>true</c> if the IDs are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(EffectHandle other)
        {
            return id == other.id;
        }

        /// <summary>
        /// Returns the hash code for this handle based on its ID.
        /// </summary>
        /// <returns>The hash code of the ID.</returns>
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        /// <summary>
        /// Converts this handle to a JSON string representation.
        /// </summary>
        /// <returns>A JSON string representing this handle.</returns>
        public override string ToString()
        {
            return this.ToJson();
        }
    }
}
