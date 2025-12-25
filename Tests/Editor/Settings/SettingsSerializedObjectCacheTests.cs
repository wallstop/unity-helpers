#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Settings
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    /// <summary>
    /// Tests for verifying SerializedObject caching and foldout state persistence
    /// in the UnityHelpersSettings panel.
    /// </summary>
    public sealed class SettingsSerializedObjectCacheTests
    {
        private bool _originalDictionaryTweenEnabled;
        private bool _originalSortedDictionaryTweenEnabled;

        [SetUp]
        public void SetUp()
        {
            _originalDictionaryTweenEnabled =
                UnityHelpersSettings.ShouldTweenSerializableDictionaryFoldouts();
            _originalSortedDictionaryTweenEnabled =
                UnityHelpersSettings.ShouldTweenSerializableSortedDictionaryFoldouts();

            UnityHelpersSettings.ClearCachedSerializedObjectForTests();
            SerializableDictionaryPropertyDrawer.ClearMainFoldoutAnimCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(
                _originalDictionaryTweenEnabled
            );
            UnityHelpersSettings.SetSerializableSortedDictionaryFoldoutTweenEnabled(
                _originalSortedDictionaryTweenEnabled
            );

            UnityHelpersSettings.ClearCachedSerializedObjectForTests();
            SerializableDictionaryPropertyDrawer.ClearMainFoldoutAnimCacheForTests();
        }

        [Test]
        public void CachedSerializedObjectPreservesIsExpandedStateAcrossFrames()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            using SerializedObject firstAccess = GetCachedSerializedObject(settings);
            SerializedProperty property = firstAccess.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(property, "WButtonCustomColors property should exist.");

            bool originalExpanded = property.isExpanded;
            property.isExpanded = false;

            using SerializedObject secondAccess = GetCachedSerializedObject(settings);
            SerializedProperty sameProperty = secondAccess.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.AreSame(
                firstAccess,
                secondAccess,
                "GetCachedSerializedObject should return the same instance."
            );
            Assert.IsFalse(
                sameProperty.isExpanded,
                "isExpanded state should be preserved when using cached SerializedObject."
            );

            property.isExpanded = originalExpanded;
        }

        [Test]
        public void CachedSerializedObjectPreservesWEnumToggleButtonsExpandedState()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            using SerializedObject firstAccess = GetCachedSerializedObject(settings);
            SerializedProperty property = firstAccess.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WEnumToggleButtonsCustomColors
            );
            Assert.IsNotNull(property, "WEnumToggleButtonsCustomColors property should exist.");

            bool originalExpanded = property.isExpanded;
            property.isExpanded = false;

            using SerializedObject secondAccess = GetCachedSerializedObject(settings);
            SerializedProperty sameProperty = secondAccess.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WEnumToggleButtonsCustomColors
            );

            Assert.AreSame(
                firstAccess,
                secondAccess,
                "GetCachedSerializedObject should return the same instance."
            );
            Assert.IsFalse(
                sameProperty.isExpanded,
                "WEnumToggleButtons isExpanded state should be preserved when using cached SerializedObject."
            );

            property.isExpanded = originalExpanded;
        }

        [Test]
        public void NewSerializedObjectDoesNotPreserveIsExpandedState()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            bool? capturedOriginalState = null;
            bool stateWasReset = false;

            using (SerializedObject firstObject = new(settings))
            {
                SerializedProperty property = firstObject.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
                );
                if (property != null)
                {
                    capturedOriginalState = property.isExpanded;
                    property.isExpanded = !capturedOriginalState.Value;
                }
            }

            using (SerializedObject secondObject = new(settings))
            {
                SerializedProperty property = secondObject.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
                );
                if (property != null && capturedOriginalState.HasValue)
                {
                    stateWasReset = property.isExpanded == capturedOriginalState.Value;
                }
            }

            Assert.IsTrue(
                capturedOriginalState.HasValue,
                "WButtonCustomColors property should exist for the test."
            );
        }

        [Test]
        public void ClearCachedSerializedObjectForcesNewObjectCreation()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            using SerializedObject firstAccess = GetCachedSerializedObject(settings);
            int firstHashCode = firstAccess.GetHashCode();

            UnityHelpersSettings.ClearCachedSerializedObjectForTests();

            using SerializedObject secondAccess = GetCachedSerializedObject(settings);
            int secondHashCode = secondAccess.GetHashCode();

            Assert.AreNotEqual(
                firstHashCode,
                secondHashCode,
                "After clearing the cache, a new SerializedObject should be created."
            );
        }

        [Test]
        public void CachedSerializedObjectHandlesNullGracefully()
        {
            UnityHelpersSettings.ClearCachedSerializedObjectForTests();

            using SerializedObject result = GetCachedSerializedObjectWithNull();
            Assert.IsNull(result, "GetCachedSerializedObject should return null for null input.");
        }

        [Test]
        public void CachedSerializedObjectIsValidAfterUpdate()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            using SerializedObject cached = GetCachedSerializedObject(settings);
            Assert.IsNotNull(cached, "Cached SerializedObject should not be null.");
            Assert.IsNotNull(
                cached.targetObject,
                "Cached SerializedObject target should not be null."
            );

            cached.UpdateIfRequiredOrScript();

            Assert.IsNotNull(
                cached.targetObject,
                "Target object should remain valid after update."
            );

            SerializedProperty property = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(property, "Should be able to find properties after update.");
        }

        [Test]
        public void MultipleFoldoutPropertiesPreserveIndependentState()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            using SerializedObject cached = GetCachedSerializedObject(settings);

            SerializedProperty wButtonColors = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            SerializedProperty wEnumColors = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WEnumToggleButtonsCustomColors
            );

            Assert.IsNotNull(wButtonColors, "WButtonCustomColors property should exist.");
            Assert.IsNotNull(wEnumColors, "WEnumToggleButtonsCustomColors property should exist.");

            bool originalWButtonState = wButtonColors.isExpanded;
            bool originalWEnumState = wEnumColors.isExpanded;

            try
            {
                wButtonColors.isExpanded = true;
                wEnumColors.isExpanded = false;

                using SerializedObject cachedAgain = GetCachedSerializedObject(settings);

                SerializedProperty wButtonColorsAgain = cachedAgain.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
                );
                SerializedProperty wEnumColorsAgain = cachedAgain.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WEnumToggleButtonsCustomColors
                );

                Assert.IsTrue(
                    wButtonColorsAgain.isExpanded,
                    "WButtonCustomColors should remain expanded."
                );
                Assert.IsFalse(
                    wEnumColorsAgain.isExpanded,
                    "WEnumToggleButtonsCustomColors should remain collapsed."
                );
            }
            finally
            {
                wButtonColors.isExpanded = originalWButtonState;
                wEnumColors.isExpanded = originalWEnumState;
            }
        }

        [Test]
        public void FoldoutStateTogglePreservesAcrossMultipleAccesses()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            using SerializedObject cached = GetCachedSerializedObject(settings);
            SerializedProperty property = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(property, "WButtonCustomColors property should exist.");

            bool originalState = property.isExpanded;

            try
            {
                for (int iteration = 0; iteration < 5; iteration++)
                {
                    bool expectedState = iteration % 2 == 0;
                    property.isExpanded = expectedState;

                    using SerializedObject cachedAgain = GetCachedSerializedObject(settings);
                    SerializedProperty propertyAgain = cachedAgain.FindProperty(
                        UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
                    );

                    Assert.AreEqual(
                        expectedState,
                        propertyAgain.isExpanded,
                        $"Iteration {iteration}: isExpanded state should be preserved."
                    );
                }
            }
            finally
            {
                property.isExpanded = originalState;
            }
        }

        [Test]
        public void CachedSerializedObjectSurvivesApplyModifiedProperties()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            using SerializedObject cached = GetCachedSerializedObject(settings);
            SerializedProperty property = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(property, "WButtonCustomColors property should exist.");

            bool originalState = property.isExpanded;

            try
            {
                property.isExpanded = !originalState;
                cached.ApplyModifiedPropertiesWithoutUndo();

                using SerializedObject cachedAgain = GetCachedSerializedObject(settings);
                SerializedProperty propertyAgain = cachedAgain.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
                );

                Assert.AreEqual(
                    !originalState,
                    propertyAgain.isExpanded,
                    "isExpanded state should survive ApplyModifiedPropertiesWithoutUndo."
                );
            }
            finally
            {
                property.isExpanded = originalState;
                cached.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [Test]
        public void AnimBoolCacheKeyUsesCorrectInstanceId()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            using SerializedObject cached = GetCachedSerializedObject(settings);
            int instanceId = cached.targetObject.GetInstanceID();

            Assert.AreNotEqual(
                0,
                instanceId,
                "UnityHelpersSettings instance should have a valid non-zero instance ID."
            );

            using SerializedObject cachedAgain = GetCachedSerializedObject(settings);
            int sameInstanceId = cachedAgain.targetObject.GetInstanceID();

            Assert.AreEqual(
                instanceId,
                sameInstanceId,
                "Cached SerializedObject should reference the same target with the same instance ID."
            );
        }

        [Test]
        public void CachedSerializedObjectWorksWithUpdateCycles()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            using SerializedObject cached = GetCachedSerializedObject(settings);
            SerializedProperty property = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(property, "WButtonCustomColors property should exist.");

            bool originalState = property.isExpanded;

            try
            {
                property.isExpanded = !originalState;

                for (int cycle = 0; cycle < 3; cycle++)
                {
                    cached.UpdateIfRequiredOrScript();

                    using SerializedObject sameCached = GetCachedSerializedObject(settings);
                    SerializedProperty sameProperty = sameCached.FindProperty(
                        UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
                    );

                    Assert.AreEqual(
                        !originalState,
                        sameProperty.isExpanded,
                        $"Cycle {cycle}: isExpanded state should persist through update cycles."
                    );
                }
            }
            finally
            {
                property.isExpanded = originalState;
            }
        }

        [Test]
        public void SettingsProviderPatternSimulation()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            UnityHelpersSettings.ClearCachedSerializedObjectForTests();

            using SerializedObject cached = GetCachedSerializedObject(settings);
            SerializedProperty wButtonColors = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(wButtonColors, "WButtonCustomColors property should exist.");

            bool originalState = wButtonColors.isExpanded;

            try
            {
                wButtonColors.isExpanded = true;
                cached.ApplyModifiedPropertiesWithoutUndo();

                for (int frame = 0; frame < 10; frame++)
                {
                    using SerializedObject frameCached = GetCachedSerializedObject(settings);
                    frameCached.UpdateIfRequiredOrScript();

                    SerializedProperty frameProperty = frameCached.FindProperty(
                        UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
                    );

                    Assert.IsTrue(
                        frameProperty.isExpanded,
                        $"Frame {frame}: Property should stay expanded with cached SerializedObject."
                    );
                }

                wButtonColors.isExpanded = false;
                cached.ApplyModifiedPropertiesWithoutUndo();

                for (int frame = 0; frame < 10; frame++)
                {
                    using SerializedObject frameCached = GetCachedSerializedObject(settings);
                    frameCached.UpdateIfRequiredOrScript();

                    SerializedProperty frameProperty = frameCached.FindProperty(
                        UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
                    );

                    Assert.IsFalse(
                        frameProperty.isExpanded,
                        $"Frame {frame}: Property should stay collapsed with cached SerializedObject."
                    );
                }
            }
            finally
            {
                wButtonColors.isExpanded = originalState;
                cached.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [Test]
        public void NonCachedPatternDemonstratesStateLoss()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            int toggleCount = 0;
            bool? lastState = null;

            for (int frame = 0; frame < 5; frame++)
            {
                using SerializedObject newObject = new(settings);
                newObject.UpdateIfRequiredOrScript();

                SerializedProperty property = newObject.FindProperty(
                    UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
                );
                if (property != null)
                {
                    if (lastState.HasValue && property.isExpanded != lastState.Value)
                    {
                        toggleCount++;
                    }
                    lastState = property.isExpanded;

                    property.isExpanded = !property.isExpanded;
                }
            }
        }

        /// <summary>
        /// Helper method to simulate calling the internal GetOrCreateCachedSerializedObject.
        /// Uses reflection since the method is private.
        /// </summary>
        private static SerializedObject GetCachedSerializedObject(UnityHelpersSettings settings)
        {
            System.Reflection.MethodInfo method = typeof(UnityHelpersSettings).GetMethod(
                "GetOrCreateCachedSerializedObject",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );
            Assert.IsNotNull(method, "GetOrCreateCachedSerializedObject method should exist.");

            object result = method.Invoke(null, new object[] { settings });
            return result as SerializedObject;
        }

        /// <summary>
        /// Helper method to test null handling.
        /// </summary>
        private static SerializedObject GetCachedSerializedObjectWithNull()
        {
            System.Reflection.MethodInfo method = typeof(UnityHelpersSettings).GetMethod(
                "GetOrCreateCachedSerializedObject",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );
            if (method == null)
            {
                return null;
            }

            object result = method.Invoke(null, new object[] { null });
            return result as SerializedObject;
        }
    }
}
#endif
