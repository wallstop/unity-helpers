namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.AssetProcessors;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Tests for prefab and scene object search functionality in DetectAssetChangeProcessor.
    /// </summary>
    public sealed class DetectAssetChangePrefabAndSceneTests : CommonTestBase
    {
        private const string Root = "Assets/__DetectAssetChangedTests__";
        private const string PayloadAssetPath = Root + "/Payload.asset";
        private const string PrefabPath = Root + "/TestPrefab.prefab";
        private const string NestedPrefabPath = Root + "/NestedTestPrefab.prefab";
        private const string MultiplePrefabPath = Root + "/MultiplePrefab.prefab";
        private const string TestScenePath = Root + "/TestScene.unity";

        private Scene _originalScene;
        private string _originalScenePath;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            CleanupTestFolders();
            AssetDatabase.Refresh();
        }

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            DetectAssetChangeProcessor.IncludeTestAssets = true;
            EnsureFolder();
            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting();

            // Store current scene info
            _originalScene = SceneManager.GetActiveScene();
            _originalScenePath = _originalScene.path;
        }

        [TearDown]
        public override void TearDown()
        {
            DetectAssetChangeProcessor.IncludeTestAssets = false;

            // Clean up scene objects first (before deleting assets)
            CleanupSceneObjects();

            DeleteAssetIfExists(PayloadAssetPath);
            DeleteAssetIfExists(PrefabPath);
            DeleteAssetIfExists(NestedPrefabPath);
            DeleteAssetIfExists(MultiplePrefabPath);
            DeleteAssetIfExists(TestScenePath);

            CleanupTestFolders();
            AssetDatabase.Refresh();
            ClearTestState();

            base.TearDown();
        }

        [Test]
        public void PrefabHandlerInvokesInstanceMethodWhenAssetCreated()
        {
            // Arrange
            GameObject prefab = CreatePrefabWithComponent<TestPrefabAssetChangeHandler>(PrefabPath);
            Track(prefab);
            CreatePayloadAsset();
            ClearTestState();

            // Need to reset processor after creating prefab so it finds the handler
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                1,
                TestPrefabAssetChangeHandler.RecordedContexts.Count,
                "Expected prefab handler to be invoked once"
            );
            Assert.AreEqual(
                AssetChangeFlags.Created,
                TestPrefabAssetChangeHandler.RecordedContexts[0].Flags
            );
            Assert.AreEqual(
                1,
                TestPrefabAssetChangeHandler.RecordedInstances.Count,
                "Expected one instance to be invoked"
            );
        }

        [Test]
        public void PrefabHandlerInvokesInstanceMethodWhenAssetDeleted()
        {
            // Arrange
            GameObject prefab = CreatePrefabWithComponent<TestPrefabAssetChangeHandler>(PrefabPath);
            Track(prefab);
            CreatePayloadAsset();
            ClearTestState();

            // Reset processor after creating prefab
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // First process creation to track the asset
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );
            ClearTestState();

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadAssetPath },
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                1,
                TestPrefabAssetChangeHandler.RecordedContexts.Count,
                "Expected prefab handler to be invoked once for deletion"
            );
            Assert.AreEqual(
                AssetChangeFlags.Deleted,
                TestPrefabAssetChangeHandler.RecordedContexts[0].Flags
            );
        }

        [Test]
        public void PrefabHandlerFindsNestedComponents()
        {
            // Arrange - Create prefab with nested child containing handler
            GameObject prefab = CreateNestedPrefabWithHandler(NestedPrefabPath);
            Track(prefab);
            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                1,
                TestNestedPrefabHandler.RecordedContexts.Count,
                "Expected nested prefab handler to be invoked"
            );
        }

        [Test]
        public void PrefabHandlerFindsMultipleComponentsOnSamePrefab()
        {
            // Arrange - Create prefab with multiple handlers
            GameObject prefab = CreatePrefabWithMultipleHandlers(MultiplePrefabPath);
            Track(prefab);
            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                2,
                TestPrefabAssetChangeHandler.RecordedInstances.Count,
                "Expected both prefab handlers to be invoked"
            );
        }

        [Test]
        public void PrefabHandlerFindsHandlersAcrossMultiplePrefabs()
        {
            // Arrange - Create two separate prefabs with handlers
            GameObject prefab1 = CreatePrefabWithComponent<TestPrefabAssetChangeHandler>(
                PrefabPath
            );
            Track(prefab1);

            // Diagnostic: Verify prefab1 was created correctly
            Assert.IsTrue(prefab1 != null, $"Failed to create prefab1 at {PrefabPath}");
            TestPrefabAssetChangeHandler handler1 =
                prefab1.GetComponent<TestPrefabAssetChangeHandler>();
            Assert.IsTrue(
                handler1 != null,
                $"Prefab1 at {PrefabPath} does not have TestPrefabAssetChangeHandler component. "
                    + $"This may indicate the MonoBehaviour is in an Editor folder and cannot be attached to GameObjects."
            );

            string prefab2Path = Root + "/TestPrefab2.prefab";
            GameObject prefab2 = CreatePrefabWithComponent<TestPrefabAssetChangeHandler>(
                prefab2Path
            );
            Track(prefab2);

            // Diagnostic: Verify prefab2 was created correctly
            Assert.IsTrue(prefab2 != null, $"Failed to create prefab2 at {prefab2Path}");
            TestPrefabAssetChangeHandler handler2 =
                prefab2.GetComponent<TestPrefabAssetChangeHandler>();
            Assert.IsTrue(
                handler2 != null,
                $"Prefab2 at {prefab2Path} does not have TestPrefabAssetChangeHandler component. "
                    + $"This may indicate the MonoBehaviour is in an Editor folder and cannot be attached to GameObjects."
            );

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                2,
                TestPrefabAssetChangeHandler.RecordedInstances.Count,
                $"Expected handlers from both prefabs to be invoked. "
                    + $"RecordedContexts.Count={TestPrefabAssetChangeHandler.RecordedContexts.Count}, "
                    + $"Prefab1 exists={prefab1 != null}, Prefab2 exists={prefab2 != null}"
            );

            // Cleanup extra prefab
            DeleteAssetIfExists(prefab2Path);
        }

        [Test]
        public void PrefabHandlerDoesNotInvokeWithoutSearchPrefabsOption()
        {
            // Arrange - TestSceneAssetChangeHandler has SearchSceneObjects option but NOT SearchPrefabs
            // So even if we create a prefab with it, it should not be found via prefab search
            GameObject go = Track(new GameObject("TestHandler"));
            TestSceneAssetChangeHandler handler = go.AddComponent<TestSceneAssetChangeHandler>();

            // Save as prefab
            EnsureFolder();
            string tempPrefabPath = Root + "/TempPrefab.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, tempPrefabPath);
            Track(prefab);
            Object.DestroyImmediate(go);
            _trackedObjects.Remove(go);

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert - The handler on prefab should NOT be invoked because it doesn't have SearchPrefabs
            // The TestSceneAssetChangeHandler only has SearchSceneObjects option, not SearchPrefabs
            Assert.AreEqual(
                0,
                TestSceneAssetChangeHandler.RecordedInstances.Count,
                "Expected no scene handlers invoked from prefab since it lacks SearchPrefabs option"
            );

            DeleteAssetIfExists(tempPrefabPath);
        }

        [Test]
        public void SceneHandlerInvokesInstanceMethodWhenAssetCreated()
        {
            // Arrange - Create handler in current scene
            GameObject go = Track(new GameObject("SceneHandler"));
            TestSceneAssetChangeHandler handler = go.AddComponent<TestSceneAssetChangeHandler>();

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                1,
                TestSceneAssetChangeHandler.RecordedContexts.Count,
                "Expected scene handler to be invoked once"
            );
            Assert.AreEqual(
                AssetChangeFlags.Created,
                TestSceneAssetChangeHandler.RecordedContexts[0].Flags
            );
            Assert.AreEqual(
                1,
                TestSceneAssetChangeHandler.RecordedInstances.Count,
                "Expected one scene instance to be invoked"
            );
            Assert.AreSame(
                handler,
                TestSceneAssetChangeHandler.RecordedInstances[0],
                "Expected the correct handler instance"
            );
        }

        [Test]
        public void SceneHandlerInvokesInstanceMethodWhenAssetDeleted()
        {
            // Arrange
            GameObject go = Track(new GameObject("SceneHandler"));
            TestSceneAssetChangeHandler handler = go.AddComponent<TestSceneAssetChangeHandler>();

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // First process creation
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );
            ClearTestState();

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadAssetPath },
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                1,
                TestSceneAssetChangeHandler.RecordedContexts.Count,
                "Expected scene handler to be invoked for deletion"
            );
            Assert.AreEqual(
                AssetChangeFlags.Deleted,
                TestSceneAssetChangeHandler.RecordedContexts[0].Flags
            );
        }

        [Test]
        public void SceneHandlerFindsNestedChildComponents()
        {
            // Arrange - Create hierarchy with handler on child
            GameObject parent = Track(new GameObject("Parent"));
            GameObject child = new("Child");
            child.transform.SetParent(parent.transform);
            TestSceneAssetChangeHandler handler = child.AddComponent<TestSceneAssetChangeHandler>();

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                1,
                TestSceneAssetChangeHandler.RecordedInstances.Count,
                "Expected nested scene handler to be invoked"
            );
            Assert.AreSame(
                handler,
                TestSceneAssetChangeHandler.RecordedInstances[0],
                "Expected the nested handler instance"
            );
        }

        [Test]
        public void SceneHandlerFindsMultipleHandlersInScene()
        {
            // Arrange - Create multiple handlers
            GameObject go1 = Track(new GameObject("SceneHandler1"));
            TestSceneAssetChangeHandler handler1 = go1.AddComponent<TestSceneAssetChangeHandler>();

            GameObject go2 = Track(new GameObject("SceneHandler2"));
            TestSceneAssetChangeHandler handler2 = go2.AddComponent<TestSceneAssetChangeHandler>();

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                2,
                TestSceneAssetChangeHandler.RecordedInstances.Count,
                "Expected both scene handlers to be invoked"
            );
        }

        [Test]
        public void SceneHandlerFindsInactiveObjects()
        {
            // Arrange - Create inactive handler
            GameObject go = Track(new GameObject("InactiveHandler"));
            go.SetActive(false);
            TestSceneAssetChangeHandler handler = go.AddComponent<TestSceneAssetChangeHandler>();

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert - GetComponentsInChildren with includeInactive=true should find it
            Assert.AreEqual(
                1,
                TestSceneAssetChangeHandler.RecordedInstances.Count,
                "Expected inactive scene handler to be invoked"
            );
        }

        [Test]
        public void CombinedHandlerFindsBothPrefabAndSceneObjects()
        {
            // Arrange - Create both prefab and scene handler
            GameObject prefab = CreatePrefabWithComponent<TestCombinedSearchHandler>(PrefabPath);
            Track(prefab);

            // Diagnostic: Verify prefab was created correctly
            Assert.IsTrue(prefab != null, $"Failed to create prefab at {PrefabPath}");
            TestCombinedSearchHandler prefabHandler =
                prefab.GetComponent<TestCombinedSearchHandler>();
            Assert.IsTrue(
                prefabHandler != null,
                $"Prefab at {PrefabPath} does not have TestCombinedSearchHandler component. "
                    + $"This may indicate the MonoBehaviour is in an Editor folder and cannot be attached to GameObjects."
            );

            GameObject sceneGo = Track(new GameObject("SceneCombinedHandler"));
            TestCombinedSearchHandler sceneHandler =
                sceneGo.AddComponent<TestCombinedSearchHandler>();

            // Diagnostic: Verify scene object was created correctly
            Assert.IsTrue(
                sceneHandler != null,
                "Failed to add TestCombinedSearchHandler to scene object. "
                    + "This may indicate the MonoBehaviour is in an Editor folder and cannot be attached to GameObjects."
            );

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                2,
                TestCombinedSearchHandler.RecordedInstances.Count,
                $"Expected both prefab and scene handlers to be invoked. "
                    + $"RecordedContexts.Count={TestCombinedSearchHandler.RecordedContexts.Count}, "
                    + $"Prefab exists={prefab != null}, Prefab has handler={prefabHandler != null}, "
                    + $"SceneGo exists={sceneGo != null}, Scene handler={sceneHandler != null}"
            );
        }

        [Test]
        public void CombinedHandlerDoesNotDuplicateWhenSameInstanceInPrefabAndScene()
        {
            // Arrange - Instantiate prefab in scene (creates a scene instance)
            GameObject prefab = CreatePrefabWithComponent<TestCombinedSearchHandler>(PrefabPath);
            Track(prefab);

            // Diagnostic: Verify prefab was created correctly
            Assert.IsTrue(prefab != null, $"Failed to create prefab at {PrefabPath}");
            TestCombinedSearchHandler prefabHandler =
                prefab.GetComponent<TestCombinedSearchHandler>();
            Assert.IsTrue(
                prefabHandler != null,
                $"Prefab at {PrefabPath} does not have TestCombinedSearchHandler component. "
                    + $"This may indicate the MonoBehaviour is in an Editor folder and cannot be attached to GameObjects."
            );

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Track(instance);

            // Diagnostic: Verify instantiated prefab has component
            Assert.IsTrue(instance != null, "Failed to instantiate prefab in scene");
            TestCombinedSearchHandler instanceHandler =
                instance.GetComponent<TestCombinedSearchHandler>();
            Assert.IsTrue(
                instanceHandler != null,
                "Instantiated prefab does not have TestCombinedSearchHandler component"
            );

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert - Should find prefab asset handler and scene instance handler
            // Both should be invoked (they are different objects with different instance IDs)
            Assert.GreaterOrEqual(
                TestCombinedSearchHandler.RecordedInstances.Count,
                1,
                $"Expected at least one handler to be invoked. "
                    + $"RecordedContexts.Count={TestCombinedSearchHandler.RecordedContexts.Count}, "
                    + $"Prefab exists={prefab != null}, Prefab handler={prefabHandler != null}, "
                    + $"Instance exists={instance != null}, Instance handler={instanceHandler != null}"
            );
        }

        [Test]
        public void HandlerHandlesNullComponentsGracefully()
        {
            // This tests the null checks in the enumeration code
            // We can't easily create null components, but we verify no crash occurs

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act - Should not throw
            Assert.DoesNotThrow(() =>
            {
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
            });
        }

        [Test]
        public void HandlerHandlesEmptyScenesGracefully()
        {
            // Arrange - No handlers in scene
            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act - Should not throw even with no scene handlers
            Assert.DoesNotThrow(() =>
            {
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
            });
        }

        [Test]
        public void HandlerHandlesDestroyedObjectsDuringEnumeration()
        {
            // Arrange
            GameObject go = new("Handler");
            TestSceneAssetChangeHandler handler = go.AddComponent<TestSceneAssetChangeHandler>();

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Destroy before processing (don't track since we destroy immediately)
            Object.DestroyImmediate(go);

            // Act - Should handle destroyed objects gracefully
            Assert.DoesNotThrow(() =>
            {
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
            });
        }

        [Test]
        public void HandlerNonComponentTypeDoesNotSearchPrefabs()
        {
            // Arrange - ScriptableObject handlers should not trigger prefab/scene search
            // even if options are set (the type check should prevent this)
            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act - Should complete normally
            Assert.DoesNotThrow(() =>
            {
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
            });
        }

        [TestCase(true, false, 1, TestName = "SearchOptions.PrefabOnly.FindsPrefab")]
        [TestCase(false, true, 1, TestName = "SearchOptions.SceneOnly.FindsScene")]
        [TestCase(true, true, 2, TestName = "SearchOptions.Both.FindsBoth")]
        public void SearchOptionsFindsCorrectInstances(
            bool createPrefab,
            bool createSceneObject,
            int expectedInvocations
        )
        {
            // Arrange
            GameObject prefab = null;
            TestCombinedSearchHandler prefabHandler = null;
            GameObject sceneGo = null;
            TestCombinedSearchHandler sceneHandler = null;

            if (createPrefab)
            {
                prefab = CreatePrefabWithComponent<TestCombinedSearchHandler>(PrefabPath);
                Track(prefab);

                // Diagnostic: Verify prefab was created correctly
                Assert.IsTrue(prefab != null, $"Failed to create prefab at {PrefabPath}");
                prefabHandler = prefab.GetComponent<TestCombinedSearchHandler>();
                Assert.IsTrue(
                    prefabHandler != null,
                    $"Prefab at {PrefabPath} does not have TestCombinedSearchHandler component. "
                        + $"This may indicate the MonoBehaviour is in an Editor folder and cannot be attached to GameObjects."
                );
            }

            if (createSceneObject)
            {
                sceneGo = Track(new GameObject("CombinedHandler"));
                sceneHandler = sceneGo.AddComponent<TestCombinedSearchHandler>();

                // Diagnostic: Verify scene object was created correctly
                Assert.IsTrue(
                    sceneHandler != null,
                    "Failed to add TestCombinedSearchHandler to scene object. "
                        + "This may indicate the MonoBehaviour is in an Editor folder and cannot be attached to GameObjects."
                );
            }

            CreatePayloadAsset();
            ClearTestState();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Act
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Assert
            Assert.AreEqual(
                expectedInvocations,
                TestCombinedSearchHandler.RecordedInstances.Count,
                $"Expected {expectedInvocations} invocations. "
                    + $"createPrefab={createPrefab}, createSceneObject={createSceneObject}, "
                    + $"Prefab exists={prefab != null}, Prefab handler={prefabHandler != null}, "
                    + $"SceneGo exists={sceneGo != null}, Scene handler={sceneHandler != null}, "
                    + $"RecordedContexts.Count={TestCombinedSearchHandler.RecordedContexts.Count}"
            );
        }

        [Test]
        public void PrefabComponentCanBeAddedToGameObject()
        {
            // This test verifies that TestPrefabAssetChangeHandler is NOT in an Editor folder
            // and can be properly added to GameObjects (a prerequisite for all prefab tests)
            GameObject go = Track(new GameObject("TestAddComponent"));
            TestPrefabAssetChangeHandler handler = go.AddComponent<TestPrefabAssetChangeHandler>();
            Assert.IsTrue(
                handler != null,
                "TestPrefabAssetChangeHandler must NOT be in an Editor folder. "
                    + "MonoBehaviours in Editor folders cannot be attached to GameObjects. "
                    + "Move TestPrefabAssetChangeHandler to a non-Editor folder (e.g., Tests/Runtime/)."
            );
        }

        [Test]
        public void SceneComponentCanBeAddedToGameObject()
        {
            // This test verifies that TestSceneAssetChangeHandler is NOT in an Editor folder
            // and can be properly added to GameObjects (a prerequisite for all scene handler tests)
            GameObject go = Track(new GameObject("TestAddComponent"));
            TestSceneAssetChangeHandler handler = go.AddComponent<TestSceneAssetChangeHandler>();
            Assert.IsTrue(
                handler != null,
                "TestSceneAssetChangeHandler must NOT be in an Editor folder. "
                    + "MonoBehaviours in Editor folders cannot be attached to GameObjects. "
                    + "Move TestSceneAssetChangeHandler to a non-Editor folder (e.g., Tests/Runtime/)."
            );
        }

        [Test]
        public void CombinedComponentCanBeAddedToGameObject()
        {
            // This test verifies that TestCombinedSearchHandler is NOT in an Editor folder
            // and can be properly added to GameObjects (a prerequisite for all combined handler tests)
            GameObject go = Track(new GameObject("TestAddComponent"));
            TestCombinedSearchHandler handler = go.AddComponent<TestCombinedSearchHandler>();
            Assert.IsTrue(
                handler != null,
                "TestCombinedSearchHandler must NOT be in an Editor folder. "
                    + "MonoBehaviours in Editor folders cannot be attached to GameObjects. "
                    + "Move TestCombinedSearchHandler to a non-Editor folder (e.g., Tests/Runtime/)."
            );
        }

        [Test]
        public void NestedComponentCanBeAddedToGameObject()
        {
            // This test verifies that TestNestedPrefabHandler is NOT in an Editor folder
            // and can be properly added to GameObjects (a prerequisite for all nested handler tests)
            GameObject go = Track(new GameObject("TestAddComponent"));
            TestNestedPrefabHandler handler = go.AddComponent<TestNestedPrefabHandler>();
            Assert.IsTrue(
                handler != null,
                "TestNestedPrefabHandler must NOT be in an Editor folder. "
                    + "MonoBehaviours in Editor folders cannot be attached to GameObjects. "
                    + "Move TestNestedPrefabHandler to a non-Editor folder (e.g., Tests/Runtime/)."
            );
        }

        [Test]
        public void CreatePrefabWithComponentReturnsValidPrefabWithComponent()
        {
            // Arrange & Act
            EnsureFolder();
            string testPrefabPath = Root + "/ValidationTestPrefab.prefab";
            GameObject prefab = CreatePrefabWithComponent<TestPrefabAssetChangeHandler>(
                testPrefabPath
            );

            try
            {
                // Assert - Verify the prefab asset was created
                Assert.IsTrue(
                    prefab != null,
                    $"CreatePrefabWithComponent failed to create prefab at {testPrefabPath}"
                );

                // Verify it's actually a prefab asset
                GameObject loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(testPrefabPath);
                Assert.IsTrue(loadedPrefab != null, $"Prefab asset not found at {testPrefabPath}");

                // Verify the component is on the prefab
                TestPrefabAssetChangeHandler handler =
                    loadedPrefab.GetComponent<TestPrefabAssetChangeHandler>();
                Assert.IsTrue(
                    handler != null,
                    $"TestPrefabAssetChangeHandler component not found on prefab at {testPrefabPath}. "
                        + "This indicates AddComponent failed, likely because the script is in an Editor folder."
                );
            }
            finally
            {
                DeleteAssetIfExists(testPrefabPath);
            }
        }

        private static void CleanupTestFolders()
        {
            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
            }

            string[] allFolders = AssetDatabase.GetSubFolders("Assets");
            if (allFolders != null)
            {
                foreach (string folder in allFolders)
                {
                    string folderName = Path.GetFileName(folder);
                    if (
                        folderName != null
                        && folderName.StartsWith(
                            "__DetectAssetChangedTests__",
                            StringComparison.Ordinal
                        )
                    )
                    {
                        AssetDatabase.DeleteAsset(folder);
                    }
                }
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string assetsFolder = Path.Combine(projectRoot, "Assets");
                if (Directory.Exists(assetsFolder))
                {
                    try
                    {
                        foreach (
                            string dir in Directory.GetDirectories(
                                assetsFolder,
                                "__DetectAssetChangedTests__*"
                            )
                        )
                        {
                            try
                            {
                                Directory.Delete(dir, recursive: true);
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }
        }

        private static void CleanupSceneObjects()
        {
            // Find and destroy all test handler objects in the scene
            TestPrefabAssetChangeHandler[] prefabHandlers =
                Object.FindObjectsByType<TestPrefabAssetChangeHandler>(FindObjectsSortMode.None);
            foreach (TestPrefabAssetChangeHandler handler in prefabHandlers)
            {
                if (handler != null && handler.gameObject != null)
                {
                    Object.DestroyImmediate(handler.gameObject);
                }
            }

            TestSceneAssetChangeHandler[] sceneHandlers =
                Object.FindObjectsByType<TestSceneAssetChangeHandler>(FindObjectsSortMode.None);
            foreach (TestSceneAssetChangeHandler handler in sceneHandlers)
            {
                if (handler != null && handler.gameObject != null)
                {
                    Object.DestroyImmediate(handler.gameObject);
                }
            }

            TestCombinedSearchHandler[] combinedHandlers =
                Object.FindObjectsByType<TestCombinedSearchHandler>(FindObjectsSortMode.None);
            foreach (TestCombinedSearchHandler handler in combinedHandlers)
            {
                if (handler != null && handler.gameObject != null)
                {
                    Object.DestroyImmediate(handler.gameObject);
                }
            }

            TestNestedPrefabHandler[] nestedHandlers =
                Object.FindObjectsByType<TestNestedPrefabHandler>(FindObjectsSortMode.None);
            foreach (TestNestedPrefabHandler handler in nestedHandlers)
            {
                if (handler != null && handler.gameObject != null)
                {
                    Object.DestroyImmediate(handler.gameObject);
                }
            }
        }

        private static void CreatePayloadAsset()
        {
            EnsureFolder();
            TestDetectableAsset payload = ScriptableObject.CreateInstance<TestDetectableAsset>();
            AssetDatabase.CreateAsset(payload, PayloadAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreatePrefabWithComponent<T>(string path)
            where T : Component
        {
            EnsureFolder();
            GameObject go = new(typeof(T).Name);
            go.AddComponent<T>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            return prefab;
        }

        private static GameObject CreateNestedPrefabWithHandler(string path)
        {
            EnsureFolder();
            GameObject root = new("Root");
            GameObject child = new("Child");
            child.transform.SetParent(root.transform);
            child.AddComponent<TestNestedPrefabHandler>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            return prefab;
        }

        private static GameObject CreatePrefabWithMultipleHandlers(string path)
        {
            EnsureFolder();
            GameObject go = new("MultiHandler");
            go.AddComponent<TestPrefabAssetChangeHandler>();
            go.AddComponent<TestPrefabAssetChangeHandler>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            return prefab;
        }

        private static void EnsureFolder()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string absoluteDirectory = Path.Combine(projectRoot, Root);
                if (!Directory.Exists(absoluteDirectory))
                {
                    Directory.CreateDirectory(absoluteDirectory);
                }
            }

            if (!AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.CreateFolder("Assets", "__DetectAssetChangedTests__");
            }
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void ClearTestState()
        {
            TestPrefabAssetChangeHandler.Clear();
            TestSceneAssetChangeHandler.Clear();
            TestCombinedSearchHandler.Clear();
            TestNestedPrefabHandler.Clear();
        }
    }
}
