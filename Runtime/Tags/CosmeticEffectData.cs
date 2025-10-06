namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Helper;
    using UnityEngine;

    /// <summary>
    /// Container component for CosmeticEffectComponents.
    /// Manages a collection of cosmetic components and determines if the GameObject requires instancing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// CosmeticEffectData serves as a prefab-like container for cosmetic effects. It:
    /// - Groups multiple CosmeticEffectComponent instances
    /// - Determines if instancing is needed based on child components
    /// - Provides equality comparison based on component types
    /// </para>
    /// <para>
    /// Attached to a GameObject with one or more CosmeticEffectComponent children.
    /// Referenced by AttributeEffect to define visual/audio feedback.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CosmeticEffectData : MonoBehaviour, IEquatable<CosmeticEffectData>
    {
        /// <summary>
        /// Indicates whether this cosmetic effect requires a new instance for each application.
        /// Returns true if any child CosmeticEffectComponent requires instancing.
        /// </summary>
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
