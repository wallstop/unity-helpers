namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes;

    /// <summary>
    /// Tests for the WGroup property context functionality in GroupGUIWidthUtility.
    /// Verifies that IsInsideWGroupPropertyDraw properly tracks when properties are
    /// being drawn inside a WGroup context, and that SerializableDictionary/SerializableSet
    /// correctly apply the WGroupFoldoutAlignmentOffset based on this context.
    /// </summary>
    [TestFixture]
    public sealed class WGroupPropertyContextTests : CommonTestBase
    {
        private int _originalIndentLevel;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _originalIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GroupGUIWidthUtility.ResetForTests();
            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            SerializableDictionaryPropertyDrawer.ClearMainFoldoutAnimCacheForTests();
            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
        }

        [TearDown]
        public override void TearDown()
        {
            EditorGUI.indentLevel = _originalIndentLevel;
            GroupGUIWidthUtility.ResetForTests();
            base.TearDown();
        }

        [Test]
        public void IsInsideWGroupPropertyDrawDefaultValueIsFalse()
        {
            GroupGUIWidthUtility.ResetForTests();

            bool isInside = GroupGUIWidthUtility.IsInsideWGroupPropertyDraw;

            Assert.That(
                isInside,
                Is.False,
                "IsInsideWGroupPropertyDraw should default to false after reset."
            );
        }

        [Test]
        public void IsInsideWGroupPropertyDrawReturnsTrueAfterPushWGroupPropertyContext()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                bool isInside = GroupGUIWidthUtility.IsInsideWGroupPropertyDraw;

                Assert.That(
                    isInside,
                    Is.True,
                    "IsInsideWGroupPropertyDraw should return true after PushWGroupPropertyContext()."
                );
            }
        }

        [Test]
        public void IsInsideWGroupPropertyDrawReturnsToPreviousValueAfterScopeDisposed()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "IsInsideWGroupPropertyDraw should be false before scope."
            );

            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "IsInsideWGroupPropertyDraw should be true inside scope."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "IsInsideWGroupPropertyDraw should return to false after scope is disposed."
            );
        }

        [Test]
        public void NestedWGroupPropertyContextScopesWorkCorrectly()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should be false initially."
            );

            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should be true in first scope."
                );

                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                        Is.True,
                        "Should be true in nested scope."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should remain true after nested scope disposes (previous value was true)."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should return to false after all scopes disposed."
            );
        }

        [Test]
        public void ResetForTestsResetsIsInsideWGroupPropertyDrawFlag()
        {
            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Flag should be true inside scope."
                );

                GroupGUIWidthUtility.ResetForTests();

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.False,
                    "ResetForTests should reset IsInsideWGroupPropertyDraw to false."
                );
            }

            // After disposal, the previous value was captured before reset,
            // so it would restore to false anyway in this case
            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should remain false after scope disposal post-reset."
            );
        }

        [Test]
        public void WGroupPropertyContextScopeRestoresPreviousValueOnException()
        {
            GroupGUIWidthUtility.ResetForTests();

            try
            {
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                        Is.True,
                        "Should be true inside scope before exception."
                    );
                    throw new InvalidOperationException("Test exception");
                }
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should restore to false after scope disposed due to exception."
            );
        }

        [Test]
        public void WGroupPropertyContextScopeIsIdempotentOnMultipleDispose()
        {
            GroupGUIWidthUtility.ResetForTests();

            IDisposable scope = GroupGUIWidthUtility.PushWGroupPropertyContext();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.True,
                "Should be true after push."
            );

            scope.Dispose();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should be false after first dispose."
            );

            // Dispose again - should not change anything
            scope.Dispose();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should remain false after second dispose (idempotent)."
            );
        }

        [Test]
        public void ExitWGroupThemingClearsPaletteAndContextFlags()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Set up a WGroup context with palette and property context
            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should be inside WGroup property context before exit."
                );

                // Exit the theming
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                        Is.False,
                        "Should NOT be inside WGroup property context while in ExitWGroupTheming scope."
                    );

                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroup,
                        Is.False,
                        "Should NOT be inside WGroup while in ExitWGroupTheming scope."
                    );
                }

                // After exit scope ends, context should be restored
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should be restored to inside WGroup property context after ExitWGroupTheming scope ends."
                );
            }
        }

        [Test]
        public void ExitWGroupThemingRestoresContextOnException()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                try
                {
                    using (GroupGUIWidthUtility.ExitWGroupTheming())
                    {
                        Assert.That(
                            GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                            Is.False,
                            "Should NOT be inside WGroup property context in exit scope."
                        );
                        throw new InvalidOperationException("Test exception");
                    }
                }
                catch (InvalidOperationException)
                {
                    // Expected
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should restore to inside WGroup property context after exception."
                );
            }
        }

        [Test]
        public void ExitWGroupThemingResetsGUIContentColorToWhite()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Set a non-white content color
            Color originalColor = GUI.contentColor;
            GUI.contentColor = Color.red;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        GUI.contentColor,
                        Is.EqualTo(Color.white),
                        "GUI.contentColor should be reset to white inside ExitWGroupTheming scope."
                    );
                }
            }
            finally
            {
                GUI.contentColor = originalColor;
            }
        }

        [Test]
        public void ExitWGroupThemingResetsGUIColorToWhite()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color originalColor = GUI.color;
            GUI.color = Color.blue;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        GUI.color,
                        Is.EqualTo(Color.white),
                        "GUI.color should be reset to white inside ExitWGroupTheming scope."
                    );
                }
            }
            finally
            {
                GUI.color = originalColor;
            }
        }

        [Test]
        public void ExitWGroupThemingResetsGUIBackgroundColorToWhite()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        GUI.backgroundColor,
                        Is.EqualTo(Color.white),
                        "GUI.backgroundColor should be reset to white inside ExitWGroupTheming scope."
                    );
                }
            }
            finally
            {
                GUI.backgroundColor = originalColor;
            }
        }

        [Test]
        public void ExitWGroupThemingRestoresGUIContentColorOnDispose()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color testColor = new(0.5f, 0.3f, 0.8f, 1f);
            Color originalColor = GUI.contentColor;
            GUI.contentColor = testColor;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    // Inside scope, should be white
                    Assert.That(GUI.contentColor, Is.EqualTo(Color.white));
                }

                // After scope, should be restored to test color
                Assert.That(
                    GUI.contentColor.r,
                    Is.EqualTo(testColor.r).Within(0.001f),
                    "GUI.contentColor should be restored after ExitWGroupTheming scope ends."
                );
                Assert.That(GUI.contentColor.g, Is.EqualTo(testColor.g).Within(0.001f));
                Assert.That(GUI.contentColor.b, Is.EqualTo(testColor.b).Within(0.001f));
            }
            finally
            {
                GUI.contentColor = originalColor;
            }
        }

        [Test]
        public void ExitWGroupThemingRestoresGUIColorOnDispose()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color testColor = new(0.2f, 0.7f, 0.4f, 1f);
            Color originalColor = GUI.color;
            GUI.color = testColor;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(GUI.color, Is.EqualTo(Color.white));
                }

                Assert.That(
                    GUI.color.r,
                    Is.EqualTo(testColor.r).Within(0.001f),
                    "GUI.color should be restored after ExitWGroupTheming scope ends."
                );
                Assert.That(GUI.color.g, Is.EqualTo(testColor.g).Within(0.001f));
                Assert.That(GUI.color.b, Is.EqualTo(testColor.b).Within(0.001f));
            }
            finally
            {
                GUI.color = originalColor;
            }
        }

        [Test]
        public void ExitWGroupThemingRestoresGUIBackgroundColorOnDispose()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color testColor = new(0.9f, 0.1f, 0.6f, 1f);
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = testColor;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(GUI.backgroundColor, Is.EqualTo(Color.white));
                }

                Assert.That(
                    GUI.backgroundColor.r,
                    Is.EqualTo(testColor.r).Within(0.001f),
                    "GUI.backgroundColor should be restored after ExitWGroupTheming scope ends."
                );
                Assert.That(GUI.backgroundColor.g, Is.EqualTo(testColor.g).Within(0.001f));
                Assert.That(GUI.backgroundColor.b, Is.EqualTo(testColor.b).Within(0.001f));
            }
            finally
            {
                GUI.backgroundColor = originalColor;
            }
        }

        [Test]
        public void ExitWGroupThemingRestoresAllGUIColorsOnException()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color testContentColor = new(0.1f, 0.2f, 0.3f, 1f);
            Color testColor = new(0.4f, 0.5f, 0.6f, 1f);
            Color testBackgroundColor = new(0.7f, 0.8f, 0.9f, 1f);

            Color originalContentColor = GUI.contentColor;
            Color originalColor = GUI.color;
            Color originalBackgroundColor = GUI.backgroundColor;

            GUI.contentColor = testContentColor;
            GUI.color = testColor;
            GUI.backgroundColor = testBackgroundColor;

            try
            {
                try
                {
                    using (GroupGUIWidthUtility.ExitWGroupTheming())
                    {
                        throw new InvalidOperationException("Test exception");
                    }
                }
                catch (InvalidOperationException)
                {
                    // Expected
                }

                // All colors should be restored even after exception
                Assert.That(
                    GUI.contentColor.r,
                    Is.EqualTo(testContentColor.r).Within(0.001f),
                    "GUI.contentColor should be restored after exception."
                );
                Assert.That(
                    GUI.color.r,
                    Is.EqualTo(testColor.r).Within(0.001f),
                    "GUI.color should be restored after exception."
                );
                Assert.That(
                    GUI.backgroundColor.r,
                    Is.EqualTo(testBackgroundColor.r).Within(0.001f),
                    "GUI.backgroundColor should be restored after exception."
                );
            }
            finally
            {
                GUI.contentColor = originalContentColor;
                GUI.color = originalColor;
                GUI.backgroundColor = originalBackgroundColor;
            }
        }

        [Test]
        public void ExitWGroupThemingClearsTextFieldNormalBackground()
        {
            GroupGUIWidthUtility.ResetForTests();

            Texture2D savedBackground = EditorStyles.textField.normal.background;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        EditorStyles.textField.normal.background,
                        Is.Null,
                        "EditorStyles.textField.normal.background should be cleared to null inside ExitWGroupTheming scope."
                    );
                }
            }
            finally
            {
                EditorStyles.textField.normal.background = savedBackground;
            }
        }

        [Test]
        public void ExitWGroupThemingClearsNumberFieldNormalBackground()
        {
            GroupGUIWidthUtility.ResetForTests();

            Texture2D savedBackground = EditorStyles.numberField.normal.background;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        EditorStyles.numberField.normal.background,
                        Is.Null,
                        "EditorStyles.numberField.normal.background should be cleared to null inside ExitWGroupTheming scope."
                    );
                }
            }
            finally
            {
                EditorStyles.numberField.normal.background = savedBackground;
            }
        }

        [Test]
        public void ExitWGroupThemingClearsObjectFieldNormalBackground()
        {
            GroupGUIWidthUtility.ResetForTests();

            Texture2D savedBackground = EditorStyles.objectField.normal.background;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        EditorStyles.objectField.normal.background,
                        Is.Null,
                        "EditorStyles.objectField.normal.background should be cleared to null inside ExitWGroupTheming scope."
                    );
                }
            }
            finally
            {
                EditorStyles.objectField.normal.background = savedBackground;
            }
        }

        [Test]
        public void ExitWGroupThemingClearsPopupNormalBackground()
        {
            GroupGUIWidthUtility.ResetForTests();

            Texture2D savedBackground = EditorStyles.popup.normal.background;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        EditorStyles.popup.normal.background,
                        Is.Null,
                        "EditorStyles.popup.normal.background should be cleared to null inside ExitWGroupTheming scope."
                    );
                }
            }
            finally
            {
                EditorStyles.popup.normal.background = savedBackground;
            }
        }

        [Test]
        public void ExitWGroupThemingClearsHelpBoxNormalBackground()
        {
            GroupGUIWidthUtility.ResetForTests();

            Texture2D savedBackground = EditorStyles.helpBox.normal.background;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        EditorStyles.helpBox.normal.background,
                        Is.Null,
                        "EditorStyles.helpBox.normal.background should be cleared to null inside ExitWGroupTheming scope."
                    );
                }
            }
            finally
            {
                EditorStyles.helpBox.normal.background = savedBackground;
            }
        }

        [Test]
        public void ExitWGroupThemingRestoresTextFieldBackgroundOnDispose()
        {
            GroupGUIWidthUtility.ResetForTests();

            Texture2D originalBackground = EditorStyles.textField.normal.background;

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                // Background should be null inside scope
                Assert.That(EditorStyles.textField.normal.background, Is.Null);
            }

            // Background should be restored after scope
            Assert.That(
                EditorStyles.textField.normal.background,
                Is.EqualTo(originalBackground),
                "EditorStyles.textField.normal.background should be restored after ExitWGroupTheming scope ends."
            );
        }

        [Test]
        public void ExitWGroupThemingRestoresNumberFieldBackgroundOnDispose()
        {
            GroupGUIWidthUtility.ResetForTests();

            Texture2D originalBackground = EditorStyles.numberField.normal.background;

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                Assert.That(EditorStyles.numberField.normal.background, Is.Null);
            }

            Assert.That(
                EditorStyles.numberField.normal.background,
                Is.EqualTo(originalBackground),
                "EditorStyles.numberField.normal.background should be restored after ExitWGroupTheming scope ends."
            );
        }

        [Test]
        public void ExitWGroupThemingClearsAllEightBackgroundStatesForTextField()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Save all background states
            Texture2D savedNormal = EditorStyles.textField.normal.background;
            Texture2D savedFocused = EditorStyles.textField.focused.background;
            Texture2D savedActive = EditorStyles.textField.active.background;
            Texture2D savedHover = EditorStyles.textField.hover.background;
            Texture2D savedOnNormal = EditorStyles.textField.onNormal.background;
            Texture2D savedOnFocused = EditorStyles.textField.onFocused.background;
            Texture2D savedOnActive = EditorStyles.textField.onActive.background;
            Texture2D savedOnHover = EditorStyles.textField.onHover.background;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        EditorStyles.textField.normal.background,
                        Is.Null,
                        "normal.background should be null"
                    );
                    Assert.That(
                        EditorStyles.textField.focused.background,
                        Is.Null,
                        "focused.background should be null"
                    );
                    Assert.That(
                        EditorStyles.textField.active.background,
                        Is.Null,
                        "active.background should be null"
                    );
                    Assert.That(
                        EditorStyles.textField.hover.background,
                        Is.Null,
                        "hover.background should be null"
                    );
                    Assert.That(
                        EditorStyles.textField.onNormal.background,
                        Is.Null,
                        "onNormal.background should be null"
                    );
                    Assert.That(
                        EditorStyles.textField.onFocused.background,
                        Is.Null,
                        "onFocused.background should be null"
                    );
                    Assert.That(
                        EditorStyles.textField.onActive.background,
                        Is.Null,
                        "onActive.background should be null"
                    );
                    Assert.That(
                        EditorStyles.textField.onHover.background,
                        Is.Null,
                        "onHover.background should be null"
                    );
                }
            }
            finally
            {
                // Restore all
                EditorStyles.textField.normal.background = savedNormal;
                EditorStyles.textField.focused.background = savedFocused;
                EditorStyles.textField.active.background = savedActive;
                EditorStyles.textField.hover.background = savedHover;
                EditorStyles.textField.onNormal.background = savedOnNormal;
                EditorStyles.textField.onFocused.background = savedOnFocused;
                EditorStyles.textField.onActive.background = savedOnActive;
                EditorStyles.textField.onHover.background = savedOnHover;
            }
        }

        [Test]
        public void ExitWGroupThemingPreservesFoldoutTextColor()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color originalColor = EditorStyles.foldout.normal.textColor;

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                // Text colors are preserved (not cleared) to keep labels visible
                Assert.That(
                    EditorStyles.foldout.normal.textColor,
                    Is.EqualTo(originalColor),
                    "EditorStyles.foldout.normal.textColor should be preserved inside ExitWGroupTheming scope."
                );
            }

            Assert.That(
                EditorStyles.foldout.normal.textColor,
                Is.EqualTo(originalColor),
                "EditorStyles.foldout.normal.textColor should remain unchanged after ExitWGroupTheming scope ends."
            );
        }

        [Test]
        public void ExitWGroupThemingPreservesLabelTextColor()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color originalColor = EditorStyles.label.normal.textColor;

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                // Text colors are preserved (not cleared) to keep labels visible
                Assert.That(
                    EditorStyles.label.normal.textColor,
                    Is.EqualTo(originalColor),
                    "EditorStyles.label.normal.textColor should be preserved inside ExitWGroupTheming scope."
                );
            }

            Assert.That(
                EditorStyles.label.normal.textColor,
                Is.EqualTo(originalColor),
                "EditorStyles.label.normal.textColor should remain unchanged after ExitWGroupTheming scope ends."
            );
        }

        [Test]
        public void ExitWGroupThemingPreservesToggleTextColor()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color originalColor = EditorStyles.toggle.normal.textColor;

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                // Text colors are preserved (not cleared) to keep labels visible
                Assert.That(
                    EditorStyles.toggle.normal.textColor,
                    Is.EqualTo(originalColor),
                    "EditorStyles.toggle.normal.textColor should be preserved inside ExitWGroupTheming scope."
                );
            }

            Assert.That(
                EditorStyles.toggle.normal.textColor,
                Is.EqualTo(originalColor),
                "EditorStyles.toggle.normal.textColor should remain unchanged after ExitWGroupTheming scope ends."
            );
        }

        [Test]
        public void ExitWGroupThemingPreservesMiniButtonTextColor()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color originalColor = EditorStyles.miniButton.normal.textColor;

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                // Text colors are preserved (not cleared) to keep labels visible
                Assert.That(
                    EditorStyles.miniButton.normal.textColor,
                    Is.EqualTo(originalColor),
                    "EditorStyles.miniButton.normal.textColor should be preserved inside ExitWGroupTheming scope."
                );
            }

            Assert.That(
                EditorStyles.miniButton.normal.textColor,
                Is.EqualTo(originalColor),
                "EditorStyles.miniButton.normal.textColor should remain unchanged after ExitWGroupTheming scope ends."
            );
        }

        [Test]
        public void ExitWGroupThemingPreservesAllEightTextColorStatesForLabel()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Save all text color states
            Color savedNormal = EditorStyles.label.normal.textColor;
            Color savedFocused = EditorStyles.label.focused.textColor;
            Color savedActive = EditorStyles.label.active.textColor;
            Color savedHover = EditorStyles.label.hover.textColor;
            Color savedOnNormal = EditorStyles.label.onNormal.textColor;
            Color savedOnFocused = EditorStyles.label.onFocused.textColor;
            Color savedOnActive = EditorStyles.label.onActive.textColor;
            Color savedOnHover = EditorStyles.label.onHover.textColor;

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                // Text colors are preserved (not cleared) to keep labels visible
                Assert.That(
                    EditorStyles.label.normal.textColor,
                    Is.EqualTo(savedNormal),
                    "normal.textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.label.focused.textColor,
                    Is.EqualTo(savedFocused),
                    "focused.textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.label.active.textColor,
                    Is.EqualTo(savedActive),
                    "active.textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.label.hover.textColor,
                    Is.EqualTo(savedHover),
                    "hover.textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.label.onNormal.textColor,
                    Is.EqualTo(savedOnNormal),
                    "onNormal.textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.label.onFocused.textColor,
                    Is.EqualTo(savedOnFocused),
                    "onFocused.textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.label.onActive.textColor,
                    Is.EqualTo(savedOnActive),
                    "onActive.textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.label.onHover.textColor,
                    Is.EqualTo(savedOnHover),
                    "onHover.textColor should be preserved"
                );
            }
        }

        [Test]
        public void ExitWGroupThemingClearsBackgroundsAndPreservesTextColorsFor12Styles()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Save original backgrounds for all 12 styles
            Texture2D textFieldBg = EditorStyles.textField.normal.background;
            Texture2D numberFieldBg = EditorStyles.numberField.normal.background;
            Texture2D objectFieldBg = EditorStyles.objectField.normal.background;
            Texture2D popupBg = EditorStyles.popup.normal.background;
            Texture2D helpBoxBg = EditorStyles.helpBox.normal.background;
            Color foldoutColor = EditorStyles.foldout.normal.textColor;
            Color labelColor = EditorStyles.label.normal.textColor;
            Color toggleColor = EditorStyles.toggle.normal.textColor;
            Color miniButtonColor = EditorStyles.miniButton.normal.textColor;
            Color miniButtonLeftColor = EditorStyles.miniButtonLeft.normal.textColor;
            Color miniButtonMidColor = EditorStyles.miniButtonMid.normal.textColor;
            Color miniButtonRightColor = EditorStyles.miniButtonRight.normal.textColor;

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                // Backgrounds should be cleared inside scope
                Assert.That(EditorStyles.textField.normal.background, Is.Null);
                Assert.That(EditorStyles.numberField.normal.background, Is.Null);
                Assert.That(EditorStyles.objectField.normal.background, Is.Null);
                Assert.That(EditorStyles.popup.normal.background, Is.Null);
                Assert.That(EditorStyles.helpBox.normal.background, Is.Null);

                // Text colors should be preserved (not cleared) to keep labels visible
                Assert.That(
                    EditorStyles.foldout.normal.textColor,
                    Is.EqualTo(foldoutColor),
                    "foldout textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.label.normal.textColor,
                    Is.EqualTo(labelColor),
                    "label textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.toggle.normal.textColor,
                    Is.EqualTo(toggleColor),
                    "toggle textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.miniButton.normal.textColor,
                    Is.EqualTo(miniButtonColor),
                    "miniButton textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.miniButtonLeft.normal.textColor,
                    Is.EqualTo(miniButtonLeftColor),
                    "miniButtonLeft textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.miniButtonMid.normal.textColor,
                    Is.EqualTo(miniButtonMidColor),
                    "miniButtonMid textColor should be preserved"
                );
                Assert.That(
                    EditorStyles.miniButtonRight.normal.textColor,
                    Is.EqualTo(miniButtonRightColor),
                    "miniButtonRight textColor should be preserved"
                );
            }

            // All should be restored after scope
            Assert.That(
                EditorStyles.textField.normal.background,
                Is.EqualTo(textFieldBg),
                "textField background should be restored"
            );
            Assert.That(
                EditorStyles.numberField.normal.background,
                Is.EqualTo(numberFieldBg),
                "numberField background should be restored"
            );
            Assert.That(
                EditorStyles.objectField.normal.background,
                Is.EqualTo(objectFieldBg),
                "objectField background should be restored"
            );
            Assert.That(
                EditorStyles.popup.normal.background,
                Is.EqualTo(popupBg),
                "popup background should be restored"
            );
            Assert.That(
                EditorStyles.helpBox.normal.background,
                Is.EqualTo(helpBoxBg),
                "helpBox background should be restored"
            );
            Assert.That(
                EditorStyles.foldout.normal.textColor,
                Is.EqualTo(foldoutColor),
                "foldout textColor should be restored"
            );
            Assert.That(
                EditorStyles.label.normal.textColor,
                Is.EqualTo(labelColor),
                "label textColor should be restored"
            );
            Assert.That(
                EditorStyles.toggle.normal.textColor,
                Is.EqualTo(toggleColor),
                "toggle textColor should be restored"
            );
            Assert.That(
                EditorStyles.miniButton.normal.textColor,
                Is.EqualTo(miniButtonColor),
                "miniButton textColor should be restored"
            );
            Assert.That(
                EditorStyles.miniButtonLeft.normal.textColor,
                Is.EqualTo(miniButtonLeftColor),
                "miniButtonLeft textColor should be restored"
            );
            Assert.That(
                EditorStyles.miniButtonMid.normal.textColor,
                Is.EqualTo(miniButtonMidColor),
                "miniButtonMid textColor should be restored"
            );
            Assert.That(
                EditorStyles.miniButtonRight.normal.textColor,
                Is.EqualTo(miniButtonRightColor),
                "miniButtonRight textColor should be restored"
            );
        }

        [Test]
        public void ExitWGroupThemingRestoresEditorStylesOnException()
        {
            GroupGUIWidthUtility.ResetForTests();

            Texture2D originalTextFieldBg = EditorStyles.textField.normal.background;
            Color originalLabelColor = EditorStyles.label.normal.textColor;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    throw new InvalidOperationException("Test exception");
                }
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            Assert.That(
                EditorStyles.textField.normal.background,
                Is.EqualTo(originalTextFieldBg),
                "textField background should be restored after exception"
            );
            Assert.That(
                EditorStyles.label.normal.textColor,
                Is.EqualTo(originalLabelColor),
                "label textColor should be restored after exception"
            );
        }

        [Test]
        public void ExitWGroupThemingNestedScopesRestoreSequentially()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color color1 = new(0.1f, 0.2f, 0.3f, 1f);
            Color color2 = new(0.4f, 0.5f, 0.6f, 1f);

            Color originalColor = GUI.contentColor;

            try
            {
                GUI.contentColor = color1;

                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(GUI.contentColor, Is.EqualTo(Color.white), "First scope: white");

                    GUI.contentColor = color2;

                    using (GroupGUIWidthUtility.ExitWGroupTheming())
                    {
                        Assert.That(
                            GUI.contentColor,
                            Is.EqualTo(Color.white),
                            "Nested scope: white"
                        );
                    }

                    // After inner scope, should restore to color2 (what it was before inner scope)
                    Assert.That(
                        GUI.contentColor.r,
                        Is.EqualTo(color2.r).Within(0.001f),
                        "After inner scope: color2"
                    );
                }

                // After outer scope, should restore to color1 (what it was before outer scope)
                Assert.That(
                    GUI.contentColor.r,
                    Is.EqualTo(color1.r).Within(0.001f),
                    "After outer scope: color1"
                );
            }
            finally
            {
                GUI.contentColor = originalColor;
            }
        }

        [Test]
        public void ExitWGroupThemingSequentialScopesAreIndependent()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color color1 = new(0.1f, 0.2f, 0.3f, 1f);
            Color color2 = new(0.7f, 0.8f, 0.9f, 1f);

            Color originalColor = GUI.contentColor;

            try
            {
                GUI.contentColor = color1;

                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(GUI.contentColor, Is.EqualTo(Color.white));
                }

                Assert.That(
                    GUI.contentColor.r,
                    Is.EqualTo(color1.r).Within(0.001f),
                    "First scope restored to color1"
                );

                GUI.contentColor = color2;

                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(GUI.contentColor, Is.EqualTo(Color.white));
                }

                Assert.That(
                    GUI.contentColor.r,
                    Is.EqualTo(color2.r).Within(0.001f),
                    "Second scope restored to color2"
                );
            }
            finally
            {
                GUI.contentColor = originalColor;
            }
        }

        [Test]
        public void ExitWGroupThemingWithNestedWGroupPaletteScope()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Simulate nested WGroup palettes
            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(GroupGUIWidthUtility.IsInsideWGroupPropertyDraw, Is.True);

                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Assert.That(GroupGUIWidthUtility.IsInsideWGroupPropertyDraw, Is.True);

                    using (GroupGUIWidthUtility.ExitWGroupTheming())
                    {
                        Assert.That(
                            GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                            Is.False,
                            "Exit theming should clear context"
                        );
                        Assert.That(
                            GroupGUIWidthUtility.IsInsideWGroup,
                            Is.False,
                            "Exit theming should clear IsInsideWGroup"
                        );
                    }

                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                        Is.True,
                        "After exit scope, should be restored"
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "After inner WGroup scope"
                );
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "After all scopes"
            );
        }

        [Test]
        public void ExitWGroupThemingIsIdempotentOnMultipleDispose()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color testColor = new(0.5f, 0.5f, 0.5f, 1f);
            Color originalColor = GUI.contentColor;
            GUI.contentColor = testColor;

            try
            {
                IDisposable scope = GroupGUIWidthUtility.ExitWGroupTheming();

                Assert.That(GUI.contentColor, Is.EqualTo(Color.white));

                scope.Dispose();

                Assert.That(
                    GUI.contentColor.r,
                    Is.EqualTo(testColor.r).Within(0.001f),
                    "First dispose restores color"
                );

                // Second dispose should be no-op
                scope.Dispose();

                Assert.That(
                    GUI.contentColor.r,
                    Is.EqualTo(testColor.r).Within(0.001f),
                    "Second dispose should not change anything"
                );
            }
            finally
            {
                GUI.contentColor = originalColor;
            }
        }

        [Test]
        public void ExitWGroupThemingWhenNotInWGroupContextIsNoOp()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Verify not in any WGroup context
            Assert.That(GroupGUIWidthUtility.IsInsideWGroupPropertyDraw, Is.False);
            Assert.That(GroupGUIWidthUtility.IsInsideWGroup, Is.False);

            Color originalContentColor = GUI.contentColor;
            Color originalColor = GUI.color;
            Color originalBackgroundColor = GUI.backgroundColor;

            try
            {
                // Even outside WGroup, scope should work without errors
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    // Should still reset to white
                    Assert.That(GUI.contentColor, Is.EqualTo(Color.white));
                    Assert.That(GUI.color, Is.EqualTo(Color.white));
                    Assert.That(GUI.backgroundColor, Is.EqualTo(Color.white));

                    // Context flags should remain false
                    Assert.That(GroupGUIWidthUtility.IsInsideWGroupPropertyDraw, Is.False);
                    Assert.That(GroupGUIWidthUtility.IsInsideWGroup, Is.False);
                }

                // Should restore original colors
                Assert.That(GUI.contentColor, Is.EqualTo(originalContentColor));
                Assert.That(GUI.color, Is.EqualTo(originalColor));
                Assert.That(GUI.backgroundColor, Is.EqualTo(originalBackgroundColor));
            }
            finally
            {
                GUI.contentColor = originalContentColor;
                GUI.color = originalColor;
                GUI.backgroundColor = originalBackgroundColor;
            }
        }

        [Test]
        public void ExitWGroupThemingDefaultTextColorIsTransparentBlack()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color savedColor = EditorStyles.label.normal.textColor;

            try
            {
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Color clearedColor = EditorStyles.label.normal.textColor;

                    // default(Color) is (0, 0, 0, 0) - transparent black
                    Assert.That(
                        clearedColor.r,
                        Is.EqualTo(0f).Within(0.001f),
                        "Cleared textColor.r should be 0"
                    );
                    Assert.That(
                        clearedColor.g,
                        Is.EqualTo(0f).Within(0.001f),
                        "Cleared textColor.g should be 0"
                    );
                    Assert.That(
                        clearedColor.b,
                        Is.EqualTo(0f).Within(0.001f),
                        "Cleared textColor.b should be 0"
                    );
                    Assert.That(
                        clearedColor.a,
                        Is.EqualTo(0f).Within(0.001f),
                        "Cleared textColor.a should be 0"
                    );
                }
            }
            finally
            {
                EditorStyles.label.normal.textColor = savedColor;
            }
        }

        [Test]
        public void ExitWGroupThemingClearsPaletteToNull()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Even though we can't directly set palette, verify IsInsideWGroup behavior
            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroup,
                Is.False,
                "Should not be inside WGroup initially"
            );

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroup,
                    Is.False,
                    "Should not be inside WGroup in exit scope"
                );

                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette,
                    Is.Null,
                    "CurrentPalette should be null in exit scope"
                );
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroup,
                Is.False,
                "Should not be inside WGroup after exit scope"
            );
        }

        [Test]
        public void ExitWGroupThemingRestoresPaletteOnException()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                bool wasInside = GroupGUIWidthUtility.IsInsideWGroupPropertyDraw;

                try
                {
                    using (GroupGUIWidthUtility.ExitWGroupTheming())
                    {
                        Assert.That(
                            GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                            Is.False,
                            "Should not be inside during exit scope"
                        );
                        throw new InvalidOperationException("Test exception");
                    }
                }
                catch (InvalidOperationException)
                {
                    // Expected
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.EqualTo(wasInside),
                    "Should restore context after exception"
                );
            }
        }

        [Test]
        public void ExitWGroupThemingSavesAndRestoresFullTextFieldState()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Save all 8 background states
            Texture2D[] savedBackgrounds = new Texture2D[8];
            savedBackgrounds[0] = EditorStyles.textField.normal.background;
            savedBackgrounds[1] = EditorStyles.textField.focused.background;
            savedBackgrounds[2] = EditorStyles.textField.active.background;
            savedBackgrounds[3] = EditorStyles.textField.hover.background;
            savedBackgrounds[4] = EditorStyles.textField.onNormal.background;
            savedBackgrounds[5] = EditorStyles.textField.onFocused.background;
            savedBackgrounds[6] = EditorStyles.textField.onActive.background;
            savedBackgrounds[7] = EditorStyles.textField.onHover.background;

            // Save all 8 text color states
            Color[] savedColors = new Color[8];
            savedColors[0] = EditorStyles.textField.normal.textColor;
            savedColors[1] = EditorStyles.textField.focused.textColor;
            savedColors[2] = EditorStyles.textField.active.textColor;
            savedColors[3] = EditorStyles.textField.hover.textColor;
            savedColors[4] = EditorStyles.textField.onNormal.textColor;
            savedColors[5] = EditorStyles.textField.onFocused.textColor;
            savedColors[6] = EditorStyles.textField.onActive.textColor;
            savedColors[7] = EditorStyles.textField.onHover.textColor;

            using (GroupGUIWidthUtility.ExitWGroupTheming())
            {
                // All should be reset
            }

            // All should be restored
            Assert.That(
                EditorStyles.textField.normal.background,
                Is.EqualTo(savedBackgrounds[0]),
                "normal.background"
            );
            Assert.That(
                EditorStyles.textField.focused.background,
                Is.EqualTo(savedBackgrounds[1]),
                "focused.background"
            );
            Assert.That(
                EditorStyles.textField.active.background,
                Is.EqualTo(savedBackgrounds[2]),
                "active.background"
            );
            Assert.That(
                EditorStyles.textField.hover.background,
                Is.EqualTo(savedBackgrounds[3]),
                "hover.background"
            );
            Assert.That(
                EditorStyles.textField.onNormal.background,
                Is.EqualTo(savedBackgrounds[4]),
                "onNormal.background"
            );
            Assert.That(
                EditorStyles.textField.onFocused.background,
                Is.EqualTo(savedBackgrounds[5]),
                "onFocused.background"
            );
            Assert.That(
                EditorStyles.textField.onActive.background,
                Is.EqualTo(savedBackgrounds[6]),
                "onActive.background"
            );
            Assert.That(
                EditorStyles.textField.onHover.background,
                Is.EqualTo(savedBackgrounds[7]),
                "onHover.background"
            );

            Assert.That(
                EditorStyles.textField.normal.textColor,
                Is.EqualTo(savedColors[0]),
                "normal.textColor"
            );
            Assert.That(
                EditorStyles.textField.focused.textColor,
                Is.EqualTo(savedColors[1]),
                "focused.textColor"
            );
            Assert.That(
                EditorStyles.textField.active.textColor,
                Is.EqualTo(savedColors[2]),
                "active.textColor"
            );
            Assert.That(
                EditorStyles.textField.hover.textColor,
                Is.EqualTo(savedColors[3]),
                "hover.textColor"
            );
            Assert.That(
                EditorStyles.textField.onNormal.textColor,
                Is.EqualTo(savedColors[4]),
                "onNormal.textColor"
            );
            Assert.That(
                EditorStyles.textField.onFocused.textColor,
                Is.EqualTo(savedColors[5]),
                "onFocused.textColor"
            );
            Assert.That(
                EditorStyles.textField.onActive.textColor,
                Is.EqualTo(savedColors[6]),
                "onActive.textColor"
            );
            Assert.That(
                EditorStyles.textField.onHover.textColor,
                Is.EqualTo(savedColors[7]),
                "onHover.textColor"
            );
        }

        [Test]
        public void DictionaryInsideWGroupPropertyContextDoesNotApplyPadding()
        {
            // When inside WGroup property context, WGroup uses EditorGUILayout.PropertyField
            // which means Unity's layout system already constrains the rect. The drawer should
            // NOT apply padding again - only EditorGUI.IndentedRect.
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float GroupLeftPadding = 12f;
            const float GroupRightPadding = 8f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            EditorGUI.indentLevel = 0;

            GroupGUIWidthUtility.ResetForTests();
            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    GroupLeftPadding,
                    GroupRightPadding
                )
            )
            {
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    // Padding should NOT be applied - Unity's layout handles it
                    Assert.AreEqual(
                        controlRect.x,
                        resolvedRect.x,
                        0.01f,
                        "Dictionary inside WGroup context should NOT apply padding (Unity layout handles it)."
                    );

                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.01f,
                        "Dictionary inside WGroup context should NOT reduce width (Unity layout handles it)."
                    );
                }
            }
        }

        [Test]
        public void NestedWGroupsWithDictionaryDoNotApplyCumulativePadding()
        {
            // When inside WGroup property context, the drawer does not apply padding
            // because Unity's layout system already handles it.
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float OuterLeftPadding = 10f;
            const float OuterRightPadding = 10f;
            const float InnerLeftPadding = 8f;
            const float InnerRightPadding = 8f;

            EditorGUI.indentLevel = 0;

            GroupGUIWidthUtility.ResetForTests();
            using (
                GroupGUIWidthUtility.PushContentPadding(
                    OuterLeftPadding + OuterRightPadding,
                    OuterLeftPadding,
                    OuterRightPadding
                )
            )
            {
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            InnerLeftPadding + InnerRightPadding,
                            InnerLeftPadding,
                            InnerRightPadding
                        )
                    )
                    {
                        using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                        {
                            Rect resolvedRect =
                                SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                    controlRect,
                                    skipIndentation: false
                                );

                            // Padding should NOT be applied - Unity's layout handles it
                            Assert.AreEqual(
                                controlRect.x,
                                resolvedRect.x,
                                0.01f,
                                "Nested WGroups should NOT apply padding for dictionary."
                            );

                            Assert.AreEqual(
                                controlRect.width,
                                resolvedRect.width,
                                0.01f,
                                "Nested WGroups should NOT reduce width for dictionary."
                            );
                        }
                    }
                }
            }
        }

        [Test]
        public void WGroupFoldoutAlignmentOffsetOnlyAppliedInsideWGroupContextForDictionary()
        {
            float alignmentOffset =
                SerializableDictionaryPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.That(
                alignmentOffset,
                Is.GreaterThan(0f),
                "WGroupFoldoutAlignmentOffset should be a positive value."
            );
            Assert.That(
                alignmentOffset,
                Is.EqualTo(2.5f).Within(0.001f),
                "WGroupFoldoutAlignmentOffset should be 2.5f for consistent visual alignment."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryFoldoutGetsAlignmentOffsetWhenInsideWGroupPropertyContext()
        {
            IntegrationTestWGroupDictionaryHost host =
                CreateScriptableObject<IntegrationTestWGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            const float SimulatedLeftPadding = 12f;
            const float SimulatedRightPadding = 12f;
            float horizontalPadding = SimulatedLeftPadding + SimulatedRightPadding;

            Rect capturedFoldoutRect = default;
            bool hasFoldoutRect = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            SimulatedLeftPadding,
                            SimulatedRightPadding
                        )
                    )
                    {
                        using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                        {
                            serializedObject.UpdateIfRequiredOrScript();
                            drawer.OnGUI(controlRect, dictionaryProperty, label);

                            hasFoldoutRect =
                                SerializableDictionaryPropertyDrawer.HasLastMainFoldoutRect;
                            if (hasFoldoutRect)
                            {
                                capturedFoldoutRect =
                                    SerializableDictionaryPropertyDrawer.LastMainFoldoutRect;
                            }
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(hasFoldoutRect, "Main foldout rect should be tracked after OnGUI.");

            float expectedX =
                controlRect.x
                + SimulatedLeftPadding
                + SerializableDictionaryPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.AreEqual(
                expectedX,
                capturedFoldoutRect.x,
                0.1f,
                "Dictionary foldout inside WGroup property context should be shifted right by alignment offset."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryFoldoutDoesNotGetAlignmentOffsetOutsideWGroupPropertyContext()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            Rect capturedFoldoutRect = default;
            Rect capturedResolvedPosition = default;
            bool hasFoldoutRect = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    // No WGroup context - should not apply alignment offset
                    serializedObject.UpdateIfRequiredOrScript();
                    drawer.OnGUI(controlRect, dictionaryProperty, label);

                    hasFoldoutRect = SerializableDictionaryPropertyDrawer.HasLastMainFoldoutRect;
                    if (hasFoldoutRect)
                    {
                        capturedFoldoutRect =
                            SerializableDictionaryPropertyDrawer.LastMainFoldoutRect;
                        capturedResolvedPosition = drawer.LastResolvedPosition;
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(hasFoldoutRect, "Main foldout rect should be tracked after OnGUI.");

            // Outside WGroup context, the foldout x should match the resolved position x
            // (no alignment offset applied)
            Assert.AreEqual(
                capturedResolvedPosition.x,
                capturedFoldoutRect.x,
                0.1f,
                "Dictionary foldout outside WGroup property context should NOT have alignment offset applied."
            );
        }

        [Test]
        public void SetInsideWGroupPropertyContextDoesNotApplyPadding()
        {
            // When inside WGroup property context, WGroup uses EditorGUILayout.PropertyField
            // which means Unity's layout system already constrains the rect. The drawer should
            // NOT apply padding again - only EditorGUI.IndentedRect.
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float GroupLeftPadding = 12f;
            const float GroupRightPadding = 8f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            EditorGUI.indentLevel = 0;

            GroupGUIWidthUtility.ResetForTests();
            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    GroupLeftPadding,
                    GroupRightPadding
                )
            )
            {
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    // Padding should NOT be applied - Unity's layout handles it
                    Assert.AreEqual(
                        controlRect.x,
                        resolvedRect.x,
                        0.01f,
                        "Set inside WGroup context should NOT apply padding (Unity layout handles it)."
                    );

                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.01f,
                        "Set inside WGroup context should NOT reduce width (Unity layout handles it)."
                    );
                }
            }
        }

        [Test]
        public void NestedWGroupsWithSetDoNotApplyCumulativePadding()
        {
            // When inside WGroup property context, the drawer does not apply padding
            // because Unity's layout system already handles it.
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float OuterLeftPadding = 10f;
            const float OuterRightPadding = 10f;
            const float InnerLeftPadding = 8f;
            const float InnerRightPadding = 8f;

            EditorGUI.indentLevel = 0;

            GroupGUIWidthUtility.ResetForTests();
            using (
                GroupGUIWidthUtility.PushContentPadding(
                    OuterLeftPadding + OuterRightPadding,
                    OuterLeftPadding,
                    OuterRightPadding
                )
            )
            {
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            InnerLeftPadding + InnerRightPadding,
                            InnerLeftPadding,
                            InnerRightPadding
                        )
                    )
                    {
                        using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                        {
                            Rect resolvedRect =
                                SerializableSetPropertyDrawer.ResolveContentRectForTests(
                                    controlRect,
                                    skipIndentation: false
                                );

                            // Padding should NOT be applied - Unity's layout handles it
                            Assert.AreEqual(
                                controlRect.x,
                                resolvedRect.x,
                                0.01f,
                                "Nested WGroups should NOT apply padding for set."
                            );

                            Assert.AreEqual(
                                controlRect.width,
                                resolvedRect.width,
                                0.01f,
                                "Nested WGroups should NOT reduce width for set."
                            );
                        }
                    }
                }
            }
        }

        [Test]
        public void WGroupFoldoutAlignmentOffsetOnlyAppliedInsideWGroupContextForSet()
        {
            float alignmentOffset = SerializableSetPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.That(
                alignmentOffset,
                Is.GreaterThan(0f),
                "WGroupFoldoutAlignmentOffset should be a positive value."
            );
            Assert.That(
                alignmentOffset,
                Is.EqualTo(2.5f).Within(0.001f),
                "WGroupFoldoutAlignmentOffset should be 2.5f for consistent visual alignment."
            );
        }

        [UnityTest]
        public IEnumerator SetFoldoutGetsAlignmentOffsetWhenInsideWGroupPropertyContext()
        {
            IntegrationTestWGroupSetHost host =
                CreateScriptableObject<IntegrationTestWGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupSetHost.set)
            );
            setProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            const float SimulatedLeftPadding = 12f;
            const float SimulatedRightPadding = 12f;
            float horizontalPadding = SimulatedLeftPadding + SimulatedRightPadding;

            Rect capturedFoldoutRect = default;
            bool hasFoldoutRect = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            SimulatedLeftPadding,
                            SimulatedRightPadding
                        )
                    )
                    {
                        using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                        {
                            serializedObject.UpdateIfRequiredOrScript();
                            drawer.OnGUI(controlRect, setProperty, label);

                            hasFoldoutRect = SerializableSetPropertyDrawer.HasLastMainFoldoutRect;
                            if (hasFoldoutRect)
                            {
                                capturedFoldoutRect =
                                    SerializableSetPropertyDrawer.LastMainFoldoutRect;
                            }
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(hasFoldoutRect, "Main foldout rect should be tracked after OnGUI.");

            float expectedX =
                controlRect.x
                + SimulatedLeftPadding
                + SerializableSetPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.AreEqual(
                expectedX,
                capturedFoldoutRect.x,
                0.1f,
                "Set foldout inside WGroup property context should be shifted right by alignment offset."
            );
        }

        [UnityTest]
        public IEnumerator SetFoldoutDoesNotGetAlignmentOffsetOutsideWGroupPropertyContext()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            host.set.Add("test_value");

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            setProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            Rect capturedFoldoutRect = default;
            Rect capturedResolvedPosition = default;
            bool hasFoldoutRect = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

                    // No WGroup context - should not apply alignment offset
                    serializedObject.UpdateIfRequiredOrScript();
                    drawer.OnGUI(controlRect, setProperty, label);

                    hasFoldoutRect = SerializableSetPropertyDrawer.HasLastMainFoldoutRect;
                    if (hasFoldoutRect)
                    {
                        capturedFoldoutRect = SerializableSetPropertyDrawer.LastMainFoldoutRect;
                        capturedResolvedPosition = drawer.LastResolvedPosition;
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(hasFoldoutRect, "Main foldout rect should be tracked after OnGUI.");

            // Outside WGroup context, the foldout x should match the resolved position x
            // (no alignment offset applied)
            Assert.AreEqual(
                capturedResolvedPosition.x,
                capturedFoldoutRect.x,
                0.1f,
                "Set foldout outside WGroup property context should NOT have alignment offset applied."
            );
        }

        [Test]
        public void WGroupPropertyContextIsIndependentOfPaddingScope()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Push padding without property context
            using (GroupGUIWidthUtility.PushContentPadding(10f, 5f, 5f))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(1),
                    "Scope depth should be 1 after padding push."
                );
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.False,
                    "Property context should still be false without explicit push."
                );

                // Now push property context
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentScopeDepth,
                        Is.EqualTo(1),
                        "Scope depth should remain 1 - property context doesn't affect depth."
                    );
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                        Is.True,
                        "Property context should be true after push."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.False,
                    "Property context should return to false."
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(1),
                    "Scope depth should still be 1."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentScopeDepth,
                Is.EqualTo(0),
                "Scope depth should be 0 after all scopes disposed."
            );
        }

        [Test]
        public void WGroupPropertyContextWithoutPaddingStillTracksCorrectly()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Push property context without any padding
            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Property context should be true."
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(0),
                    "Scope depth should remain 0 - no padding was pushed."
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentHorizontalPadding,
                    Is.EqualTo(0f).Within(0.001f),
                    "Horizontal padding should be 0."
                );
            }
        }

        [Test]
        public void DictionaryAndSetHaveMatchingWGroupFoldoutAlignmentOffsets()
        {
            float dictionaryOffset =
                SerializableDictionaryPropertyDrawer.WGroupFoldoutAlignmentOffset;
            float setOffset = SerializableSetPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.AreEqual(
                dictionaryOffset,
                setOffset,
                0.001f,
                "Dictionary and Set should have matching WGroupFoldoutAlignmentOffset values for visual consistency."
            );
        }
    }
#endif
}
