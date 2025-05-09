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
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

    [Serializable]
    public sealed class AttributeEffect :
#if ODIN_INSPECTOR
        SerializedScriptableObject
#else
        ScriptableObject
#endif
            , IEquatable<AttributeEffect>
    {
        public string HumanReadableDescription => BuildDescription();

        public readonly List<AttributeModification> modifications = new();

        public ModifierDurationType durationType = ModifierDurationType.Duration;

#if ODIN_INSPECTOR
        [ShowIf("@durationType == ModifierDurationType.Duration")]
#endif
        public float duration;

#if ODIN_INSPECTOR
        [ShowIf("@durationType == ModifierDurationType.Duration")]
#endif
        public bool resetDurationOnReapplication;

        public List<string> effectTags = new();

        [JsonIgnore]
        public readonly List<CosmeticEffectData> cosmeticEffects = new();

        private List<string> CosmeticEffectsForJson =>
            cosmeticEffects
                ?.Select(cosmeticEffectData => cosmeticEffectData.name)
                .ToList(cosmeticEffects.Count) ?? new List<string>(0);

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

            StringBuilder descriptionBuilder = new();
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

        // Needed now since most things are based on serialized attribute effects and each unserialization will be a new instance
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

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is AttributeEffect other && Equals(other);
        }

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
