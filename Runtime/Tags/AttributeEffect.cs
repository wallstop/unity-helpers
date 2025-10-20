namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using Core.Extension;
    using Core.Helper;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

    /// <summary>
    /// Reusable, data‑driven bundle of stat modifications, tags, and cosmetic feedback.
    /// Serves as the authoring unit for buffs, debuffs, and status effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Composition:
    /// - Attribute modifications: a list of <see cref="AttributeModification"/> applied to <see cref="Attribute"/> fields
    /// - Tags: string markers for cross‑system state gating and queries
    /// - Cosmetics: <see cref="CosmeticEffectData"/> references for visuals/audio on apply/remove
    /// - Duration: <see cref="ModifierDurationType"/> with seconds and reapplication policy
    /// </para>
    /// <para>
    /// Problems solved and benefits:
    /// - Centralizes effect logic and presentation in one asset
    /// - Safely stacks via <see cref="EffectHandle"/> per application
    /// - Works with <see cref="EffectHandler"/> and <see cref="TagHandler"/> for lifecycle and state tracking
    /// - Author once, reuse everywhere (designers can tweak without code changes)
    /// </para>
    /// <para>
    /// Usage examples:
    /// <code>
    /// // Create a speed boost effect in the editor
    /// // Then apply it to a GameObject:
    /// GameObject player = ...;
    /// AttributeEffect speedBoost = ...; // ScriptableObject reference
    /// EffectHandle? handle = player.ApplyEffect(speedBoost);
    ///
    /// // Instant vs Duration vs Infinite
    /// //  - Instant: modifies base values permanently, returns null handle
    /// //  - Duration: temporary, expires automatically, returns handle
    /// //  - Infinite: persists until RemoveEffect(handle) is called, returns handle
    ///
    /// // Removing later
    /// if (handle.HasValue) player.RemoveEffect(handle.Value);
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    [CreateAssetMenu(menuName = "Wallstop Studios/Unity Helpers/Attribute Effect")]
    public sealed class AttributeEffect :
#if ODIN_INSPECTOR
        SerializedScriptableObject
#else
        ScriptableObject
