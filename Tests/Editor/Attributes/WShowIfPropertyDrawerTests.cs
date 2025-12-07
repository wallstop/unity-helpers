namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes;
    using Object = UnityEngine.Object;

    [TestFixture]
    public sealed class WShowIfPropertyDrawerTests : CommonTestBase
    {
        [Test]
        public void BoolConditionHidesFieldWhenFalse()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nameof(TestContainer.boolCondition))
            );

            container.boolCondition = false;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.False(
                InvokeShouldShow(drawer, dependentProperty),
                $"Expected ShouldShow=false when boolCondition=false. Container.boolCondition={container.boolCondition}, SerializedObject hash={serializedObject.Current?.GetHashCode()}"
            );

            container.boolCondition = true;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.True(
                InvokeShouldShow(drawer, dependentProperty),
                $"Expected ShouldShow=true when boolCondition=true. Container.boolCondition={container.boolCondition}, SerializedObject hash={serializedObject.Current?.GetHashCode()}"
            );
        }

        [Test]
        public void NotEqualComparisonWithoutExpectedInvertsBoolean()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();

            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );

            WShowIfAttribute attribute = new(
                nameof(TestContainer.boolCondition),
                WShowIfComparison.NotEqual
            );
            Assert.AreEqual(WShowIfComparison.NotEqual, attribute.comparison);
            Assert.IsEmpty(attribute.expectedValues);

            WShowIfPropertyDrawer drawer = CreateDrawer(attribute);

            container.boolCondition = true;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.False(
                InvokeShouldShow(drawer, dependentProperty),
                $"Expected ShouldShow=false with NotEqual comparison when boolCondition=true. Container.boolCondition={container.boolCondition}, SerializedObject hash={serializedObject.Current?.GetHashCode()}"
            );

            container.boolCondition = false;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.True(
                InvokeShouldShow(drawer, dependentProperty),
                $"Expected ShouldShow=true with NotEqual comparison when boolCondition=false. Container.boolCondition={container.boolCondition}, SerializedObject hash={serializedObject.Current?.GetHashCode()}"
            );
        }

        [Test]
        public void EnumConditionMatchesExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.durationDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.durationType),
                    expectedValues: new object[] { ModifierDurationType.Duration }
                )
            );

            container.durationType = ModifierDurationType.Instant;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.durationDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.durationType = ModifierDurationType.Duration;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.durationDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void EnumConditionHonorsInverseFlag()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.durationType),
                    inverse: true,
                    expectedValues: new object[] { ModifierDurationType.Instant }
                )
            );

            container.durationType = ModifierDurationType.Instant;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.durationType = ModifierDurationType.Infinite;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void FloatConditionMatchesExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.floatCondition),
                    expectedValues: new object[] { 3.5f }
                )
            );

            container.floatCondition = 0f;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.floatCondition = 3.5f;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void DoubleConditionMatchesEquivalentIntExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.doubleDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.doubleCondition),
                    expectedValues: new object[] { 7 }
                )
            );

            container.doubleCondition = 2.5d;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.doubleDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.doubleCondition = 7d;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.doubleDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void IntConditionMatchesExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.intCondition),
                    expectedValues: new object[] { 42 }
                )
            );

            container.intCondition = 7;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 42;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void StringConditionMatchesExpectedValues()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.stringCondition),
                    expectedValues: new object[] { "alpha", "beta" }
                )
            );

            container.stringCondition = "gamma";
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.stringCondition = "alpha";
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void StringGreaterThanComparisonUsesOrdinal()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringGreaterThanDependent)
            );

            WShowIfAttribute attribute = new(
                nameof(TestContainer.stringCondition),
                WShowIfComparison.GreaterThan,
                "m"
            );
            WShowIfPropertyDrawer drawer = CreateDrawer(attribute);

            container.stringCondition = "alpha";
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringGreaterThanDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.stringCondition = "zeta";
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringGreaterThanDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void NotEqualComparisonExcludesExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );

            WShowIfAttribute attribute = new(
                nameof(TestContainer.intCondition),
                WShowIfComparison.NotEqual,
                42
            );
            Assert.AreEqual(WShowIfComparison.NotEqual, attribute.comparison);
            CollectionAssert.AreEqual(new object[] { 42 }, attribute.expectedValues);

            WShowIfPropertyDrawer drawer = CreateDrawer(attribute);

            container.intCondition = 42;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 7;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void GreaterThanComparisonUsesExpectedThreshold()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );

            WShowIfAttribute attribute = new(
                nameof(TestContainer.intCondition),
                WShowIfComparison.GreaterThan,
                10
            );
            Assert.AreEqual(WShowIfComparison.GreaterThan, attribute.comparison);
            CollectionAssert.AreEqual(new object[] { 10 }, attribute.expectedValues);

            WShowIfPropertyDrawer drawer = CreateDrawer(attribute);

            container.intCondition = 5;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 11;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            SerializedProperty conditionProperty = serializedObject.Current.FindProperty(
                nameof(TestContainer.intCondition)
            );
            Assert.NotNull(conditionProperty);
            Assert.AreEqual(11, conditionProperty.intValue);
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void GreaterThanOrEqualComparisonMatchesBoundary()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );

            WShowIfAttribute attribute = new(
                nameof(TestContainer.intCondition),
                WShowIfComparison.GreaterThanOrEqual,
                10
            );
            Assert.AreEqual(WShowIfComparison.GreaterThanOrEqual, attribute.comparison);
            CollectionAssert.AreEqual(new object[] { 10 }, attribute.expectedValues);

            WShowIfPropertyDrawer drawer = CreateDrawer(attribute);

            container.intCondition = 9;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 10;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void LessThanOrEqualComparisonMatchesBoundary()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.floatCondition),
                    WShowIfComparison.LessThanOrEqual,
                    3.5f
                )
            );

            container.floatCondition = 4f;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.floatCondition = 3.5f;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void LessThanComparisonUsesExpectedThreshold()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );

            WShowIfAttribute attribute = new(
                nameof(TestContainer.floatCondition),
                WShowIfComparison.LessThan,
                3.5f
            );
            Assert.AreEqual(WShowIfComparison.LessThan, attribute.comparison);
            CollectionAssert.AreEqual(new object[] { 3.5f }, attribute.expectedValues);

            WShowIfPropertyDrawer drawer = CreateDrawer(attribute);

            container.floatCondition = 3.5f;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.floatCondition = 1f;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void IsNullComparisonHandlesUnityObjectReferences()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.referenceDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.referenceCondition),
                    WShowIfComparison.IsNull
                )
            );

            container.referenceCondition = Track(
                ScriptableObject.CreateInstance<DummyScriptable>()
            );
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.referenceDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            Object.DestroyImmediate(container.referenceCondition);
            container.referenceCondition = null;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.referenceDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void IsNullOrEmptyComparisonHandlesStringsAndCollections()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty stringDependent = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringNullOrEmptyDependent)
            );
            SerializedProperty listDependent = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.listDependent)
            );

            WShowIfPropertyDrawer stringDrawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.stringCondition),
                    WShowIfComparison.IsNullOrEmpty
                )
            );

            WShowIfPropertyDrawer listDrawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.listCondition),
                    WShowIfComparison.IsNullOrEmpty
                )
            );

            container.stringCondition = "value";
            container.listCondition.Clear();
            container.listCondition.Add(1);
            stringDependent = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringNullOrEmptyDependent)
            );
            listDependent = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.listDependent)
            );
            Assert.False(InvokeShouldShow(stringDrawer, stringDependent));
            Assert.False(InvokeShouldShow(listDrawer, listDependent));

            container.stringCondition = string.Empty;
            container.listCondition.Clear();
            stringDependent = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringNullOrEmptyDependent)
            );
            listDependent = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.listDependent)
            );
            Assert.True(InvokeShouldShow(stringDrawer, stringDependent));
            Assert.True(InvokeShouldShow(listDrawer, listDependent));
        }

        [Test]
        public void IsNullOrEmptyComparisonTreatsDestroyedUnityObjectsAsNull()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            container.objectCondition = Track(new GameObject("ShowIfObjectCondition"));
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.objectNullOrEmptyDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.objectCondition),
                    WShowIfComparison.IsNullOrEmpty
                )
            );

            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            Object.DestroyImmediate(container.objectCondition);
            container.objectCondition = null;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.objectNullOrEmptyDependent)
            );

            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void IsNotNullOrEmptyComparisonEvaluatesStrings()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.stringCondition),
                    WShowIfComparison.IsNotNullOrEmpty
                )
            );

            container.stringCondition = string.Empty;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.stringCondition = "content";
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void PropertyConditionEvaluatesThroughReflection()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nameof(TestContainer.ComputedProperty))
            );

            container.boolCondition = true;
            container.intCondition = 0;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 1;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void MethodConditionEvaluatesThroughReflection()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nameof(TestContainer.HasPositiveDuration))
            );

            container.durationType = ModifierDurationType.Instant;
            container.durationAmount = 0;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.durationType = ModifierDurationType.Duration;
            container.durationAmount = 2;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void NestedPathConditionIsResolved()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.nestedDependent)
            );

            string nestedPath =
                nameof(TestContainer.nested)
                + "."
                + nameof(TestContainer.NestedData.child)
                + "."
                + nameof(TestContainer.NestedChild.value);
            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nestedPath, WShowIfComparison.GreaterThanOrEqual, 5)
            );

            container.nested.child.value = 2;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.nestedDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.nested.child.value = 5;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.nestedDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void UnknownComparisonFallsBackToEquality()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );

