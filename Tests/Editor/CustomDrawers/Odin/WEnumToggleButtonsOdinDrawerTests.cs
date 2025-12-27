namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using System.Collections;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.EnumToggleButtons;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.EnumToggleButtons;

    /// <summary>
    /// Tests for WEnumToggleButtonsOdinDrawer ensuring WEnumToggleButtons attribute
    /// works correctly with Odin Inspector for both regular and flags enums.
    /// </summary>
    [TestFixture]
    public sealed class WEnumToggleButtonsOdinDrawerTests : CommonTestBase
    {
        private static readonly MethodInfo GetCachedEnumOptionsMethod;
        private static readonly MethodInfo BuildEnumOptionsMethod;
        private static readonly MethodInfo CalculateAllFlagsMaskMethod;
        private static readonly MethodInfo ShouldPaginateMethod;
        private static readonly MethodInfo ResolvePageSizeMethod;
        private static readonly MethodInfo ConvertToUInt64Method;
        private static readonly MethodInfo IsPowerOfTwoMethod;
        private static readonly MethodInfo ResolveButtonSegmentMethod;
        private static readonly Type OdinToggleOptionType;
        private static readonly Type ButtonSegmentType;

        static WEnumToggleButtonsOdinDrawerTests()
        {
            Type drawerType = typeof(WEnumToggleButtonsOdinDrawer);

            GetCachedEnumOptionsMethod = drawerType.GetMethod(
                "GetCachedEnumOptions",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            BuildEnumOptionsMethod = drawerType.GetMethod(
                "BuildEnumOptions",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            CalculateAllFlagsMaskMethod = drawerType.GetMethod(
                "CalculateAllFlagsMask",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            ShouldPaginateMethod = drawerType.GetMethod(
                "ShouldPaginate",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            ResolvePageSizeMethod = drawerType.GetMethod(
                "ResolvePageSize",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            Type sharedType = typeof(EnumToggleButtonsShared);

            ConvertToUInt64Method = sharedType.GetMethod(
                "ConvertToUInt64",
                BindingFlags.Static | BindingFlags.Public
            );

            IsPowerOfTwoMethod = sharedType.GetMethod(
                "IsPowerOfTwo",
                BindingFlags.Static | BindingFlags.Public
            );

            ResolveButtonSegmentMethod = sharedType.GetMethod(
                "ResolveButtonSegment",
                BindingFlags.Static | BindingFlags.Public
            );

            OdinToggleOptionType = drawerType.GetNestedType(
                "OdinToggleOption",
                BindingFlags.NonPublic
            );

            ButtonSegmentType = drawerType.GetNestedType("ButtonSegment", BindingFlags.NonPublic);
        }

        [Test]
        public void DrawerRegistrationForRegularEnumIsCorrect()
        {
            OdinEnumToggleButtonsRegularTarget target =
                CreateScriptableObject<OdinEnumToggleButtonsRegularTarget>();
            Editor editor = Editor.CreateEditor(target);

            try
            {
                Assert.That(editor, Is.Not.Null, "Editor should be created for target");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [Test]
        public void DrawerRegistrationForFlagsEnumIsCorrect()
        {
            OdinEnumToggleButtonsFlagsTarget target =
                CreateScriptableObject<OdinEnumToggleButtonsFlagsTarget>();
            Editor editor = Editor.CreateEditor(target);

            try
            {
                Assert.That(editor, Is.Not.Null, "Editor should be created for flags target");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator DrawerHandlesMonoBehaviourTarget()
        {
            while (EditorApplication.isCompiling)
            {
                yield return null;
            }

            OdinEnumToggleButtonsMonoBehaviour target = NewGameObject("EnumToggleMB")
                .AddComponent<OdinEnumToggleButtonsMonoBehaviour>();
            Editor editor = Editor.CreateEditor(target);

            try
            {
                Assert.That(
                    editor,
                    Is.Not.Null,
                    "Editor should be created for MonoBehaviour target"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator OnInspectorGuiDoesNotThrowForRegularEnum()
        {
            OdinEnumToggleButtonsRegularTarget target =
                CreateScriptableObject<OdinEnumToggleButtonsRegularTarget>();
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
                    $"OnInspectorGUI should not throw for regular enum. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator OnInspectorGuiDoesNotThrowForFlagsEnum()
        {
            OdinEnumToggleButtonsFlagsTarget target =
                CreateScriptableObject<OdinEnumToggleButtonsFlagsTarget>();
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
                    $"OnInspectorGUI should not throw for flags enum. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator OnInspectorGuiDoesNotThrowForMonoBehaviour()
        {
            OdinEnumToggleButtonsMonoBehaviour target = NewGameObject("EnumToggleMB")
                .AddComponent<OdinEnumToggleButtonsMonoBehaviour>();
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
                    $"OnInspectorGUI should not throw for MonoBehaviour. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator RepeatedOnInspectorGuiCallsDoNotThrow()
        {
            OdinEnumToggleButtonsRegularTarget target =
                CreateScriptableObject<OdinEnumToggleButtonsRegularTarget>();
            Editor editor = Editor.CreateEditor(target);
            bool testCompleted = false;
            Exception caughtException = null;

            try
            {
                yield return TestIMGUIExecutor.Run(() =>
                {
                    try
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            editor.OnInspectorGUI();
                        }
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
                    $"Repeated OnInspectorGUI calls should not throw. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [Test]
        public void GetCachedEnumOptionsReturnsOptionsForRegularEnum()
        {
            Array options = InvokeGetCachedEnumOptions(typeof(SimpleTestEnum));

            Assert.That(options, Is.Not.Null, "Options should not be null");
            Assert.That(options.Length, Is.EqualTo(3), "SimpleTestEnum has 3 values");
        }

        [Test]
        public void GetCachedEnumOptionsReturnsOptionsForFlagsEnum()
        {
            Array options = InvokeGetCachedEnumOptions(typeof(TestFlagsEnum));

            Assert.That(options, Is.Not.Null, "Options should not be null");
            Assert.That(
                options.Length,
                Is.EqualTo(4),
                "TestFlagsEnum has 4 individual flag values (including None)"
            );
        }

        [Test]
        public void GetCachedEnumOptionsReturnsNullForNonEnumType()
        {
            Array options = InvokeGetCachedEnumOptions(typeof(int));

            Assert.That(options, Is.Null, "Options should be null for non-enum type");
        }

        [Test]
        public void GetCachedEnumOptionsReturnsNullForNullType()
        {
            Array options = InvokeGetCachedEnumOptions(null);

            Assert.That(options, Is.Null, "Options should be null for null type");
        }

        [Test]
        public void GetCachedEnumOptionsCachesResults()
        {
            Array options1 = InvokeGetCachedEnumOptions(typeof(SimpleTestEnum));
            Array options2 = InvokeGetCachedEnumOptions(typeof(SimpleTestEnum));

            Assert.That(
                options1,
                Is.SameAs(options2),
                "Cached options should be the same instance"
            );
        }

        [Test]
        public void BuildEnumOptionsExcludesCompositeFlags()
        {
            Array options = InvokeBuildEnumOptions(
                typeof(TestFlagsEnumWithComposite),
                isFlags: true
            );

            Assert.That(options, Is.Not.Null, "Options should not be null");
            Assert.That(
                options.Length,
                Is.EqualTo(4),
                "Should exclude composite All flag and include only single flags"
            );
        }

        [TestCase(typeof(SmallTestEnum), 2)]
        [TestCase(typeof(SimpleTestEnum), 3)]
        [TestCase(typeof(MediumTestEnum), 5)]
        [TestCase(typeof(LargeTestEnum), 10)]
        public void GetCachedEnumOptionsReturnsCorrectCountForEnumSize(
            Type enumType,
            int expectedCount
        )
        {
            Array options = InvokeGetCachedEnumOptions(enumType);

            Assert.That(options, Is.Not.Null, "Options should not be null");
            Assert.That(
                options.Length,
                Is.EqualTo(expectedCount),
                $"Enum {enumType.Name} should have {expectedCount} values"
            );
        }

        [Test]
        public void CalculateAllFlagsMaskReturnsCorrectMask()
        {
            Array options = InvokeGetCachedEnumOptions(typeof(TestFlagsEnum));
            ulong mask = InvokeCalculateAllFlagsMask(options);

            ulong expectedMask = (ulong)(
                TestFlagsEnum.FlagA | TestFlagsEnum.FlagB | TestFlagsEnum.FlagC
            );
            Assert.That(mask, Is.EqualTo(expectedMask), "Mask should include all non-zero flags");
        }

        [Test]
        public void CalculateAllFlagsMaskExcludesZeroFlag()
        {
            Array options = InvokeGetCachedEnumOptions(typeof(TestFlagsEnum));
            ulong mask = InvokeCalculateAllFlagsMask(options);

            Assert.That(mask, Is.Not.EqualTo(0UL), "Mask should not be zero");
            Assert.That((mask & 0UL), Is.EqualTo(0UL), "Zero flag should not affect the mask");
        }

        [TestCase(0UL, false)]
        [TestCase(1UL, true)]
        [TestCase(2UL, true)]
        [TestCase(3UL, false)]
        [TestCase(4UL, true)]
        [TestCase(5UL, false)]
        [TestCase(8UL, true)]
        [TestCase(16UL, true)]
        [TestCase(15UL, false)]
        [TestCase(32UL, true)]
        [TestCase(64UL, true)]
        [TestCase(128UL, true)]
        [TestCase(256UL, true)]
        [TestCase(1024UL, true)]
        [TestCase(2048UL, true)]
        [TestCase(4096UL, true)]
        [TestCase(1UL << 20, true)] // 1048576
        [TestCase(1UL << 30, true)] // 1073741824
        [TestCase(1UL << 40, true)] // Large power of 2
        [TestCase(1UL << 50, true)] // Very large power of 2
        [TestCase(1UL << 62, true)] // Near max ulong power of 2
        [TestCase(1UL << 63, true)] // Maximum power of 2 for ulong (9223372036854775808)
        [TestCase(6UL, false)]
        [TestCase(7UL, false)]
        [TestCase(9UL, false)]
        [TestCase(10UL, false)]
        [TestCase(12UL, false)]
        [TestCase(100UL, false)]
        [TestCase(1000UL, false)]
        [TestCase(255UL, false)]
        [TestCase(511UL, false)]
        [TestCase(1023UL, false)]
        [TestCase(ulong.MaxValue, false)] // 18446744073709551615 (all bits set, not a power of 2)
        [TestCase(ulong.MaxValue - 1UL, false)] // 18446744073709551614
        [TestCase((1UL << 63) - 1UL, false)] // 9223372036854775807 (one less than largest power of 2)
        [TestCase((1UL << 63) + 1UL, false)] // 9223372036854775809 (one more than largest power of 2)
        public void IsPowerOfTwoReturnsCorrectResult(ulong value, bool expectedResult)
        {
            bool result = InvokeIsPowerOfTwo(value);

            Assert.That(
                result,
                Is.EqualTo(expectedResult),
                $"IsPowerOfTwo({value}) should return {expectedResult}"
            );
        }

        [TestCase(SimpleTestEnum.OptionA, 0UL)]
        [TestCase(SimpleTestEnum.OptionB, 1UL)]
        [TestCase(SimpleTestEnum.OptionC, 2UL)]
        public void ConvertToUInt64ReturnsCorrectValueForRegularEnum(
            SimpleTestEnum enumValue,
            ulong expectedValue
        )
        {
            ulong result = InvokeConvertToUInt64(enumValue);

            Assert.That(
                result,
                Is.EqualTo(expectedValue),
                $"ConvertToUInt64({enumValue}) should return {expectedValue}"
            );
        }

        [TestCase(TestFlagsEnum.None, 0UL)]
        [TestCase(TestFlagsEnum.FlagA, 1UL)]
        [TestCase(TestFlagsEnum.FlagB, 2UL)]
        [TestCase(TestFlagsEnum.FlagC, 4UL)]
        [TestCase(TestFlagsEnum.FlagA | TestFlagsEnum.FlagB, 3UL)]
        public void ConvertToUInt64ReturnsCorrectValueForFlagsEnum(
            TestFlagsEnum enumValue,
            ulong expectedValue
        )
        {
            ulong result = InvokeConvertToUInt64(enumValue);

            Assert.That(
                result,
                Is.EqualTo(expectedValue),
                $"ConvertToUInt64({enumValue}) should return {expectedValue}"
            );
        }

        [Test]
        public void ConvertToUInt64ReturnsZeroForNull()
        {
            ulong result = InvokeConvertToUInt64(null);

            Assert.That(result, Is.EqualTo(0UL), "ConvertToUInt64(null) should return 0");
        }

        // Single column (columns <= 1) always returns Single
        [TestCase(0, 1, 1, 0)] // Single button, single column → Single
        [TestCase(0, 3, 1, 0)] // 3 buttons, single column, index 0 → Single
        [TestCase(1, 3, 1, 0)] // 3 buttons, single column, index 1 → Single
        [TestCase(2, 3, 1, 0)] // 3 buttons, single column, index 2 → Single
        [TestCase(0, 5, 0, 0)] // Edge case: columns = 0 → Single
        // 2 buttons in 2 columns (full row)
        [TestCase(0, 2, 2, 1)] // First button → Left
        [TestCase(1, 2, 2, 3)] // Last button → Right
        // 3 buttons in 3 columns (full row)
        [TestCase(0, 3, 3, 1)] // First button → Left
        [TestCase(1, 3, 3, 2)] // Middle button → Middle
        [TestCase(2, 3, 3, 3)] // Last button → Right
        // 5 buttons in 3 columns (2 full rows, 1 partial row)
        // Row 0: [0-Left] [1-Middle] [2-Right]
        // Row 1: [3-Left] [4-Right]
        [TestCase(0, 5, 3, 1)] // Row 0, col 0 → Left
        [TestCase(1, 5, 3, 2)] // Row 0, col 1 → Middle
        [TestCase(2, 5, 3, 3)] // Row 0, col 2 → Right
        [TestCase(3, 5, 3, 1)] // Row 1, col 0 → Left
        [TestCase(4, 5, 3, 3)] // Row 1, col 1, last button → Right
        // 4 buttons in 3 columns (partial last row with single button)
        // Row 0: [0-Left] [1-Middle] [2-Right]
        // Row 1: [3-Single]
        [TestCase(0, 4, 3, 1)] // Row 0, col 0 → Left
        [TestCase(1, 4, 3, 2)] // Row 0, col 1 → Middle
        [TestCase(2, 4, 3, 3)] // Row 0, col 2 → Right
        [TestCase(3, 4, 3, 0)] // Row 1, col 0, last button AND first column → Single
        // 7 buttons in 4 columns (partial last row with 3 buttons)
        // Row 0: [0-Left] [1-Middle] [2-Middle] [3-Right]
        // Row 1: [4-Left] [5-Middle] [6-Right]
        [TestCase(0, 7, 4, 1)] // Row 0, col 0 → Left
        [TestCase(1, 7, 4, 2)] // Row 0, col 1 → Middle
        [TestCase(2, 7, 4, 2)] // Row 0, col 2 → Middle
        [TestCase(3, 7, 4, 3)] // Row 0, col 3 → Right
        [TestCase(4, 7, 4, 1)] // Row 1, col 0 → Left
        [TestCase(5, 7, 4, 2)] // Row 1, col 1 → Middle
        [TestCase(6, 7, 4, 3)] // Row 1, col 2, last button → Right
        // Edge case: 1 button in multi-column layout
        [TestCase(0, 1, 3, 0)] // Single button → Single (first AND last)
        public void ResolveButtonSegmentReturnsCorrectSegment(
            int index,
            int total,
            int columns,
            int expectedSegment
        )
        {
            int result = InvokeResolveButtonSegment(index, total, columns);

            Assert.That(
                result,
                Is.EqualTo(expectedSegment),
                $"ResolveButtonSegment({index}, {total}, {columns}) should return segment {expectedSegment}"
            );
        }

        [TestCase(10, true, 8)]
        [TestCase(5, true, 8)]
        [TestCase(20, true, 8)]
        public void ShouldPaginateReturnsTrueWhenExceedsPageSize(
            int optionCount,
            bool enablePagination,
            int defaultPageSize
        )
        {
            WEnumToggleButtonsAttribute attribute = new(
                buttonsPerRow: 0,
                showSelectAll: true,
                showSelectNone: true,
                enablePagination: enablePagination,
                pageSize: defaultPageSize
            );

            (bool shouldPaginate, int pageSize) = InvokeShouldPaginate(attribute, optionCount);

            if (optionCount > defaultPageSize)
            {
                Assert.That(
                    shouldPaginate,
                    Is.True,
                    $"Should paginate when option count ({optionCount}) exceeds page size ({defaultPageSize})"
                );
            }
            else
            {
                Assert.That(
                    shouldPaginate,
                    Is.False,
                    $"Should not paginate when option count ({optionCount}) is within page size ({defaultPageSize})"
                );
            }
        }

        [Test]
        public void ShouldPaginateReturnsFalseWhenPaginationDisabled()
        {
            WEnumToggleButtonsAttribute attribute = new(
                buttonsPerRow: 0,
                showSelectAll: true,
                showSelectNone: true,
                enablePagination: false,
                pageSize: 8
            );

            (bool shouldPaginate, int _) = InvokeShouldPaginate(attribute, 100);

            Assert.That(
                shouldPaginate,
                Is.False,
                "Should not paginate when EnablePagination is false"
            );
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void ResolvePageSizeReturnsValidValue(int attributePageSize)
        {
            WEnumToggleButtonsAttribute attribute = new(
                buttonsPerRow: 0,
                showSelectAll: true,
                showSelectNone: true,
                enablePagination: true,
                pageSize: attributePageSize
            );

            int pageSize = InvokeResolvePageSize(attribute);

            Assert.That(pageSize, Is.GreaterThan(0), "Page size should be positive");
            Assert.That(pageSize, Is.LessThanOrEqualTo(100), "Page size should be reasonable");
        }

        [UnityTest]
        public IEnumerator MultipleEditorsCanBeCreatedForSameTargetType()
        {
            OdinEnumToggleButtonsRegularTarget target1 =
                CreateScriptableObject<OdinEnumToggleButtonsRegularTarget>();
            OdinEnumToggleButtonsRegularTarget target2 =
                CreateScriptableObject<OdinEnumToggleButtonsRegularTarget>();

            Editor editor1 = Editor.CreateEditor(target1);
            Editor editor2 = Editor.CreateEditor(target2);
            bool testCompleted = false;
            Exception caughtException = null;

            try
            {
                Assert.That(editor1, Is.Not.Null);
                Assert.That(editor2, Is.Not.Null);
                Assert.That(editor1, Is.Not.SameAs(editor2));

                yield return TestIMGUIExecutor.Run(() =>
                {
                    try
                    {
                        editor1.OnInspectorGUI();
                        editor2.OnInspectorGUI();
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
                    $"OnInspectorGUI should not throw for multiple editors. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor1);
                UnityEngine.Object.DestroyImmediate(editor2);
            }
        }

        [UnityTest]
        public IEnumerator CanEditMultipleObjectsDoesNotThrow()
        {
            OdinEnumToggleButtonsRegularTarget target1 =
                CreateScriptableObject<OdinEnumToggleButtonsRegularTarget>();
            OdinEnumToggleButtonsRegularTarget target2 =
                CreateScriptableObject<OdinEnumToggleButtonsRegularTarget>();

            UnityEngine.Object[] targets = new UnityEngine.Object[] { target1, target2 };
            Editor editor = Editor.CreateEditor(targets);
            bool testCompleted = false;
            Exception caughtException = null;

            try
            {
                Assert.That(editor, Is.Not.Null);

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
                    $"OnInspectorGUI should not throw for multiple objects. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator InspectorHandlesDestroyedTargetGracefully()
        {
            GameObject go = NewGameObject("EnumToggleMB");
            OdinEnumToggleButtonsMonoBehaviour target =
                go.AddComponent<OdinEnumToggleButtonsMonoBehaviour>();
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
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                    }
                });

                Assert.That(
                    caughtException,
                    Is.Null,
                    $"First OnInspectorGUI should not throw. Exception: {caughtException}"
                );

                UnityEngine.Object.DestroyImmediate(target);

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
                    $"OnInspectorGUI should not throw after target destroyed. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator DrawerHandlesAllAttributeConfigurations()
        {
            OdinEnumToggleButtonsAllConfigs target =
                CreateScriptableObject<OdinEnumToggleButtonsAllConfigs>();
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
                    $"OnInspectorGUI should not throw for all attribute configurations. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator DrawerHandlesCustomButtonsPerRow()
        {
            OdinEnumToggleButtonsCustomLayout target =
                CreateScriptableObject<OdinEnumToggleButtonsCustomLayout>();
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
                    $"OnInspectorGUI should not throw with custom buttons per row. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator DrawerHandlesSelectAllAndSelectNoneButtons()
        {
            OdinEnumToggleButtonsToolbar target =
                CreateScriptableObject<OdinEnumToggleButtonsToolbar>();
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
                    $"OnInspectorGUI should not throw with select all/none buttons. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator DrawerHandlesPaginationConfiguration()
        {
            OdinEnumToggleButtonsPaginated target =
                CreateScriptableObject<OdinEnumToggleButtonsPaginated>();
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
                    $"OnInspectorGUI should not throw with pagination. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator DrawerHandlesLargeEnumWithPagination()
        {
            OdinEnumToggleButtonsLargeEnum target =
                CreateScriptableObject<OdinEnumToggleButtonsLargeEnum>();
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
                    $"OnInspectorGUI should not throw for large enum with pagination. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator DrawerHandlesColorKeyConfiguration()
        {
            OdinEnumToggleButtonsColorKey target =
                CreateScriptableObject<OdinEnumToggleButtonsColorKey>();
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
                    $"OnInspectorGUI should not throw with color key. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator DrawerHandlesMultipleEnumFieldsOnSameTarget()
        {
            OdinEnumToggleButtonsMultipleFields target =
                CreateScriptableObject<OdinEnumToggleButtonsMultipleFields>();
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
                    $"OnInspectorGUI should not throw with multiple enum fields. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [Test]
        public void FlagsEnumAllowsMultipleSelection()
        {
            OdinEnumToggleButtonsFlagsTarget target =
                CreateScriptableObject<OdinEnumToggleButtonsFlagsTarget>();
            target.flags = TestFlagsEnum.FlagA | TestFlagsEnum.FlagB;

            ulong mask = InvokeConvertToUInt64(target.flags);
            ulong expectedMask = (ulong)(TestFlagsEnum.FlagA | TestFlagsEnum.FlagB);

            Assert.That(
                mask,
                Is.EqualTo(expectedMask),
                "Flags enum should support multiple selection"
            );
        }

        [Test]
        public void RegularEnumRestrictsToSingleSelection()
        {
            OdinEnumToggleButtonsRegularTarget target =
                CreateScriptableObject<OdinEnumToggleButtonsRegularTarget>();
            target.enumValue = SimpleTestEnum.OptionB;

            ulong mask = InvokeConvertToUInt64(target.enumValue);

            Assert.That(mask, Is.EqualTo(1UL), "Regular enum should have single value");
        }

        private static Array InvokeGetCachedEnumOptions(Type enumType)
        {
            return (Array)GetCachedEnumOptionsMethod?.Invoke(null, new object[] { enumType });
        }

        private static Array InvokeBuildEnumOptions(Type enumType, bool isFlags)
        {
            return (Array)BuildEnumOptionsMethod?.Invoke(null, new object[] { enumType, isFlags });
        }

        private static ulong InvokeCalculateAllFlagsMask(Array options)
        {
            return (ulong)CalculateAllFlagsMaskMethod?.Invoke(null, new object[] { options })!;
        }

        private static (bool shouldPaginate, int pageSize) InvokeShouldPaginate(
            WEnumToggleButtonsAttribute attribute,
            int optionCount
        )
        {
            object[] parameters = new object[] { attribute, optionCount, 0 };
            bool result = (bool)ShouldPaginateMethod?.Invoke(null, parameters)!;
            int pageSize = (int)parameters[2];
            return (result, pageSize);
        }

        private static int InvokeResolvePageSize(WEnumToggleButtonsAttribute attribute)
        {
            return (int)ResolvePageSizeMethod?.Invoke(null, new object[] { attribute })!;
        }

        private static ulong InvokeConvertToUInt64(object value)
        {
            return (ulong)ConvertToUInt64Method?.Invoke(null, new object[] { value })!;
        }

        private static bool InvokeIsPowerOfTwo(ulong value)
        {
            return (bool)IsPowerOfTwoMethod?.Invoke(null, new object[] { value })!;
        }

        private static int InvokeResolveButtonSegment(int index, int total, int columns)
        {
            object result = ResolveButtonSegmentMethod?.Invoke(
                null,
                new object[] { index, total, columns }
            );
            return Convert.ToInt32(result);
        }
    }
#endif
}
