namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Threading;
    using Core.Extension;

    [Serializable]
    public readonly struct EffectHandle
        : IEquatable<EffectHandle>,
            IComparable<EffectHandle>,
            IComparable
    {
        internal static long Id;

        public readonly AttributeEffect effect;

        public readonly long id;

        public static EffectHandle CreateInstance(AttributeEffect effect)
        {
            return new EffectHandle(Interlocked.Increment(ref Id), effect);
        }

        private EffectHandle(long id, AttributeEffect effect)
        {
            this.id = id;
            this.effect = effect;
        }

        public int CompareTo(EffectHandle other)
        {
            return id.CompareTo(other.id);
        }

        public int CompareTo(object obj)
        {
            if (obj is EffectHandle other)
            {
                return CompareTo(other);
            }

            return -1;
        }

        public override bool Equals(object obj)
        {
            return obj is EffectHandle other && Equals(other);
        }

        public bool Equals(EffectHandle other)
        {
            return id == other.id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override string ToString()
        {
            return this.ToJson();
        }
    }
}
