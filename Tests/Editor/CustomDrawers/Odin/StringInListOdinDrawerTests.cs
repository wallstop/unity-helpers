namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.StringInList;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.StringInList;

    /// <summary>
    /// Tests for StringInListOdinDrawer ensuring StringInList attribute
    /// works correctly with Odin Inspector for SerializedMonoBehaviour
    /// and SerializedScriptableObject types.
    /// </summary>
    [TestFixture]
    public sealed class StringInListOdinDrawerTests : CommonTestBase
    {
        [Test]
        public void DrawerRegistrationForScriptableObjectIsCorrect()
        {
            OdinStringInListScriptableObjectTarget target =
                CreateScriptableObject<OdinStringInListScriptableObjectTarget>();
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

        [UnityTest]
        public IEnumerator DrawerRegistrationForMonoBehaviourIsCorrect()
        {
            while (EditorApplication.isCompiling)
            {
                yield return null;
            }

            OdinStringInListMonoBehaviourTarget target = NewGameObject("StringInListMB")
                .AddComponent<OdinStringInListMonoBehaviourTarget>();
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
            OdinStringInListScriptableObjectTarget target =
                CreateScriptableObject<OdinStringInListScriptableObjectTarget>();
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
            OdinStringInListMonoBehaviourTarget target = NewGameObject("StringInListMB")
                .AddComponent<OdinStringInListMonoBehaviourTarget>();
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
        public IEnumerator StringFieldWithStringOptionsWorks()
        {
            OdinStringInListStringFieldTarget target =
                CreateScriptableObject<OdinStringInListStringFieldTarget>();
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
                    $"OnInspectorGUI should not throw for string field with string options. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator IntFieldWithIndexSelectionWorks()
        {
            OdinStringInListIntFieldTarget target =
                CreateScriptableObject<OdinStringInListIntFieldTarget>();
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
                    $"OnInspectorGUI should not throw for int field with index selection. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator SerializableTypeFieldWorks()
        {
            OdinStringInListSerializableTypeTarget target =
                CreateScriptableObject<OdinStringInListSerializableTypeTarget>();
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
                    $"OnInspectorGUI should not throw for SerializableType field. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator EmptyListHandlingShowsHelpBox()
        {
            OdinStringInListEmptyOptionsTarget target =
                CreateScriptableObject<OdinStringInListEmptyOptionsTarget>();
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
        public IEnumerator LargeListPaginationWorks()
        {
            OdinStringInListLargeListTarget target =
                CreateScriptableObject<OdinStringInListLargeListTarget>();
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
                    $"OnInspectorGUI should not throw for large list with pagination. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator StaticMethodProviderWorks()
        {
            OdinStringInListStaticProviderTarget target =
                CreateScriptableObject<OdinStringInListStaticProviderTarget>();
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
        public IEnumerator InstanceMethodProviderWorks()
        {
            OdinStringInListInstanceProviderTarget target =
                CreateScriptableObject<OdinStringInListInstanceProviderTarget>();
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
            OdinStringInListScriptableObjectTarget target =
                CreateScriptableObject<OdinStringInListScriptableObjectTarget>();
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

        [UnityTest]
        public IEnumerator MultipleEditorsCanBeCreatedForSameTargetType()
        {
            OdinStringInListScriptableObjectTarget target1 =
                CreateScriptableObject<OdinStringInListScriptableObjectTarget>();
            OdinStringInListScriptableObjectTarget target2 =
                CreateScriptableObject<OdinStringInListScriptableObjectTarget>();

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
        public IEnumerator MultipleStringInListFieldsOnSameTargetWork()
        {
            OdinStringInListMultipleFieldsTarget target =
                CreateScriptableObject<OdinStringInListMultipleFieldsTarget>();
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
                    $"OnInspectorGUI should not throw with multiple StringInList fields. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator UnsupportedFieldTypeShowsErrorHelpBox()
        {
            OdinStringInListUnsupportedTypeTarget target =
                CreateScriptableObject<OdinStringInListUnsupportedTypeTarget>();
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
                    $"OnInspectorGUI should not throw for unsupported field type. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator StringFieldWithCurrentValueNotInListWorks()
        {
            OdinStringInListStringFieldTarget target =
                CreateScriptableObject<OdinStringInListStringFieldTarget>();
            target.selectedDifficulty = "NotInList";

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
                    $"OnInspectorGUI should not throw when current value is not in list. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator IntFieldWithOutOfRangeIndexWorks()
        {
            OdinStringInListIntFieldTarget target =
                CreateScriptableObject<OdinStringInListIntFieldTarget>();
            target.selectedIndex = 999;

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
                    $"OnInspectorGUI should not throw when index is out of range. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator IntFieldWithNegativeIndexWorks()
        {
            OdinStringInListIntFieldTarget target =
                CreateScriptableObject<OdinStringInListIntFieldTarget>();
            target.selectedIndex = -1;

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
                    $"OnInspectorGUI should not throw when index is negative. Exception: {caughtException}"
                );
                Assert.That(testCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [UnityTest]
        public IEnumerator NullSerializableTypeWorks()
        {
            OdinStringInListSerializableTypeTarget target =
                CreateScriptableObject<OdinStringInListSerializableTypeTarget>();
            target.selectedType = default;

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
                    $"OnInspectorGUI should not throw when SerializableType is default. Exception: {caughtException}"
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