#pragma warning disable CS0618
            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.intCondition),
                    WShowIfComparison.Unknown,
                    5
                )
            );
#pragma warning restore CS0618

            container.intCondition = 2;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 5;
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void GenericComparableComparisonUsesTypedInterface()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            container.genericComparableHolder.value = new TestContainer.GenericComparable(2);

            using SerializedObjectTracker serializedObject = new();
            SerializedProperty dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.genericComparableDependent)
            );

            WShowIfAttribute attribute = new(
                nameof(TestContainer.genericComparableHolder)
                    + "."
                    + nameof(TestContainer.GenericComparableHolder.value),
                WShowIfComparison.GreaterThan,
                new TestContainer.GenericComparable(5)
            );
            WShowIfPropertyDrawer drawer = CreateDrawer(attribute);

            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.genericComparableDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.genericComparableHolder.value = new TestContainer.GenericComparable(10);
            dependentProperty = RefreshProperty(
                serializedObject,
                container,
                nameof(TestContainer.genericComparableDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void MultipleSerializedObjectsForSameTargetEachReflectCorrectState()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nameof(TestContainer.boolCondition))
            );

            container.boolCondition = false;

            using SerializedObject serializedObject1 = new(container);
            serializedObject1.Update();
            SerializedProperty property1 = serializedObject1.FindProperty(
                nameof(TestContainer.boolDependent)
            );
            Assert.NotNull(property1, "Could not find property in first SerializedObject");
            bool result1 = InvokeShouldShow(drawer, property1);
            Assert.False(
                result1,
                $"First evaluation should return false when boolCondition=false. Got {result1}"
            );

            container.boolCondition = true;

            using SerializedObject serializedObject2 = new(container);
            serializedObject2.Update();
            SerializedProperty property2 = serializedObject2.FindProperty(
                nameof(TestContainer.boolDependent)
            );
            Assert.NotNull(property2, "Could not find property in second SerializedObject");
            bool result2 = InvokeShouldShow(drawer, property2);
            Assert.True(
                result2,
                $"Second evaluation with fresh SerializedObject should return true when boolCondition=true. Got {result2}. SerializedObject1 hash={serializedObject1.GetHashCode()}, SerializedObject2 hash={serializedObject2.GetHashCode()}"
            );
        }

        [Test]
        public void RapidConditionChangesWithNewSerializedObjectsReturnCorrectResults()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nameof(TestContainer.boolCondition))
            );

            for (int iteration = 0; iteration < 5; iteration++)
            {
                bool expectedShow = iteration % 2 == 1;
                container.boolCondition = expectedShow;

                using SerializedObject serializedObject = new(container);
                serializedObject.Update();
                SerializedProperty property = serializedObject.FindProperty(
                    nameof(TestContainer.boolDependent)
                );
                Assert.NotNull(property, $"Could not find property in iteration {iteration}");

                bool actualShow = InvokeShouldShow(drawer, property);
                Assert.AreEqual(
                    expectedShow,
                    actualShow,
                    $"Iteration {iteration}: Expected ShouldShow={expectedShow} when boolCondition={container.boolCondition}, but got {actualShow}"
                );
            }
        }

        private static WShowIfPropertyDrawer CreateDrawer(WShowIfAttribute attribute)
        {
            WShowIfPropertyDrawer drawer = new();
            drawer.InitializeForTesting(attribute);
            return drawer;
        }

        private static bool InvokeShouldShow(
            WShowIfPropertyDrawer drawer,
            SerializedProperty property
        )
        {
            return drawer.ShouldShow(property);
        }

        private static SerializedProperty RefreshProperty(
            SerializedObjectTracker tracker,
            ScriptableObject owner,
            string propertyName
        )
        {
            return tracker.Refresh(owner, propertyName);
        }

        private sealed class SerializedObjectTracker : IDisposable
        {
            private readonly List<SerializedObject> _trackedObjects = new();
            private SerializedObject _current;

            public SerializedObject Current => _current;

            public SerializedProperty Refresh(ScriptableObject owner, string propertyName)
            {
                SerializedObject serializedObject = new(owner);
                serializedObject.Update();
                _trackedObjects.Add(serializedObject);
                _current = serializedObject;
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                Assert.NotNull(property);
                return property;
            }

            public void Dispose()
            {
                if (_trackedObjects.Count == 0)
                {
                    return;
                }

                foreach (SerializedObject serializedObject in _trackedObjects)
                {
                    serializedObject?.Dispose();
                }

                _trackedObjects.Clear();
                _current = null;
            }
        }
    }
}
