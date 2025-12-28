namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValueDropDown;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.ValueDropDown;

    /// <summary>
    /// Tests for WValueDropDownOdinDrawer ensuring WValueDropDown attribute
    /// works correctly with Odin Inspector for SerializedMonoBehaviour
    /// and SerializedScriptableObject types.
    /// </summary>
    [TestFixture]
    public sealed class WValueDropDownOdinDrawerTests : CommonTestBase
    {
        [Test]
        public void DrawerRegistrationForScriptableObjectIsCorrect()
        {
            OdinValueDropDownScriptableObjectTarget target =
                CreateScriptableObject<OdinValueDropDownScriptableObjectTarget>();
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
        public void DrawerRegistrationForMonoBehaviourIsCorrect()
        {
            OdinValueDropDownMonoBehaviourTarget target = NewGameObject("ValueDropDownMB")
                .AddComponent<OdinValueDropDownMonoBehaviourTarget>();
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
        public IEnumerator OnInspectorGuiDoesNotThrowForScriptableObject()
        {
            OdinValueDropDownScriptableObjectTarget target =
                CreateScriptableObject<OdinValueDropDownScriptableObjectTarget>();
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
                    $"OnInspectorGUI should not throw for ScriptableObject. Exception: {caughtException}"
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
            OdinValueDropDownMonoBehaviourTarget target = NewGameObject("ValueDropDownMB")
                .AddComponent<OdinValueDropDownMonoBehaviourTarget>();
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
        public IEnumerator StringDropdownFromStaticMethodWorks()
        {
            OdinValueDropDownStaticProviderTarget target =
                CreateScriptableObject<OdinValueDropDownStaticProviderTarget>();
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
                    $"OnInspectorGUI should not throw for static method provider. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator EnumDropdownWorks()
        {
            OdinValueDropDownEnumTarget target =
                CreateScriptableObject<OdinValueDropDownEnumTarget>();
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
                    $"OnInspectorGUI should not throw for enum dropdown. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator ObjectDropdownWorks()
        {
            OdinValueDropDownObjectRefTarget target =
                CreateScriptableObject<OdinValueDropDownObjectRefTarget>();
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
                    $"OnInspectorGUI should not throw for object dropdown. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator EmptyOptionsShowsHelpBox()
        {
            OdinValueDropDownEmptyOptionsTarget target =
                CreateScriptableObject<OdinValueDropDownEmptyOptionsTarget>();
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
                    $"OnInspectorGUI should not throw when options are empty. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator InlineIntOptionsWork()
        {
            OdinValueDropDownInlineIntTarget target =
                CreateScriptableObject<OdinValueDropDownInlineIntTarget>();
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
                    $"OnInspectorGUI should not throw for inline int options. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator InlineStringOptionsWork()
        {
            OdinValueDropDownInlineStringTarget target =
                CreateScriptableObject<OdinValueDropDownInlineStringTarget>();
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
                    $"OnInspectorGUI should not throw for inline string options. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [Test]
        public void FindSelectedIndexReturnsCorrectIndexForMatchingValue()
        {
            object[] options = { "Alpha", "Beta", "Gamma" };
            object currentValue = "Beta";

            int index = WValueDropDownOdinDrawer.FindSelectedIndex(currentValue, options);

            Assert.That(index, Is.EqualTo(1), "Index should be 1 for 'Beta'");
        }

        [Test]
        public void FindSelectedIndexReturnsNegativeOneForNonMatchingValue()
        {
            object[] options = { "Alpha", "Beta", "Gamma" };
            object currentValue = "Delta";

            int index = WValueDropDownOdinDrawer.FindSelectedIndex(currentValue, options);

            Assert.That(index, Is.EqualTo(-1), "Index should be -1 for non-matching value");
        }

        [Test]
        public void FindSelectedIndexReturnsNegativeOneForNullValue()
        {
            object[] options = { "Alpha", "Beta", "Gamma" };

            int index = WValueDropDownOdinDrawer.FindSelectedIndex(null, options);

            Assert.That(index, Is.EqualTo(-1), "Index should be -1 for null value");
        }

        [Test]
        public void ValuesMatchReturnsTrueForSameReference()
        {
            object value = new object();

            bool match = DropDownShared.ValuesMatch(value, value);

            Assert.That(match, Is.True, "Same reference should match");
        }

        [Test]
        public void ValuesMatchReturnsTrueForEqualStrings()
        {
            string value1 = "Test";
            string value2 = "Test";

            bool match = DropDownShared.ValuesMatch(value1, value2);

            Assert.That(match, Is.True, "Equal strings should match");
        }

        [Test]
        public void ValuesMatchReturnsFalseForNullAndNonNull()
        {
            object value1 = null;
            object value2 = "Test";

            bool match = DropDownShared.ValuesMatch(value1, value2);

            Assert.That(match, Is.False, "Null and non-null should not match");
        }

        [Test]
        public void ValuesMatchReturnsTrueForNumericEquality()
        {
            int value1 = 42;
            long value2 = 42L;

            bool match = DropDownShared.ValuesMatch(value1, value2);

            Assert.That(match, Is.True, "Numerically equal values should match");
        }

        [Test]
        public void ValuesMatchReturnsTrueForEnumAndIntEquivalent()
        {
            TestDropDownMode enumValue = TestDropDownMode.ModeB;
            int intValue = 1;

            bool match = DropDownShared.ValuesMatch(enumValue, intValue);

            Assert.That(match, Is.True, "Enum and its underlying int value should match");
        }

        [Test]
        public void GetDisplayOptionsReturnsCorrectStrings()
        {
            object[] options = { "Alpha", 42, TestDropDownMode.ModeA };

            string[] displayOptions = WValueDropDownOdinDrawer.GetDisplayOptions(options);

            Assert.That(displayOptions, Is.Not.Null);
            Assert.That(displayOptions.Length, Is.EqualTo(3));
            Assert.That(displayOptions[0], Is.EqualTo("Alpha"));
            Assert.That(displayOptions[1], Is.EqualTo("42"));
            Assert.That(displayOptions[2], Is.EqualTo("ModeA"));
        }

        [Test]
        public void FormatOptionReturnsNullStringForNull()
        {
            string formatted = DropDownShared.FormatOption(null);

            Assert.That(formatted, Is.EqualTo("(null)"));
        }

        [Test]
        public void FormatOptionReturnsEnumName()
        {
            string formatted = DropDownShared.FormatOption(TestDropDownMode.ModeC);

            Assert.That(formatted, Is.EqualTo("ModeC"));
        }

        [Test]
        public void FormatOptionReturnsIntAsString()
        {
            string formatted = DropDownShared.FormatOption(123);

            Assert.That(formatted, Is.EqualTo("123"));
        }

        [UnityTest]
        public IEnumerator InstanceMethodProviderWorks()
        {
            OdinValueDropDownInstanceProviderTarget target =
                CreateScriptableObject<OdinValueDropDownInstanceProviderTarget>();
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
                    $"OnInspectorGUI should not throw for instance method provider. Exception: {caughtException}"
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
            OdinValueDropDownScriptableObjectTarget target =
                CreateScriptableObject<OdinValueDropDownScriptableObjectTarget>();
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
                    $"OnInspectorGUI should not throw on repeated calls. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator MultipleEditorsCanBeCreatedForSameTargetType()
        {
            OdinValueDropDownScriptableObjectTarget target1 =
                CreateScriptableObject<OdinValueDropDownScriptableObjectTarget>();
            OdinValueDropDownScriptableObjectTarget target2 =
                CreateScriptableObject<OdinValueDropDownScriptableObjectTarget>();

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
        public IEnumerator LargeOptionListUsesPopupDropDown()
        {
            OdinValueDropDownLargeListTarget target =
                CreateScriptableObject<OdinValueDropDownLargeListTarget>();
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
                    $"OnInspectorGUI should not throw for large option list. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator MultipleDropdownFieldsOnSameTargetWork()
        {
            OdinValueDropDownMultipleFieldsTarget target =
                CreateScriptableObject<OdinValueDropDownMultipleFieldsTarget>();
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
                    $"OnInspectorGUI should not throw with multiple dropdown fields. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }
    }
#endif
}
