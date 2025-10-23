namespace WallstopStudios.UnityHelpers.Tests.Tags
{
    using System;
    using System.Text.Json;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using Attribute = WallstopStudios.UnityHelpers.Tags.Attribute;

    public abstract class AttributeTagsTestBase : CommonTestBase
    {
        protected static void ResetEffectHandleId(long value = 0)
        {
            EffectHandle.Id = value;
        }
    }

    [TestFixture]
    public sealed class AttributeModificationTests
    {
        [Test]
        public void EqualityOperatorsRespectFields()
        {
            AttributeModification baseline = new()
            {
                attribute = "health",
                action = ModificationAction.Multiplication,
                value = 1.5f,
            };

            AttributeModification clone = baseline;
            Assert.IsTrue(baseline == clone);
            Assert.IsFalse(baseline != clone);
            Assert.AreEqual(baseline, clone);
            Assert.AreEqual(baseline.GetHashCode(), clone.GetHashCode());

            AttributeModification differentAttribute = baseline;
            differentAttribute.attribute = "armor";
            Assert.IsFalse(baseline == differentAttribute);
            Assert.IsTrue(baseline != differentAttribute);

            AttributeModification differentAction = baseline;
            differentAction.action = ModificationAction.Addition;
            Assert.IsFalse(baseline.Equals(differentAction));

            AttributeModification differentValue = baseline;
            differentValue.value = 2f;
            Assert.IsFalse(baseline.Equals(differentValue));
        }

        [Test]
        public void ToStringSerializesAllFields()
        {
            AttributeModification modification = new()
            {
                attribute = "health",
                action = ModificationAction.Override,
                value = 42.5f,
            };

            using JsonDocument document = JsonDocument.Parse(modification.ToString());
            JsonElement root = document.RootElement;
            Assert.AreEqual("health", root.GetProperty("attribute").GetString());
            Assert.AreEqual("Override", root.GetProperty("action").GetString());
            Assert.AreEqual(42.5f, root.GetProperty("value").GetSingle());
        }
    }

    [TestFixture]
    public sealed class AttributeTests : AttributeTagsTestBase
    {
        [SetUp]
        public void SetUp()
        {
            ResetEffectHandleId();
        }

        [Test]
        public void CurrentValueReflectsBaseValueWhenUnmodified()
        {
            Attribute attribute = new(12f);
            Assert.AreEqual(12f, attribute.CurrentValue);
            Assert.AreEqual(12f, attribute.BaseValue);
        }

        [Test]
        public void ApplyAttributeModificationWithoutHandleMutatesBase()
        {
            Attribute attribute = new(10f);
            AttributeModification modification = new()
            {
                attribute = "health",
                action = ModificationAction.Addition,
                value = 5f,
            };

            attribute.ApplyAttributeModification(modification);
            Assert.AreEqual(15f, attribute.BaseValue);
            Assert.AreEqual(15f, attribute.CurrentValue);
        }

        [Test]
        public void ApplyAndRemoveAttributeModificationWithHandleRecalculates()
        {
            Attribute attribute = new(100f);
            AttributeModification addition = new()
            {
                attribute = "health",
                action = ModificationAction.Addition,
                value = 25f,
            };

            AttributeEffect effect = Track(ScriptableObject.CreateInstance<AttributeEffect>());
            effect.name = "Buff";
            EffectHandle handle = EffectHandle.CreateInstance(effect);

            attribute.ApplyAttributeModification(addition, handle);
            Assert.AreEqual(125f, attribute.CurrentValue);
            Assert.AreEqual(100f, attribute.BaseValue);

            bool removed = attribute.RemoveAttributeModification(handle);
            Assert.IsTrue(removed);
            Assert.AreEqual(100f, attribute.CurrentValue);
        }

        [Test]
        public void ApplyAttributeModificationWithMultiplicationExecutesInOrder()
        {
            Attribute attribute = new(10f);
            AttributeEffect effect = Track(ScriptableObject.CreateInstance<AttributeEffect>());
            effect.name = "Stacking";
            EffectHandle handle = EffectHandle.CreateInstance(effect);

            attribute.ApplyAttributeModification(
                new AttributeModification
                {
                    attribute = "health",
                    action = ModificationAction.Addition,
                    value = 5f,
                },
                handle
            );
            attribute.ApplyAttributeModification(
                new AttributeModification
                {
                    attribute = "health",
                    action = ModificationAction.Multiplication,
                    value = 2f,
                },
                handle
            );

            Assert.AreEqual(30f, attribute.CurrentValue);
        }

        [Test]
        public void AttributeEqualsSupportsFloatComparisons()
        {
            Attribute attribute = new(7.25f);
            Assert.IsTrue(attribute.Equals(7.25f));
            Assert.IsTrue(attribute.Equals((double)7.25f));
            Assert.IsFalse(attribute.Equals(7.5f));
            Assert.AreEqual("7.25", attribute.ToString());
        }

        [Test]
        public void ClearCacheForcesRecalculation()
        {
            Attribute attribute = new(10f);
            AttributeModification addition = new()
            {
                attribute = "health",
                action = ModificationAction.Addition,
                value = 5f,
            };

            attribute.ApplyAttributeModification(addition);
            Assert.AreEqual(15f, attribute.CurrentValue);

            attribute.ClearCache();
            Assert.AreEqual(15f, attribute.CurrentValue);
        }

        [Test]
        public void AddProducesHandleAndAppliesAddition()
        {
            Attribute attribute = new(10f);

            EffectHandle handle = attribute.Add(5f);
            Assert.AreEqual(15f, attribute.CurrentValue);
            Assert.AreEqual(1L, handle.id);

            bool removed = attribute.RemoveAttributeModification(handle);
            Assert.IsTrue(removed);
            Assert.AreEqual(10f, attribute.CurrentValue);
        }

        [Test]
        public void SubtractStacksAsNegativeAddition()
        {
            Attribute attribute = new(20f);

            EffectHandle addition = attribute.Add(5f);
            EffectHandle subtraction = attribute.Subtract(8f);
            EffectHandle multiplier = attribute.Multiply(2f);

            Assert.AreEqual(34f, attribute.CurrentValue);

            bool subtractionRemoved = attribute.RemoveAttributeModification(subtraction);
            Assert.IsTrue(subtractionRemoved);
            Assert.AreEqual(50f, attribute.CurrentValue);

            attribute.RemoveAttributeModification(addition);
            attribute.RemoveAttributeModification(multiplier);
        }

        [Test]
        public void DivideAppliesReciprocalMultiplication()
        {
            Attribute attribute = new(12f);

            EffectHandle addition = attribute.Add(6f);
            EffectHandle division = attribute.Divide(3f);

            Assert.AreEqual(6f, attribute.CurrentValue);

            bool divisionRemoved = attribute.RemoveAttributeModification(division);
            Assert.IsTrue(divisionRemoved);
            Assert.AreEqual(18f, attribute.CurrentValue);

            attribute.RemoveAttributeModification(addition);
        }

        [Test]
        public void DivideThrowsWhenValueIsZero()
        {
            Attribute attribute = new(10f);
            Assert.Throws<ArgumentException>(() => attribute.Divide(0f));
        }

        [Test]
        public void ArithmeticHelpersThrowWhenValueIsNotFinite()
        {
            Attribute attribute = new(5f);

            Assert.Throws<ArgumentException>(() => attribute.Add(float.NaN));
            Assert.Throws<ArgumentException>(() => attribute.Subtract(float.PositiveInfinity));
            Assert.Throws<ArgumentException>(() => attribute.Multiply(float.NegativeInfinity));
            Assert.Throws<ArgumentException>(() => attribute.Divide(float.PositiveInfinity));
        }
    }

    [TestFixture]
    public sealed class AttributeEffectTests : AttributeTagsTestBase
    {
        [Test]
        public void HumanReadableDescriptionFormatsAllModificationTypes()
        {
            AttributeEffect effect = Track(ScriptableObject.CreateInstance<AttributeEffect>());
            effect.name = "Composite";
            effect.modifications.Add(
                new AttributeModification
                {
                    attribute = "health",
                    action = ModificationAction.Addition,
                    value = 5f,
                }
            );
            effect.modifications.Add(
                new AttributeModification
                {
                    attribute = "attack_speed",
                    action = ModificationAction.Multiplication,
                    value = 1.5f,
                }
            );
            effect.modifications.Add(
                new AttributeModification
                {
                    attribute = "armor",
                    action = ModificationAction.Override,
                    value = 10f,
                }
            );

            string description = effect.HumanReadableDescription;
            Assert.AreEqual("+5 Health, +50% Attack Speed, 10 Armor", description);
        }

        [Test]
        public void HumanReadableDescriptionSkipsNeutralModifications()
        {
            AttributeEffect effect = Track(ScriptableObject.CreateInstance<AttributeEffect>());
            effect.modifications.Add(
                new AttributeModification
                {
                    attribute = "health",
                    action = ModificationAction.Addition,
                    value = 0f,
                }
            );
            effect.modifications.Add(
                new AttributeModification
                {
                    attribute = "speed",
                    action = ModificationAction.Multiplication,
                    value = 1f,
                }
            );

            Assert.IsEmpty(effect.HumanReadableDescription);
        }

        [Test]
        public void ToStringSerializesSummaryAndCollections()
        {
            AttributeEffect effect = Track(ScriptableObject.CreateInstance<AttributeEffect>());
            effect.name = "JsonEffect";
            effect.durationType = ModifierDurationType.Duration;
            effect.duration = 3.25f;
            effect.resetDurationOnReapplication = true;
            effect.modifications.Add(
                new AttributeModification
                {
                    attribute = "health",
                    action = ModificationAction.Addition,
                    value = 10f,
                }
            );
            effect.effectTags.Add("Buff");

            GameObject cosmeticHolder = Track(new GameObject("Glow", typeof(CosmeticEffectData)));
            CosmeticEffectData cosmeticData = cosmeticHolder.GetComponent<CosmeticEffectData>();
            effect.cosmeticEffects.Add(cosmeticData);

            using JsonDocument document = JsonDocument.Parse(effect.ToString());
            JsonElement root = document.RootElement;
            Assert.AreEqual(
                effect.HumanReadableDescription,
                root.GetProperty("Description").GetString()
            );
            Assert.AreEqual("Duration", root.GetProperty("durationType").GetString());
            Assert.AreEqual(3.25f, root.GetProperty("duration").GetSingle());
            Assert.AreEqual("Buff", root.GetProperty("tags")[0].GetString());
            Assert.AreEqual("Glow", root.GetProperty("CosmeticEffects")[0].GetString());
            Assert.AreEqual(1, root.GetProperty("modifications").GetArrayLength());
        }

        [Test]
        public void EqualsRequiresMatchingState()
        {
            AttributeEffect left = Track(ScriptableObject.CreateInstance<AttributeEffect>());
            AttributeEffect right = Track(ScriptableObject.CreateInstance<AttributeEffect>());
            left.name = right.name = "Stack";
            left.durationType = right.durationType = ModifierDurationType.Duration;
            left.duration = right.duration = 2f;
            left.resetDurationOnReapplication = right.resetDurationOnReapplication = false;

            AttributeModification modification = new()
            {
                attribute = "health",
                action = ModificationAction.Addition,
                value = 5f,
            };

            left.modifications.Add(modification);
            right.modifications.Add(modification);
            Assert.IsTrue(left.Equals(right));

            right.modifications[0] = new AttributeModification
            {
                attribute = "health",
                action = ModificationAction.Addition,
                value = 10f,
            };

            Assert.IsFalse(left.Equals(right));
        }
    }
}
