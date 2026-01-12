// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Unity object lifecycle management in tests - CommonTestBase pattern

namespace WallstopStudios.UnityHelpers.Examples
{
    using System;
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Tests.Core;

    // All tests creating Unity objects MUST inherit from CommonTestBase
    [TestFixture]
    public sealed class UnityObjectLifecycleTests : CommonTestBase
    {
        // CORRECT: Objects tracked for automatic cleanup
        [Test]
        public void DrawerCreatesEditorSuccessfully()
        {
            MyTarget target = CreateScriptableObject<MyTarget>();
            Editor editor = Track(Editor.CreateEditor(target));

            Assert.IsTrue(editor != null);
        }

        // CORRECT: Testing destroyed object handling with UNH-SUPPRESS
        [Test]
        public void InspectorHandlesDestroyedTargetGracefully()
        {
            MyTarget target = CreateScriptableObject<MyTarget>();
            Editor editor = Track(Editor.CreateEditor(target));

            editor.OnInspectorGUI();

            UnityEngine.Object.DestroyImmediate(target); // UNH-SUPPRESS: Test verifies behavior after target destroyed
            _trackedObjects.Remove(target); // Remove from tracking to prevent double-destroy in teardown

            Assert.DoesNotThrow(() => editor.OnInspectorGUI());
        }

        // CORRECT: Async test pattern with Track()
        [UnityTest]
        public IEnumerator OnInspectorGuiDoesNotThrowForTarget()
        {
            MyTarget target = CreateScriptableObject<MyTarget>();
            Editor editor = Track(Editor.CreateEditor(target));
            bool completed = false;
            Exception caught = null;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    editor.OnInspectorGUI();
                    completed = true;
                }
                catch (Exception ex)
                {
                    caught = ex;
                }
            });

            Assert.IsTrue(caught == null);
            Assert.IsTrue(completed);
        }
    }

    // Track Methods Reference:
    // | Method                        | Use For                                              |
    // | CreateScriptableObject<T>()   | Creating test ScriptableObject targets               |
    // | NewGameObject(name)           | Creating test GameObject instances                   |
    // | Track(obj)                    | Any Unity object (Editor, Material, Texture2D)       |
    // | TrackDisposable(disposable)   | IDisposable resources                                |
    // | TrackAssetPath(path)          | Created asset files that need deletion               |
    // | _trackedObjects.Remove(obj)   | Remove from tracking after intentional destroy       |

    // Dummy classes for example
    internal sealed class MyTarget : ScriptableObject { }

    internal static class TestIMGUIExecutor
    {
        public static IEnumerator Run(Action action)
        {
            action?.Invoke();
            yield return null;
        }
    }
}
