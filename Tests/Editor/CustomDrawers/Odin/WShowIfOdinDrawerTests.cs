namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ShowIf;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;
    using WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.ShowIf;

    /// <summary>
    /// Tests for WShowIfOdinDrawer ensuring WShowIf attributes work correctly with Odin Inspector.
    /// </summary>
    [TestFixture]
    public sealed class WShowIfOdinDrawerTests : CommonTestBase
    {
        [Test]
        public void BoolConditionTrueShowsField()
        {
            OdinShowIfBoolTarget target = CreateScriptableObject<OdinShowIfBoolTarget>();
            target.boolCondition = true;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfBoolTarget.boolCondition),
                new WShowIfAttribute(nameof(OdinShowIfBoolTarget.boolCondition))
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.True, "Field should show when boolean condition is true");
        }

        [Test]
        public void BoolConditionFalseHidesField()
        {
            OdinShowIfBoolTarget target = CreateScriptableObject<OdinShowIfBoolTarget>();
            target.boolCondition = false;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfBoolTarget.boolCondition),
                new WShowIfAttribute(nameof(OdinShowIfBoolTarget.boolCondition))
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.False, "Field should hide when boolean condition is false");
        }

        [TestCase(true, true, false)]
        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        [TestCase(false, false, false)]
        public void BoolConditionWithInverseFlipsBehavior(
            bool conditionValue,
            bool inverse,
            bool expectedShow
        )
        {
            OdinShowIfBoolTarget target = CreateScriptableObject<OdinShowIfBoolTarget>();
            target.boolCondition = conditionValue;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfBoolTarget.boolCondition),
                new WShowIfAttribute(nameof(OdinShowIfBoolTarget.boolCondition), inverse)
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.EqualTo(expectedShow),
                $"With condition={conditionValue} and inverse={inverse}, expected show={expectedShow}"
            );
        }

        [TestCase(true, WShowIfComparison.NotEqual, false)]
        [TestCase(false, WShowIfComparison.NotEqual, true)]
        [TestCase(true, WShowIfComparison.Equal, true)]
        [TestCase(false, WShowIfComparison.Equal, false)]
        public void BoolConditionWithComparisonModeWorks(
            bool conditionValue,
            WShowIfComparison comparison,
            bool expectedShow
        )
        {
            OdinShowIfBoolTarget target = CreateScriptableObject<OdinShowIfBoolTarget>();
            target.boolCondition = conditionValue;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfBoolTarget.boolCondition),
                new WShowIfAttribute(nameof(OdinShowIfBoolTarget.boolCondition), comparison)
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.EqualTo(expectedShow),
                $"With condition={conditionValue} and comparison={comparison}, expected show={expectedShow}"
            );
        }

        [Test]
        public void EnumEqualComparisonShowsWhenMatches()
        {
            OdinShowIfEnumTarget target = CreateScriptableObject<OdinShowIfEnumTarget>();
            target.testMode = TestModeEnum.ModeA;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfEnumTarget.testMode),
                new WShowIfAttribute(
                    nameof(OdinShowIfEnumTarget.testMode),
                    expectedValues: new object[] { TestModeEnum.ModeA }
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.True, "Field should show when enum matches expected value");
        }

        [Test]
        public void EnumEqualComparisonHidesWhenDoesNotMatch()
        {
            OdinShowIfEnumTarget target = CreateScriptableObject<OdinShowIfEnumTarget>();
            target.testMode = TestModeEnum.ModeB;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfEnumTarget.testMode),
                new WShowIfAttribute(
                    nameof(OdinShowIfEnumTarget.testMode),
                    expectedValues: new object[] { TestModeEnum.ModeA }
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.False,
                "Field should hide when enum does not match expected value"
            );
        }

        [Test]
        public void EnumNotEqualComparisonShowsWhenDoesNotMatch()
        {
            OdinShowIfEnumTarget target = CreateScriptableObject<OdinShowIfEnumTarget>();
            target.testMode = TestModeEnum.ModeB;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfEnumTarget.testMode),
                new WShowIfAttribute(
                    nameof(OdinShowIfEnumTarget.testMode),
                    WShowIfComparison.NotEqual,
                    TestModeEnum.ModeA
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Field should show when enum does not equal expected value with NotEqual comparison"
            );
        }

        [Test]
        public void EnumNotEqualComparisonHidesWhenMatches()
        {
            OdinShowIfEnumTarget target = CreateScriptableObject<OdinShowIfEnumTarget>();
            target.testMode = TestModeEnum.ModeA;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfEnumTarget.testMode),
                new WShowIfAttribute(
                    nameof(OdinShowIfEnumTarget.testMode),
                    WShowIfComparison.NotEqual,
                    TestModeEnum.ModeA
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.False,
                "Field should hide when enum equals expected value with NotEqual comparison"
            );
        }

        [TestCase(TestModeEnum.ModeA, true)]
        [TestCase(TestModeEnum.ModeB, true)]
        [TestCase(TestModeEnum.ModeC, false)]
        public void EnumMultipleExpectedValuesMatchesAny(TestModeEnum value, bool expectedShow)
        {
            OdinShowIfEnumTarget target = CreateScriptableObject<OdinShowIfEnumTarget>();
            target.testMode = value;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfEnumTarget.testMode),
                new WShowIfAttribute(
                    nameof(OdinShowIfEnumTarget.testMode),
                    expectedValues: new object[] { TestModeEnum.ModeA, TestModeEnum.ModeB }
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.EqualTo(expectedShow),
                $"With enum={value}, expected show={expectedShow}"
            );
        }

        [Test]
        public void FlagsEnumShowsWhenExactFlagMatches()
        {
            OdinShowIfFlagsTarget target = CreateScriptableObject<OdinShowIfFlagsTarget>();
            target.flags = TestFlagsEnum.FlagA;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfFlagsTarget.flags),
                new WShowIfAttribute(
                    nameof(OdinShowIfFlagsTarget.flags),
                    expectedValues: new object[] { TestFlagsEnum.FlagA }
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.True, "Field should show when exact flag matches");
        }

        [Test]
        public void FlagsEnumShowsWhenCombinedFlagsContainExpected()
        {
            OdinShowIfFlagsTarget target = CreateScriptableObject<OdinShowIfFlagsTarget>();
            target.flags = TestFlagsEnum.FlagA | TestFlagsEnum.FlagB;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfFlagsTarget.flags),
                new WShowIfAttribute(
                    nameof(OdinShowIfFlagsTarget.flags),
                    expectedValues: new object[] { TestFlagsEnum.FlagA }
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Field should show when combined flags contain expected flag"
            );
        }

        [Test]
        public void FlagsEnumHidesWhenNoFlagsSet()
        {
            OdinShowIfFlagsTarget target = CreateScriptableObject<OdinShowIfFlagsTarget>();
            target.flags = TestFlagsEnum.None;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfFlagsTarget.flags),
                new WShowIfAttribute(
                    nameof(OdinShowIfFlagsTarget.flags),
                    expectedValues: new object[] { TestFlagsEnum.FlagA }
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.False, "Field should hide when no flags are set");
        }

        [Test]
        public void FlagsEnumHidesWhenDifferentFlagSet()
        {
            OdinShowIfFlagsTarget target = CreateScriptableObject<OdinShowIfFlagsTarget>();
            target.flags = TestFlagsEnum.FlagB;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfFlagsTarget.flags),
                new WShowIfAttribute(
                    nameof(OdinShowIfFlagsTarget.flags),
                    expectedValues: new object[] { TestFlagsEnum.FlagA }
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.False, "Field should hide when different flag is set");
        }

        [Test]
        public void FlagsEnumShowsWhenAllFlagsSetAndExpectedIsSubset()
        {
            OdinShowIfFlagsTarget target = CreateScriptableObject<OdinShowIfFlagsTarget>();
            target.flags = (TestFlagsEnum)(-1);

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfFlagsTarget.flags),
                new WShowIfAttribute(
                    nameof(OdinShowIfFlagsTarget.flags),
                    expectedValues: new object[] { TestFlagsEnum.FlagA | TestFlagsEnum.FlagB }
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Field should show when all flags set and expected is subset"
            );
        }

        [TestCase(5, WShowIfComparison.GreaterThan, 3, true)]
        [TestCase(3, WShowIfComparison.GreaterThan, 3, false)]
        [TestCase(2, WShowIfComparison.GreaterThan, 3, false)]
        [TestCase(5, WShowIfComparison.GreaterThanOrEqual, 3, true)]
        [TestCase(3, WShowIfComparison.GreaterThanOrEqual, 3, true)]
        [TestCase(2, WShowIfComparison.GreaterThanOrEqual, 3, false)]
        [TestCase(2, WShowIfComparison.LessThan, 3, true)]
        [TestCase(3, WShowIfComparison.LessThan, 3, false)]
        [TestCase(5, WShowIfComparison.LessThan, 3, false)]
        [TestCase(2, WShowIfComparison.LessThanOrEqual, 3, true)]
        [TestCase(3, WShowIfComparison.LessThanOrEqual, 3, true)]
        [TestCase(5, WShowIfComparison.LessThanOrEqual, 3, false)]
        public void IntComparisonModesWorkCorrectly(
            int value,
            WShowIfComparison comparison,
            int threshold,
            bool expectedShow
        )
        {
            OdinShowIfIntTarget target = CreateScriptableObject<OdinShowIfIntTarget>();
            target.intCondition = value;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfIntTarget.intCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfIntTarget.intCondition),
                    comparison,
                    threshold
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.EqualTo(expectedShow),
                $"With value={value}, comparison={comparison}, threshold={threshold}, expected show={expectedShow}"
            );
        }

        [Test]
        public void IntEqualComparisonShowsWhenMatches()
        {
            OdinShowIfIntTarget target = CreateScriptableObject<OdinShowIfIntTarget>();
            target.intCondition = 42;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfIntTarget.intCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfIntTarget.intCondition),
                    expectedValues: new object[] { 42 }
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.True, "Field should show when int matches expected value");
        }

        [Test]
        public void IntNotEqualComparisonShowsWhenDoesNotMatch()
        {
            OdinShowIfIntTarget target = CreateScriptableObject<OdinShowIfIntTarget>();
            target.intCondition = 10;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfIntTarget.intCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfIntTarget.intCondition),
                    WShowIfComparison.NotEqual,
                    42
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Field should show when int does not equal expected with NotEqual comparison"
            );
        }

        [Test]
        public void IsNullComparisonShowsWhenReferenceIsNull()
        {
            OdinShowIfReferenceTarget target = CreateScriptableObject<OdinShowIfReferenceTarget>();
            target.objectReference = null;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfReferenceTarget.objectReference),
                new WShowIfAttribute(
                    nameof(OdinShowIfReferenceTarget.objectReference),
                    WShowIfComparison.IsNull
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Field should show when reference is null with IsNull comparison"
            );
        }

        [Test]
        public void IsNullComparisonHidesWhenReferenceIsNotNull()
        {
            OdinShowIfReferenceTarget target = CreateScriptableObject<OdinShowIfReferenceTarget>();
            target.objectReference = Track(new GameObject("TestObject"));

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfReferenceTarget.objectReference),
                new WShowIfAttribute(
                    nameof(OdinShowIfReferenceTarget.objectReference),
                    WShowIfComparison.IsNull
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.False,
                "Field should hide when reference is not null with IsNull comparison"
            );
        }

        [Test]
        public void IsNotNullComparisonShowsWhenReferenceIsNotNull()
        {
            OdinShowIfReferenceTarget target = CreateScriptableObject<OdinShowIfReferenceTarget>();
            target.objectReference = Track(new GameObject("TestObject"));

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfReferenceTarget.objectReference),
                new WShowIfAttribute(
                    nameof(OdinShowIfReferenceTarget.objectReference),
                    WShowIfComparison.IsNotNull
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Field should show when reference is not null with IsNotNull comparison"
            );
        }

        [Test]
        public void IsNotNullComparisonHidesWhenReferenceIsNull()
        {
            OdinShowIfReferenceTarget target = CreateScriptableObject<OdinShowIfReferenceTarget>();
            target.objectReference = null;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfReferenceTarget.objectReference),
                new WShowIfAttribute(
                    nameof(OdinShowIfReferenceTarget.objectReference),
                    WShowIfComparison.IsNotNull
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.False,
                "Field should hide when reference is null with IsNotNull comparison"
            );
        }

        [Test]
        public void IsNullComparisonHandlesDestroyedUnityObject()
        {
            OdinShowIfReferenceTarget target = CreateScriptableObject<OdinShowIfReferenceTarget>();
            GameObject go = new GameObject("ToBeDestroyed");
            target.objectReference = go;
            UnityEngine.Object.DestroyImmediate(go);

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfReferenceTarget.objectReference),
                new WShowIfAttribute(
                    nameof(OdinShowIfReferenceTarget.objectReference),
                    WShowIfComparison.IsNull
                )
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Field should show when destroyed Unity object checked with IsNull"
            );
        }

        [Test]
        public void IsNullOrEmptyShowsWhenStringIsEmpty()
        {
            OdinShowIfStringTarget target = CreateScriptableObject<OdinShowIfStringTarget>();
            target.stringCondition = string.Empty;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfStringTarget.stringCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfStringTarget.stringCondition),
                    WShowIfComparison.IsNullOrEmpty
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.True, "Field should show when string is empty");
        }

        [Test]
        public void IsNullOrEmptyShowsWhenStringIsNull()
        {
            OdinShowIfStringTarget target = CreateScriptableObject<OdinShowIfStringTarget>();
            target.stringCondition = null;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfStringTarget.stringCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfStringTarget.stringCondition),
                    WShowIfComparison.IsNullOrEmpty
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.True, "Field should show when string is null");
        }

        [Test]
        public void IsNullOrEmptyHidesWhenStringHasContent()
        {
            OdinShowIfStringTarget target = CreateScriptableObject<OdinShowIfStringTarget>();
            target.stringCondition = "content";

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfStringTarget.stringCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfStringTarget.stringCondition),
                    WShowIfComparison.IsNullOrEmpty
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.False, "Field should hide when string has content");
        }

        [Test]
        public void IsNotNullOrEmptyShowsWhenStringHasContent()
        {
            OdinShowIfStringTarget target = CreateScriptableObject<OdinShowIfStringTarget>();
            target.stringCondition = "content";

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfStringTarget.stringCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfStringTarget.stringCondition),
                    WShowIfComparison.IsNotNullOrEmpty
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.True, "Field should show when string has content");
        }

        [Test]
        public void IsNotNullOrEmptyHidesWhenStringIsEmpty()
        {
            OdinShowIfStringTarget target = CreateScriptableObject<OdinShowIfStringTarget>();
            target.stringCondition = string.Empty;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfStringTarget.stringCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfStringTarget.stringCondition),
                    WShowIfComparison.IsNotNullOrEmpty
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.False, "Field should hide when string is empty");
        }

        [Test]
        public void IsNullOrEmptyShowsWhenCollectionIsEmpty()
        {
            OdinShowIfCollectionTarget target =
                CreateScriptableObject<OdinShowIfCollectionTarget>();
            target.listCondition = new List<int>();

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfCollectionTarget.listCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfCollectionTarget.listCondition),
                    WShowIfComparison.IsNullOrEmpty
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.True, "Field should show when collection is empty");
        }

        [Test]
        public void IsNullOrEmptyHidesWhenCollectionHasItems()
        {
            OdinShowIfCollectionTarget target =
                CreateScriptableObject<OdinShowIfCollectionTarget>();
            target.listCondition = new List<int> { 1, 2, 3 };

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfCollectionTarget.listCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfCollectionTarget.listCondition),
                    WShowIfComparison.IsNullOrEmpty
                )
            );

            Assert.That(success, Is.True);
            Assert.That(shouldShow, Is.False, "Field should hide when collection has items");
        }

        [Test]
        public void PropertyConditionEvaluatesCorrectly()
        {
            OdinShowIfPropertyTarget target = CreateScriptableObject<OdinShowIfPropertyTarget>();
            target.boolField = true;
            target.intField = 5;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfPropertyTarget.ComputedProperty),
                new WShowIfAttribute(nameof(OdinShowIfPropertyTarget.ComputedProperty))
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Field should show when computed property returns true"
            );
        }

        [Test]
        public void PropertyConditionHidesWhenFalse()
        {
            OdinShowIfPropertyTarget target = CreateScriptableObject<OdinShowIfPropertyTarget>();
            target.boolField = true;
            target.intField = 0;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfPropertyTarget.ComputedProperty),
                new WShowIfAttribute(nameof(OdinShowIfPropertyTarget.ComputedProperty))
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.False,
                "Field should hide when computed property returns false"
            );
        }

        [Test]
        public void MethodConditionEvaluatesCorrectly()
        {
            OdinShowIfMethodTarget target = CreateScriptableObject<OdinShowIfMethodTarget>();
            target.value = 10;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfMethodTarget.IsPositive),
                new WShowIfAttribute(nameof(OdinShowIfMethodTarget.IsPositive))
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Field should show when method condition returns true"
            );
        }

        [Test]
        public void MethodConditionHidesWhenFalse()
        {
            OdinShowIfMethodTarget target = CreateScriptableObject<OdinShowIfMethodTarget>();
            target.value = -5;

            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfMethodTarget.IsPositive),
                new WShowIfAttribute(nameof(OdinShowIfMethodTarget.IsPositive))
            );

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.False,
                "Field should hide when method condition returns false"
            );
        }

        [UnityTest]
        public IEnumerator OdinSerializedMonoBehaviourWithWShowIfDoesNotThrow()
        {
            OdinShowIfMonoBehaviour target = NewGameObject("ShowIfTest")
                .AddComponent<OdinShowIfMonoBehaviour>();
            Editor editor = Editor.CreateEditor(target);
            bool testCompleted = false;
            Exception caughtException = null;

            try
            {
                yield return TestIMGUIExecutor.Run(() =>
                {
                    try
                    {
                        editor.OnInspectorGUI();
                        testCompleted = true;
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                    }
                });

                Assert.That(
                    caughtException,
                    Is.Null,
                    $"OnInspectorGUI should not throw. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator OdinSerializedScriptableObjectWithWShowIfDoesNotThrow()
        {
            OdinShowIfScriptableObject target =
                CreateScriptableObject<OdinShowIfScriptableObject>();
            Editor editor = Editor.CreateEditor(target);
            bool testCompleted = false;
            Exception caughtException = null;

            try
            {
                yield return TestIMGUIExecutor.Run(() =>
                {
                    try
                    {
                        editor.OnInspectorGUI();
                        testCompleted = true;
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                    }
                });

                Assert.That(
                    caughtException,
                    Is.Null,
                    $"OnInspectorGUI should not throw. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator MultipleWShowIfFieldsInSameObjectDoNotThrow()
        {
            OdinShowIfMultipleFields target = CreateScriptableObject<OdinShowIfMultipleFields>();
            Editor editor = Editor.CreateEditor(target);
            bool testCompleted = false;
            Exception caughtException = null;

            try
            {
                target.boolCondition = true;
                yield return TestIMGUIExecutor.Run(() =>
                {
                    try
                    {
                        editor.OnInspectorGUI();
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                    }
                });
                Assert.That(
                    caughtException,
                    Is.Null,
                    $"OnInspectorGUI should not throw (boolCondition=true). Exception: {caughtException}"
                );

                target.boolCondition = false;
                target.intCondition = 5;
                caughtException = null;
                yield return TestIMGUIExecutor.Run(() =>
                {
                    try
                    {
                        editor.OnInspectorGUI();
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                    }
                });
                Assert.That(
                    caughtException,
                    Is.Null,
                    $"OnInspectorGUI should not throw (intCondition=5). Exception: {caughtException}"
                );

                target.enumCondition = TestModeEnum.ModeA;
                caughtException = null;
                yield return TestIMGUIExecutor.Run(() =>
                {
                    try
                    {
                        editor.OnInspectorGUI();
                        testCompleted = true;
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                    }
                });
                Assert.That(
                    caughtException,
                    Is.Null,
                    $"OnInspectorGUI should not throw (enumCondition=ModeA). Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator RepeatedInspectorGuiCallsWithChangingConditionsDoNotThrow()
        {
            OdinShowIfScriptableObject target =
                CreateScriptableObject<OdinShowIfScriptableObject>();
            Editor editor = Editor.CreateEditor(target);
            bool testCompleted = false;
            Exception caughtException = null;

            try
            {
                for (int i = 0; i < 10; i++)
                {
                    target.showDependent = i % 2 == 0;
                    int iteration = i;
                    caughtException = null;
                    yield return TestIMGUIExecutor.Run(() =>
                    {
                        try
                        {
                            editor.OnInspectorGUI();
                            if (iteration == 9)
                            {
                                testCompleted = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            caughtException = ex;
                        }
                    });
                    Assert.That(
                        caughtException,
                        Is.Null,
                        $"OnInspectorGUI should not throw (iteration {i}). Exception: {caughtException}"
                    );
                }
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [Test]
        public void NonExistentConditionFieldDoesNotThrow()
        {
            OdinShowIfBoolTarget target = CreateScriptableObject<OdinShowIfBoolTarget>();

            object conditionValue = WShowIfOdinDrawer.GetConditionValueForTest(
                target,
                "nonExistentField"
            );

            Assert.That(conditionValue, Is.Null, "Non-existent field should return null");
        }

        [Test]
        public void NullParentValueHandledGracefully()
        {
            object conditionValue = WShowIfOdinDrawer.GetConditionValueForTest(null, "someField");

            Assert.That(conditionValue, Is.Null, "Null parent should return null condition value");
        }

        [Test]
        public void EmptyConditionFieldNameHandledGracefully()
        {
            OdinShowIfBoolTarget target = CreateScriptableObject<OdinShowIfBoolTarget>();

            object conditionValue = WShowIfOdinDrawer.GetConditionValueForTest(
                target,
                string.Empty
            );

            Assert.That(conditionValue, Is.Null, "Empty condition field name should return null");
        }

        [Test]
        public void UnknownComparisonFallsBackToEqual()
        {
            OdinShowIfIntTarget target = CreateScriptableObject<OdinShowIfIntTarget>();
            target.intCondition = 5;

#pragma warning disable CS0618
            (bool success, bool shouldShow) = EvaluateCondition(
                target,
                nameof(OdinShowIfIntTarget.intCondition),
                new WShowIfAttribute(
                    nameof(OdinShowIfIntTarget.intCondition),
                    WShowIfComparison.Unknown,
                    5
                )
            );
#pragma warning restore CS0618

            Assert.That(success, Is.True);
            Assert.That(
                shouldShow,
                Is.True,
                "Unknown comparison should fall back to Equal and match"
            );
        }

        private (bool success, bool shouldShow) EvaluateCondition(
            ScriptableObject target,
            string conditionField,
            WShowIfAttribute attribute
        )
        {
            object conditionValue = WShowIfOdinDrawer.GetConditionValueForTest(
                target,
                attribute.conditionField
            );

            bool success = ShowIfConditionEvaluator.TryEvaluateCondition(
                conditionValue,
                attribute,
                out bool shouldShow
            );

            return (success, shouldShow);
        }
    }
#endif
}
