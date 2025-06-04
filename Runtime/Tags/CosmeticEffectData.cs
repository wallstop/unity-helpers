namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Helper;
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class CosmeticEffectData : MonoBehaviour, IEquatable<CosmeticEffectData>
    {
        // Is an instanced version of this gameObject created when applied.
        public bool RequiresInstancing =>
            _cosmetics.Value.Any(cosmeticEffect => cosmeticEffect.RequiresInstance);

        [NonSerialized]
        private readonly Lazy<CosmeticEffectComponent[]> _cosmetics;

        [NonSerialized]
        private readonly Lazy<HashSet<Type>> _cosmeticTypes;

        public CosmeticEffectData()
        {
            _cosmetics = new Lazy<CosmeticEffectComponent[]>(
                GetComponents<CosmeticEffectComponent>
            );
            _cosmeticTypes = new Lazy<HashSet<Type>>(() =>
                _cosmetics.Value.Select(cosmetic => cosmetic.GetType()).ToHashSet()
            );
        }

        public override bool Equals(object other)
        {
            return other is CosmeticEffectData cosmeticEffectData && Equals(cosmeticEffectData);
        }

        public bool Equals(CosmeticEffectData other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null || GetHashCode() != other.GetHashCode())
            {
                return false;
            }

            bool componentTypeEquals = _cosmeticTypes.Value.SetEquals(other._cosmeticTypes.Value);
            if (!componentTypeEquals)
            {
                return false;
            }

            return Helpers.NameEquals(this, other);
        }

        public override int GetHashCode()
        {
            return _cosmeticTypes.Value.Count.GetHashCode();
        }
    }
}
