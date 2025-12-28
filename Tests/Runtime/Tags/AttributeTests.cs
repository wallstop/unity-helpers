// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Tags
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Tags;
    using Attribute = WallstopStudios.UnityHelpers.Tags.Attribute;

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
}
