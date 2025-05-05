namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using Core.DataStructure.Adapters;
    using Core.Extension;

    [Serializable]
    public readonly struct EffectHandle
        : IEquatable<EffectHandle>,
            IComparable<EffectHandle>,
            IComparable
    {
        public readonly AttributeEffect effect;

        public readonly KGuid id;

        public static EffectHandle CreateInstance(AttributeEffect effect)
        {
            return new EffectHandle(Guid.NewGuid(), effect);
        }

        private EffectHandle(KGuid id, AttributeEffect effect)
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
