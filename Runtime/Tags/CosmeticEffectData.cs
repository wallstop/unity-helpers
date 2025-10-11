namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Helper;
    using UnityEngine;

    /// <summary>
    /// Prefab-like container for visual/audio behaviors that represent an effect’s cosmetic feedback.
    /// Groups one or more <see cref="CosmeticEffectComponent"/>s and declares if instancing is required.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Role in the system: <see cref="AttributeEffect"/> references one or more CosmeticEffectData assets.
    /// When the effect is applied, <see cref="EffectHandler"/> will either:
    /// - Reuse the existing CosmeticEffectData on the target (RequiresInstancing = false), OR
    /// - Instantiate a copy and parent it to the target (RequiresInstancing = true).
    /// On removal, corresponding cosmetic components receive <see cref="CosmeticEffectComponent.OnRemoveEffect"/>.
    /// </para>
    /// <para>
    /// Problems solved:
    /// - Decouple gameplay logic from presentation.
    /// - Support shared cosmetic presenters (e.g., a single status icon) or per‑instance visuals (e.g., particle emitters).
    /// - Automatic lifecycle management (instantiation and cleanup) alongside effect application/removal.
    /// </para>
    /// <para>
    /// Authoring pattern:
    /// - Create a prefab with a CosmeticEffectData + one or more CosmeticEffectComponent scripts.
    /// - Mark a component’s <see cref="CosmeticEffectComponent.RequiresInstance"/> true if a unique instance per effect is needed.
    /// - Reference the prefab in your <see cref="AttributeEffect.cosmeticEffects"/> list.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// // PoisonEffectData (Prefab)
    /// //  - CosmeticEffectData
    /// //  - PoisonParticles : CosmeticEffectComponent (RequiresInstance = true)
    /// //  - PoisonIcon : CosmeticEffectComponent (shared UI, RequiresInstance = false)
    ///
    /// // In AttributeEffect: cosmeticEffects = [ PoisonEffectData ]
    /// // EffectHandler will instance PoisonParticles per application and reuse PoisonIcon as needed.
    /// </code>
    /// </para>
    /// <para>
    /// Tips:
    /// - Prefer shared presenters when possible (fewer instantiations).
    /// - If a component animates its own teardown, set <see cref="CosmeticEffectComponent.CleansUpSelf"/> to true.
    /// - Keep CosmeticEffectData lightweight; heavy content belongs in the child components.
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