#endif
            , IEquatable<AttributeEffect>
    {
        /// <summary>
        /// Gets a human-readable description of this effect based on its modifications.
        /// The description is automatically generated from the modifications list.
        /// </summary>
        /// <value>A formatted string describing all modifications in this effect.</value>
        /// <example>"+20 Health, +1.5x Speed, -10% Defense"</example>
        public string HumanReadableDescription => BuildDescription();

        /// <summary>
        /// The list of attribute modifications to apply when this effect is activated.
        /// Each modification specifies an attribute name, action type, and value.
        /// </summary>
        public List<AttributeModification> modifications = new();

        /// <summary>
        /// Specifies how long this effect should persist (Instant, Duration, or Infinite).
        /// </summary>
        public ModifierDurationType durationType = ModifierDurationType.Duration;

#if ODIN_INSPECTOR
        [ShowIf("@durationType == ModifierDurationType.Duration")]
#endif
        /// <summary>
        /// The duration in seconds for this effect. Only used when <see cref="durationType"/> is <see cref="ModifierDurationType.Duration"/>.
        /// </summary>
        public float duration;

#if ODIN_INSPECTOR
        [ShowIf("@durationType == ModifierDurationType.Duration")]
#endif
        /// <summary>
        /// If true, reapplying this effect while it's already active will reset the duration timer.
        /// Only used when <see cref="durationType"/> is <see cref="ModifierDurationType.Duration"/>.
        /// </summary>
        /// <example>
        /// A poison effect with resetDurationOnReapplication=true will restart its 5-second timer
        /// each time the poison is reapplied, preventing stacking but extending the effect.
        /// </example>
        public bool resetDurationOnReapplication;

        /// <summary>
        /// A list of string tags that are applied when this effect is active.
        /// Tags can be used to track effect categories, prevent certain actions, or enable special behaviors.
        /// </summary>
        /// <example>
        /// Tags like "Stunned", "Poisoned", "Invulnerable" can be checked by game systems
        /// to determine if certain actions should be allowed or prevented.
        /// </example>
        public List<string> effectTags = new();

        /// <summary>
        /// Determines whether this effect applies the specified tag.
        /// </summary>
        /// <param name="effectTag">The tag to check.</param>
        /// <returns><c>true</c> if the tag is present; otherwise, <c>false</c>.</returns>
        public bool HasTag(string effectTag)
        {
            if (effectTags == null || string.IsNullOrEmpty(effectTag))
            {
                return false;
            }

            for (int i = 0; i < effectTags.Count; ++i)
            {
                if (string.Equals(effectTags[i], effectTag, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether this effect applies any of the specified tags.
        /// </summary>
        /// <param name="effectTagsToCheck">The tags to inspect.</param>
        /// <returns><c>true</c> if at least one tag is applied; otherwise, <c>false</c>.</returns>
        public bool HasAnyTag(IEnumerable<string> effectTagsToCheck)
        {
            if (effectTags == null || effectTagsToCheck == null)
            {
                return false;
            }

            switch (effectTagsToCheck)
            {
                case IReadOnlyList<string> list:
                {
                    return HasAnyTag(list);
                }
                case HashSet<string> hashSet:
                {
                    foreach (string candidate in hashSet)
                    {
                        if (string.IsNullOrEmpty(candidate))
                        {
                            continue;
                        }

                        if (HasTag(candidate))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            foreach (string candidate in effectTagsToCheck)
            {
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                if (HasTag(candidate))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether this effect applies any of the specified tags.
        /// Optimized for indexed collections.
        /// </summary>
        /// <param name="effectTagsToCheck">The tags to inspect.</param>
        /// <returns><c>true</c> if at least one tag is applied; otherwise, <c>false</c>.</returns>
        public bool HasAnyTag(IReadOnlyList<string> effectTagsToCheck)
        {
            if (effectTags == null || effectTagsToCheck == null)
            {
                return false;
            }

            for (int i = 0; i < effectTagsToCheck.Count; ++i)
            {
                string candidate = effectTagsToCheck[i];
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                if (HasTag(candidate))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether this effect contains modifications for the specified attribute.
        /// </summary>
        /// <param name="attributeName">The attribute name to inspect.</param>
        /// <returns><c>true</c> if the effect modifies <paramref name="attributeName"/>; otherwise, <c>false</c>.</returns>
        public bool ModifiesAttribute(string attributeName)
        {
            if (modifications == null || string.IsNullOrEmpty(attributeName))
            {
                return false;
            }

            for (int i = 0; i < modifications.Count; ++i)
            {
                AttributeModification modification = modifications[i];
                if (string.Equals(modification.attribute, attributeName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copies all modifications that affect the specified attribute into the provided buffer.
        /// </summary>
        /// <param name="attributeName">The attribute to filter by.</param>
        /// <param name="buffer">The destination buffer. Existing entries are preserved.</param>
        /// <returns>The number of modifications added to <paramref name="buffer"/>.</returns>
        public List<AttributeModification> GetModifications(
            string attributeName,
            List<AttributeModification> buffer = null
        )
        {
            buffer ??= new List<AttributeModification>();
            buffer.Clear();
            if (modifications == null || string.IsNullOrEmpty(attributeName))
            {
                return buffer;
            }

            for (int i = 0; i < modifications.Count; ++i)
            {
                AttributeModification modification = modifications[i];
                if (string.Equals(modification.attribute, attributeName, StringComparison.Ordinal))
                {
                    buffer.Add(modification);
                }
            }

            return buffer;
        }

        /// <summary>
        /// A list of cosmetic effect data that defines visual and audio feedback for this effect.
        /// These are applied when the effect becomes active and removed when it expires.
        /// </summary>
        [JsonIgnore]
        public List<CosmeticEffectData> cosmeticEffects = new();

        private List<string> CosmeticEffectsForJson =>
            cosmeticEffects?.Select(cosmeticEffectData => cosmeticEffectData.name).ToList()
            ?? new List<string>(0);

        /// <summary>
        /// Converts this effect to a JSON string representation including all modifications, tags, and cosmetic effects.
        /// </summary>
        /// <returns>A JSON string representing this effect.</returns>
        public override string ToString()
        {
            return new
            {
                Description = HumanReadableDescription,
                CosmeticEffects = CosmeticEffectsForJson,
                modifications,
                durationType,
                duration,
                tags = effectTags,
            }.ToJson();
        }

        private string BuildDescription()
        {
            if (modifications == null)
            {
                return nameof(AttributeEffect);
            }

            using PooledResource<StringBuilder> stringBuilderBuffer = Buffers.StringBuilder.Get(
                out StringBuilder descriptionBuilder
            );
            for (int i = 0; i < modifications.Count; ++i)
            {
                AttributeModification modification = modifications[i];
                switch (modification.action)
                {
                    case ModificationAction.Addition:
                    {
                        if (modification.value < 0)
                        {
                            _ = descriptionBuilder.Append(modification.value);
                            _ = descriptionBuilder.Append(' ');
                        }
                        else if (modification.value == 0)
                        {
                            continue;
                        }
                        else
                        {
                            _ = descriptionBuilder.AppendFormat("+{0} ", modification.value);
                        }

                        break;
                    }
                    case ModificationAction.Multiplication:
                    {
                        if (modification.value < 1)
                        {
                            _ = descriptionBuilder.AppendFormat(
                                "-{0}% ",
                                (1 - modification.value) * 100
                            );
                        }
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        else if (modification.value == 1)
                        {
                            continue;
                        }
                        else
                        {
                            _ = descriptionBuilder.AppendFormat(
                                "+{0}% ",
                                (modification.value - 1) * 100
                            );
                        }

                        break;
                    }
                    case ModificationAction.Override:
                    {
                        _ = descriptionBuilder.AppendFormat("{0} ", modification.value);
                        break;
                    }
                    default:
                    {
                        throw new InvalidEnumArgumentException(
                            nameof(modification.value),
                            (int)modification.value,
                            typeof(ModificationAction)
                        );
                    }
                }

                _ = descriptionBuilder.Append(modification.attribute.ToPascalCase(" "));
                if (i < modifications.Count - 1)
                {
                    _ = descriptionBuilder.Append(", ");
                }
            }

            return descriptionBuilder.ToString();
        }

        /// <summary>
        /// Determines whether this effect is equal to another effect by comparing all fields.
        /// This is needed because deserialization creates new instances, so reference equality is insufficient.
        /// </summary>
        /// <param name="other">The effect to compare with.</param>
        /// <returns><c>true</c> if all fields match; otherwise, <c>false</c>.</returns>
        public bool Equals(AttributeEffect other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            if (!string.Equals(name, other.name))
            {
                return false;
            }

            if (durationType != other.durationType)
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (duration != other.duration)
            {
                return false;
            }

            if (resetDurationOnReapplication != other.resetDurationOnReapplication)
            {
                return false;
            }

            if (modifications == null)
            {
                if (other.modifications != null)
                {
                    return false;
                }
            }
            else if (other.modifications == null)
            {
                return false;
            }
            else
            {
                if (modifications.Count != other.modifications.Count)
                {
                    return false;
                }

                for (int i = 0; i < modifications.Count; ++i)
                {
                    if (modifications[i] != other.modifications[i])
                    {
                        return false;
                    }
                }
            }

            if (effectTags == null)
            {
                if (other.effectTags != null)
                {
                    return false;
                }
            }
            else if (other.effectTags == null)
            {
                return false;
            }
            else
            {
                if (effectTags.Count != other.effectTags.Count)
                {
                    return false;
                }

                for (int i = 0; i < effectTags.Count; ++i)
                {
                    if (
                        !string.Equals(effectTags[i], other.effectTags[i], StringComparison.Ordinal)
                    )
                    {
                        return false;
                    }
                }
            }

            if (cosmeticEffects == null)
            {
                if (other.cosmeticEffects != null)
                {
                    return false;
                }
            }
            else if (other.cosmeticEffects == null)
            {
                return false;
            }
            else
            {
                if (cosmeticEffects.Count != other.cosmeticEffects.Count)
                {
                    return false;
                }

                for (int i = 0; i < cosmeticEffects.Count; ++i)
                {
                    if (!Equals(cosmeticEffects[i], other.cosmeticEffects[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether this effect equals the specified object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> if the object is an AttributeEffect with equal values; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is AttributeEffect other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this effect based on its configuration.
        /// </summary>
        /// <returns>A hash code combining counts of modifications, tags, and cosmetic effects.</returns>
        public override int GetHashCode()
        {
            return Objects.HashCode(
                modifications?.Count,
                durationType,
                duration,
                resetDurationOnReapplication,
                effectTags?.Count,
                cosmeticEffects?.Count
            );
        }
    }
}
