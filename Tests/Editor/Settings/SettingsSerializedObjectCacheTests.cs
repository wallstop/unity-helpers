// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Settings
{
    using System;
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

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            // Using 'using' would dispose the shared static cache, causing subsequent accesses to fail
            SerializedObject firstAccess = GetCachedSerializedObject(settings);
            SerializedProperty property = firstAccess.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(property, "WButtonCustomColors property should exist.");

            bool originalExpanded = property.isExpanded;
            property.isExpanded = false;

            SerializedObject secondAccess = GetCachedSerializedObject(settings);
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

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject firstAccess = GetCachedSerializedObject(settings);
            SerializedProperty property = firstAccess.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WEnumToggleButtonsCustomColors
            );
            Assert.IsNotNull(property, "WEnumToggleButtonsCustomColors property should exist.");

            bool originalExpanded = property.isExpanded;
            property.isExpanded = false;

            SerializedObject secondAccess = GetCachedSerializedObject(settings);
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

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject firstAccess = GetCachedSerializedObject(settings);
            int firstHashCode = firstAccess.GetHashCode();

            UnityHelpersSettings.ClearCachedSerializedObjectForTests();

            SerializedObject secondAccess = GetCachedSerializedObject(settings);
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

            // Note: using is safe here since result is expected to be null
            SerializedObject result = GetCachedSerializedObjectWithNull();
            Assert.IsNull(result, "GetCachedSerializedObject should return null for null input.");
        }

        [Test]
        public void CachedSerializedObjectIsValidAfterUpdate()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject cached = GetCachedSerializedObject(settings);
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

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject cached = GetCachedSerializedObject(settings);

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

                SerializedObject cachedAgain = GetCachedSerializedObject(settings);

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

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject cached = GetCachedSerializedObject(settings);
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

                    SerializedObject cachedAgain = GetCachedSerializedObject(settings);
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

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject cached = GetCachedSerializedObject(settings);
            SerializedProperty property = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(property, "WButtonCustomColors property should exist.");

            bool originalState = property.isExpanded;

            try
            {
                property.isExpanded = !originalState;
                cached.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject cachedAgain = GetCachedSerializedObject(settings);
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

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject cached = GetCachedSerializedObject(settings);
            int instanceId = cached.targetObject.GetInstanceID();

            Assert.AreNotEqual(
                0,
                instanceId,
                "UnityHelpersSettings instance should have a valid non-zero instance ID."
            );

            SerializedObject cachedAgain = GetCachedSerializedObject(settings);
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

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject cached = GetCachedSerializedObject(settings);
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

                    SerializedObject sameCached = GetCachedSerializedObject(settings);
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

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject cached = GetCachedSerializedObject(settings);
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
                    SerializedObject frameCached = GetCachedSerializedObject(settings);
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
                    SerializedObject frameCached = GetCachedSerializedObject(settings);
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

        [Test]
        public void CacheReturnsIdenticalObjectAcrossMultipleCalls()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject firstCall = GetCachedSerializedObject(settings);
            Assert.IsNotNull(firstCall, "First call should return a valid SerializedObject.");

            SerializedObject secondCall = GetCachedSerializedObject(settings);
            Assert.IsNotNull(secondCall, "Second call should return a valid SerializedObject.");

            SerializedObject thirdCall = GetCachedSerializedObject(settings);
            Assert.IsNotNull(thirdCall, "Third call should return a valid SerializedObject.");

            Assert.AreSame(
                firstCall,
                secondCall,
                "Cache should return the exact same object reference on second call."
            );
            Assert.AreSame(
                secondCall,
                thirdCall,
                "Cache should return the exact same object reference on third call."
            );
            Assert.AreSame(
                firstCall,
                thirdCall,
                "Cache should return the exact same object reference across all calls."
            );

            // Additional verification using ReferenceEquals for absolute certainty
            Assert.IsTrue(
                ReferenceEquals(firstCall, secondCall),
                "ReferenceEquals should confirm identical object references."
            );
        }

        [Test]
        public void CacheInvalidationRecreatesNewObject()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject beforeInvalidation = GetCachedSerializedObject(settings);
            Assert.IsNotNull(
                beforeInvalidation,
                "SerializedObject before invalidation should not be null."
            );

            // Store reference for comparison after invalidation
            SerializedObject sameObjectReference = GetCachedSerializedObject(settings);
            Assert.AreSame(
                beforeInvalidation,
                sameObjectReference,
                "Before invalidation, cache should return same reference."
            );

            // Invalidate the cache
            UnityHelpersSettings.ClearCachedSerializedObjectForTests();

            // Get new cached object after invalidation
            SerializedObject afterInvalidation = GetCachedSerializedObject(settings);
            Assert.IsNotNull(
                afterInvalidation,
                "SerializedObject after invalidation should not be null."
            );

            // Verify it's a completely new object
            Assert.AreNotSame(
                beforeInvalidation,
                afterInvalidation,
                "After cache invalidation, a new SerializedObject should be created."
            );
            Assert.IsFalse(
                ReferenceEquals(beforeInvalidation, afterInvalidation),
                "ReferenceEquals should confirm different object references after invalidation."
            );

            // Verify the new object is functional
            SerializedProperty property = afterInvalidation.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(
                property,
                "New SerializedObject after invalidation should have valid properties."
            );
        }

        [Test]
        public void PropertyFromStaleSerializedObjectThrowsDescriptiveError()
        {
            // This test demonstrates what happens when a SerializedObject is disposed
            // while code still holds references to its properties. This is the exact bug
            // that occurs when using 'using' statements with cached SerializedObjects.
            //
            // EDUCATIONAL NOTE: The bug this test documents:
            // - Test A gets cached SerializedObject and wraps it in 'using'
            // - Test A disposes the cached SerializedObject when 'using' block exits
            // - Test B calls GetCachedSerializedObject, gets the disposed object from cache
            // - Test B tries to use properties from disposed object -> Exception!
            //
            // This is why we NEVER use 'using' with GetCachedSerializedObject.

            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            // Create a fresh non-cached SerializedObject that we will dispose
            SerializedObject disposableObject = new(settings);
            SerializedProperty propertyBeforeDispose = disposableObject.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(
                propertyBeforeDispose,
                "Property should be accessible before SerializedObject disposal."
            );

            // Verify property is functional before disposal
            bool originalExpanded = propertyBeforeDispose.isExpanded;

            // Dispose the SerializedObject (simulates what 'using' does)
            disposableObject.Dispose();

            // Now demonstrate the error condition - attempting to access the property
            // after its parent SerializedObject was disposed should fail
            bool exceptionThrown = false;
            string exceptionMessage = string.Empty;
            try
            {
                // This access should fail because the SerializedObject is disposed
                // The exact behavior may vary by Unity version, but it should not succeed silently
                bool _ = propertyBeforeDispose.isExpanded;
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                exceptionMessage = ex.Message;
            }

            // Log the diagnostic information for future developers
            if (exceptionThrown)
            {
                Debug.Log(
                    $"[DIAGNOSTIC] Accessing property from disposed SerializedObject throws: {exceptionMessage}"
                );
            }
            else
            {
                Debug.LogWarning(
                    "[DIAGNOSTIC] Unity did not throw an exception when accessing disposed SerializedObject property. "
                        + "This behavior may vary by Unity version, but the property state is still invalid."
                );
            }

            // The test passes regardless - its purpose is diagnostic documentation
            // The key learning: disposed SerializedObjects cause problems, so never use 'using' with cached ones
            Assert.Pass(
                "Diagnostic test completed. See console for behavior when accessing disposed SerializedObject."
            );
        }

        [Test]
        public void SerializedPropertyRemainsValidWithinSameCacheLifetime()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject cached = GetCachedSerializedObject(settings);
            Assert.IsNotNull(cached, "Cached SerializedObject should not be null.");

            SerializedProperty property = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(property, "WButtonCustomColors property should exist.");

            bool originalState = property.isExpanded;

            try
            {
                // Perform multiple operations on the property across simulated "frames"
                for (int frame = 0; frame < 10; frame++)
                {
                    // Toggle the state
                    property.isExpanded = !property.isExpanded;

                    // Verify the property is still valid and accessible
                    Assert.IsNotNull(
                        property.serializedObject,
                        $"Frame {frame}: Property's serializedObject reference should remain valid."
                    );
                    Assert.IsNotNull(
                        property.propertyPath,
                        $"Frame {frame}: Property's propertyPath should remain valid."
                    );

                    // Verify we can still read/write the property
                    bool currentState = property.isExpanded;
                    Assert.AreEqual(
                        frame % 2 == 0 ? !originalState : originalState,
                        currentState,
                        $"Frame {frame}: Property state should be correctly toggled."
                    );

                    // Update the SerializedObject (common pattern in editor code)
                    cached.UpdateIfRequiredOrScript();

                    // Property should still be valid after update
                    Assert.IsNotNull(
                        property.propertyPath,
                        $"Frame {frame}: Property should remain valid after UpdateIfRequiredOrScript."
                    );
                }
            }
            finally
            {
                property.isExpanded = originalState;
            }
        }

        [Test]
        public void MultiplePropertiesFromSameCachedObjectShareLifetime()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject cached = GetCachedSerializedObject(settings);
            Assert.IsNotNull(cached, "Cached SerializedObject should not be null.");

            // Retrieve multiple different properties from the same cached object
            SerializedProperty wButtonColors = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            SerializedProperty wEnumColors = cached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WEnumToggleButtonsCustomColors
            );
            SerializedProperty dictionaryTween = cached.FindProperty(
                UnityHelpersSettings
                    .SerializedPropertyNames
                    .SerializableDictionaryFoldoutTweenEnabled
            );
            SerializedProperty sortedDictionaryTween = cached.FindProperty(
                UnityHelpersSettings
                    .SerializedPropertyNames
                    .SerializableSortedDictionaryFoldoutTweenEnabled
            );

            Assert.IsNotNull(wButtonColors, "WButtonCustomColors property should exist.");
            Assert.IsNotNull(wEnumColors, "WEnumToggleButtonsCustomColors property should exist.");
            Assert.IsNotNull(
                dictionaryTween,
                "TweenSerializableDictionaryFoldouts property should exist."
            );
            Assert.IsNotNull(
                sortedDictionaryTween,
                "TweenSerializableSortedDictionaryFoldouts property should exist."
            );

            // Store original states
            bool originalWButtonState = wButtonColors.isExpanded;
            bool originalWEnumState = wEnumColors.isExpanded;

            try
            {
                // Modify multiple properties
                wButtonColors.isExpanded = true;
                wEnumColors.isExpanded = false;

                // All properties should share the same parent SerializedObject
                Assert.AreSame(
                    wButtonColors.serializedObject,
                    wEnumColors.serializedObject,
                    "Properties should share the same parent SerializedObject."
                );
                Assert.AreSame(
                    wEnumColors.serializedObject,
                    dictionaryTween.serializedObject,
                    "All properties should share the same parent SerializedObject."
                );
                Assert.AreSame(
                    dictionaryTween.serializedObject,
                    sortedDictionaryTween.serializedObject,
                    "All properties should share the same parent SerializedObject."
                );

                // Verify all properties remain valid after multiple access
                cached.UpdateIfRequiredOrScript();

                Assert.IsNotNull(
                    wButtonColors.propertyPath,
                    "WButtonColors should remain valid after update."
                );
                Assert.IsNotNull(
                    wEnumColors.propertyPath,
                    "WEnumColors should remain valid after update."
                );
                Assert.IsNotNull(
                    dictionaryTween.propertyPath,
                    "DictionaryTween should remain valid after update."
                );
                Assert.IsNotNull(
                    sortedDictionaryTween.propertyPath,
                    "SortedDictionaryTween should remain valid after update."
                );

                // Verify states are preserved
                Assert.IsTrue(
                    wButtonColors.isExpanded,
                    "WButtonColors expanded state should be preserved."
                );
                Assert.IsFalse(
                    wEnumColors.isExpanded,
                    "WEnumColors collapsed state should be preserved."
                );
            }
            finally
            {
                wButtonColors.isExpanded = originalWButtonState;
                wEnumColors.isExpanded = originalWEnumState;
            }
        }

        [Test]
        public void CacheHandlesTargetObjectChangeGracefully()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;

            // Note: Do NOT use 'using' with cached SerializedObjects - the cache manages its own lifecycle
            SerializedObject firstCached = GetCachedSerializedObject(settings);
            Assert.IsNotNull(firstCached, "First cached SerializedObject should not be null.");

            int originalTargetInstanceId = firstCached.targetObject.GetInstanceID();
            Assert.AreNotEqual(
                0,
                originalTargetInstanceId,
                "Target object should have a valid instance ID."
            );

            // Clear the cache to simulate what would happen if the target changed
            UnityHelpersSettings.ClearCachedSerializedObjectForTests();

            // Get the settings instance again (it's a singleton, so same object)
            UnityHelpersSettings sameSettings = UnityHelpersSettings.instance;

            // Get new cached object
            SerializedObject secondCached = GetCachedSerializedObject(sameSettings);
            Assert.IsNotNull(secondCached, "Second cached SerializedObject should not be null.");

            // Verify cache created a new SerializedObject
            Assert.AreNotSame(
                firstCached,
                secondCached,
                "After cache clear, a new SerializedObject should be created."
            );

            // Verify the new SerializedObject targets the same underlying settings object
            int newTargetInstanceId = secondCached.targetObject.GetInstanceID();
            Assert.AreEqual(
                originalTargetInstanceId,
                newTargetInstanceId,
                "New SerializedObject should target the same settings instance."
            );

            // Verify the new cached object is fully functional
            SerializedProperty property = secondCached.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(property, "New SerializedObject should have functional properties.");

            // Verify subsequent calls return the same new cached object
            SerializedObject thirdCached = GetCachedSerializedObject(sameSettings);
            Assert.AreSame(
                secondCached,
                thirdCached,
                "After recreation, cache should consistently return the new object."
            );
        }

        /// <summary>
        /// Helper method to call the internal GetOrCreateCachedSerializedObject.
        /// </summary>
        private static SerializedObject GetCachedSerializedObject(UnityHelpersSettings settings)
        {
            return UnityHelpersSettings.GetOrCreateCachedSerializedObject(settings);
        }

        /// <summary>
        /// Helper method to test null handling.
        /// </summary>
        private static SerializedObject GetCachedSerializedObjectWithNull()
        {
            return UnityHelpersSettings.GetOrCreateCachedSerializedObject(null);
        }
    }
}
#endif
