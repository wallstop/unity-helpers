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
    using WallstopStudios.UnityHelpers.Tests.Utils;
    using Object = UnityEngine.Object;

    [TestFixture]
    public sealed class WShowIfPropertyDrawerTests : CommonTestBase
    {
        [Test]
        public void BoolConditionHidesFieldWhenFalse()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nameof(TestContainer.boolCondition))
            );

            container.boolCondition = false;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.boolCondition = true;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void NotEqualComparisonWithoutExpectedInvertsBoolean()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty dependentProperty = serializedObject.FindProperty(
                nameof(TestContainer.boolDependent)
            );
            Assert.NotNull(dependentProperty);

            WShowIfAttribute attribute = new(
                nameof(TestContainer.boolCondition),
                WShowIfComparison.NotEqual
            );
            Assert.AreEqual(WShowIfComparison.NotEqual, attribute.comparison);
            Assert.IsEmpty(attribute.expectedValues);

            WShowIfPropertyDrawer drawer = CreateDrawer(attribute);

            container.boolCondition = true;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.boolCondition = false;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void EnumConditionMatchesExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.durationDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.durationType = ModifierDurationType.Duration;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.durationDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void EnumConditionHonorsInverseFlag()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.durationType = ModifierDurationType.Infinite;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void FloatConditionMatchesExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.floatCondition = 3.5f;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void DoubleConditionMatchesEquivalentIntExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.doubleDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.doubleCondition = 7d;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.doubleDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void IntConditionMatchesExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 42;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void StringConditionMatchesExpectedValues()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.stringCondition = "alpha";
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void StringGreaterThanComparisonUsesOrdinal()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.stringGreaterThanDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.stringCondition = "zeta";
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.stringGreaterThanDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void NotEqualComparisonExcludesExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 7;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void GreaterThanComparisonUsesExpectedThreshold()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 11;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            SerializedProperty conditionProperty = serializedObject.FindProperty(
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
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 10;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void LessThanOrEqualComparisonMatchesBoundary()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.floatCondition = 3.5f;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void LessThanComparisonUsesExpectedThreshold()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.floatCondition = 1f;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.floatDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void IsNullComparisonHandlesUnityObjectReferences()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.referenceDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.referenceCondition),
                    WShowIfComparison.IsNull
                )
            );

            container.referenceCondition = ScriptableObject.CreateInstance<DummyScriptable>();
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.referenceDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            Object.DestroyImmediate(container.referenceCondition);
            container.referenceCondition = null;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.referenceDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void IsNullOrEmptyComparisonHandlesStringsAndCollections()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty stringDependent = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.stringNullOrEmptyDependent)
            );
            SerializedProperty listDependent = serializedObject.FindProperty(
                nameof(TestContainer.listDependent)
            );
            Assert.NotNull(listDependent);

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
                ref serializedObject,
                container,
                nameof(TestContainer.stringNullOrEmptyDependent)
            );
            listDependent = serializedObject.FindProperty(nameof(TestContainer.listDependent));
            Assert.NotNull(listDependent);
            Assert.False(InvokeShouldShow(stringDrawer, stringDependent));
            Assert.False(InvokeShouldShow(listDrawer, listDependent));

            container.stringCondition = string.Empty;
            container.listCondition.Clear();
            stringDependent = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.stringNullOrEmptyDependent)
            );
            listDependent = serializedObject.FindProperty(nameof(TestContainer.listDependent));
            Assert.NotNull(listDependent);
            Assert.True(InvokeShouldShow(stringDrawer, stringDependent));
            Assert.True(InvokeShouldShow(listDrawer, listDependent));
        }

        [Test]
        public void IsNotNullOrEmptyComparisonEvaluatesStrings()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.stringCondition = "content";
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.stringDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void PropertyConditionEvaluatesThroughReflection()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nameof(TestContainer.ComputedProperty))
            );

            container.boolCondition = true;
            container.intCondition = 0;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 1;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.boolDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void MethodConditionEvaluatesThroughReflection()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nameof(TestContainer.HasPositiveDuration))
            );

            container.durationType = ModifierDurationType.Instant;
            container.durationAmount = 0;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.durationType = ModifierDurationType.Duration;
            container.durationAmount = 2;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.inverseDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void NestedPathConditionIsResolved()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.nestedDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.nested.child.value = 5;
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.nestedDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
        }

        [Test]
        public void UnknownComparisonFallsBackToEquality()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.intDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.intCondition = 5;
            dependentProperty = RefreshProperty(
                ref serializedObject,
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

            SerializedObject serializedObject = null;
            SerializedProperty dependentProperty = RefreshProperty(
                ref serializedObject,
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
                ref serializedObject,
                container,
                nameof(TestContainer.genericComparableDependent)
            );
            Assert.False(InvokeShouldShow(drawer, dependentProperty));

            container.genericComparableHolder.value = new TestContainer.GenericComparable(10);
            dependentProperty = RefreshProperty(
                ref serializedObject,
                container,
                nameof(TestContainer.genericComparableDependent)
            );
            Assert.True(InvokeShouldShow(drawer, dependentProperty));
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
            ref SerializedObject serializedObject,
            ScriptableObject owner,
            string propertyName
        )
        {
            serializedObject = new SerializedObject(owner);
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.NotNull(property);
            return property;
        }

        private sealed class TestContainer : ScriptableObject
        {
            public bool boolCondition;

            [WShowIf(nameof(boolCondition))]
            public int boolDependent;

            public ModifierDurationType durationType = ModifierDurationType.Instant;

            public int durationAmount;

            [WShowIf(
                nameof(durationType),
                expectedValues: new object[] { ModifierDurationType.Duration }
            )]
            public int durationDependent;

            [WShowIf(
                nameof(durationType),
                inverse: true,
                expectedValues: new object[] { ModifierDurationType.Instant }
            )]
            public int inverseDependent;

            public float floatCondition;

            [WShowIf(nameof(floatCondition), expectedValues: new object[] { 3.5f })]
            public int floatDependent;

            public double doubleCondition;

            [WShowIf(nameof(doubleCondition), expectedValues: new object[] { 7 })]
            public int doubleDependent;

            public int intCondition;

            [WShowIf(nameof(intCondition), expectedValues: new object[] { 42 })]
            public int intDependent;

            public string stringCondition;

            [WShowIf(nameof(stringCondition), expectedValues: new object[] { "alpha", "beta" })]
            public int stringDependent;

            public int stringGreaterThanDependent;

            [WShowIf(nameof(stringCondition), WShowIfComparison.IsNullOrEmpty)]
            public int stringNullOrEmptyDependent;

            public List<int> listCondition = new();

            [WShowIf(nameof(listCondition), WShowIfComparison.IsNullOrEmpty)]
            public int listDependent;

            public ScriptableObject referenceCondition;

            [WShowIf(nameof(referenceCondition), WShowIfComparison.IsNull)]
            public int referenceDependent;

            public NestedData nested = new();

            [WShowIf(
                nameof(nested) + "." + nameof(NestedData.child) + "." + nameof(NestedChild.value),
                WShowIfComparison.GreaterThanOrEqual,
                5
            )]
            public int nestedDependent;

            public GenericComparableHolder genericComparableHolder = new();

            public int genericComparableDependent;

            public bool ComputedProperty => boolCondition && intCondition > 0;

            public bool HasPositiveDuration()
            {
                return durationType == ModifierDurationType.Duration && durationAmount > 0;
            }

            [Serializable]
            public sealed class NestedData
            {
                public NestedChild child = new();
            }

            [Serializable]
            public sealed class NestedChild
            {
                public int value;
            }

            [Serializable]
            public sealed class GenericComparableHolder
            {
                public GenericComparable value = new();
            }

            [Serializable]
            public sealed class GenericComparable : IComparable<GenericComparable>
            {
                public int magnitude;

                public GenericComparable() { }

                public GenericComparable(int magnitude)
                {
                    this.magnitude = magnitude;
                }

                public int CompareTo(GenericComparable other)
                {
                    if (other == null)
                    {
                        return 1;
                    }

                    return magnitude.CompareTo(other.magnitude);
                }
            }
        }

        private sealed class DummyScriptable : ScriptableObject { }
    }
}
