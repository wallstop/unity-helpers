namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using WallstopStudios.UnityHelpers.Core.Helper;

    /// <summary>
    /// Key used to group effect handles for stacking decisions.
    /// </summary>
    internal readonly struct EffectStackKey : IEquatable<EffectStackKey>
    {
        private readonly EffectStackGroup _group;
        private readonly AttributeEffect _effect;
        private readonly string _customKey;

        private EffectStackKey(EffectStackGroup group, AttributeEffect effect, string customKey)
        {
            _group = group;
            _effect = effect;
            _customKey = customKey;
        }

        public static EffectStackKey CreateReference(AttributeEffect effect)
        {
            return new EffectStackKey(EffectStackGroup.Reference, effect, null);
        }

        public static EffectStackKey CreateCustom(string customKey)
        {
            return new EffectStackKey(EffectStackGroup.CustomKey, null, customKey);
        }

        public bool Equals(EffectStackKey other)
        {
            if (_group != other._group)
            {
                return false;
            }

            return _group switch
            {
                EffectStackGroup.Reference => ReferenceEquals(_effect, other._effect),
                EffectStackGroup.CustomKey => string.Equals(
                    _customKey,
                    other._customKey,
                    StringComparison.Ordinal
                ),
                _ => false,
            };
        }

        public override bool Equals(object obj)
        {
            return obj is EffectStackKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _group switch
            {
                EffectStackGroup.Reference => Objects.HashCode(_group, _effect),
                EffectStackGroup.CustomKey => Objects.HashCode(
                    _group,
                    _customKey != null ? StringComparer.Ordinal.GetHashCode(_customKey) : 0
                ),
                _ => Objects.HashCode(_group),
            };
        }

        public static bool operator ==(EffectStackKey left, EffectStackKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EffectStackKey left, EffectStackKey right)
        {
            return !(left == right);
        }
    }
}
