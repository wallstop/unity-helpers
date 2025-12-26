#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WButton
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes;

    /// <summary>
    /// Tests for WButtonEditorHelper API used to integrate WButton with custom editors.
    /// </summary>
    public sealed class WButtonEditorHelperTests : CommonTestBase
    {
        private sealed class TestComponent : ScriptableObject
        {
            public int invocationCount;

            [WButton("Test Button")]
            private void TestMethod()
            {
                invocationCount++;
            }
        }

        private sealed class TestEditor : Editor
        {
            // Simulates a custom editor
        }

        [Test]
        public void WButtonEditorHelper_CanBeCreated()
        {
            WButtonEditorHelper helper = new WButtonEditorHelper();
            Assert.That(helper, Is.Not.Null);
        }

        [Test]
        public void WButtonEditorHelper_DrawButtonsAtTop_WithValidEditor_DoesNotThrow()
        {
            TestComponent target = CreateScriptableObject<TestComponent>();
            TestEditor editor = Editor.CreateEditor(target) as TestEditor;
            WButtonEditorHelper helper = new WButtonEditorHelper();

            Assert.DoesNotThrow(() => helper.DrawButtonsAtTop(editor));

            DestroyImmediate(editor); // UNH-SUPPRESS: Editor is not a tracked Unity object
        }

        [Test]
        public void WButtonEditorHelper_DrawButtonsAtBottom_WithValidEditor_DoesNotThrow()
        {
            TestComponent target = CreateScriptableObject<TestComponent>();
            TestEditor editor = Editor.CreateEditor(target) as TestEditor;
            WButtonEditorHelper helper = new WButtonEditorHelper();

            Assert.DoesNotThrow(() => helper.DrawButtonsAtBottom(editor));

            DestroyImmediate(editor); // UNH-SUPPRESS: Editor is not a tracked Unity object
        }

        [Test]
        public void WButtonEditorHelper_ProcessInvocations_DoesNotThrow()
        {
            WButtonEditorHelper helper = new WButtonEditorHelper();

            Assert.DoesNotThrow(() => helper.ProcessInvocations());
        }

        [Test]
        public void WButtonEditorHelper_DrawButtonsAtBottomAndProcessInvocations_DoesNotThrow()
        {
            TestComponent target = CreateScriptableObject<TestComponent>();
            TestEditor editor = Editor.CreateEditor(target) as TestEditor;
            WButtonEditorHelper helper = new WButtonEditorHelper();

            Assert.DoesNotThrow(() => helper.DrawButtonsAtBottomAndProcessInvocations(editor));

            DestroyImmediate(editor); // UNH-SUPPRESS: Editor is not a tracked Unity object
        }

        [Test]
        public void WButtonEditorHelper_DrawAllButtonsAndProcessInvocations_DoesNotThrow()
        {
            TestComponent target = CreateScriptableObject<TestComponent>();
            TestEditor editor = Editor.CreateEditor(target) as TestEditor;
            WButtonEditorHelper helper = new WButtonEditorHelper();

            Assert.DoesNotThrow(() => helper.DrawAllButtonsAndProcessInvocations(editor));

            DestroyImmediate(editor); // UNH-SUPPRESS: Editor is not a tracked Unity object
        }

        [Test]
        public void WButtonEditorHelper_MultipleInstances_MaintainSeparateState()
        {
            // Verify that multiple helpers can coexist without interfering
            WButtonEditorHelper helper1 = new WButtonEditorHelper();
            WButtonEditorHelper helper2 = new WButtonEditorHelper();

            Assert.That(helper1, Is.Not.SameAs(helper2));
        }

        [Test]
        public void WButtonEditorHelper_CanBeReused_AcrossMultipleCalls()
        {
            TestComponent target = CreateScriptableObject<TestComponent>();
            TestEditor editor = Editor.CreateEditor(target) as TestEditor;
            WButtonEditorHelper helper = new WButtonEditorHelper();

            // Simulate multiple OnInspectorGUI calls
            Assert.DoesNotThrow(() =>
            {
                helper.DrawButtonsAtTop(editor);
                helper.DrawButtonsAtBottomAndProcessInvocations(editor);
            });

            Assert.DoesNotThrow(() =>
            {
                helper.DrawButtonsAtTop(editor);
                helper.DrawButtonsAtBottomAndProcessInvocations(editor);
            });

            DestroyImmediate(editor); // UNH-SUPPRESS: Editor is not a tracked Unity object
        }
    }
}
#endif
