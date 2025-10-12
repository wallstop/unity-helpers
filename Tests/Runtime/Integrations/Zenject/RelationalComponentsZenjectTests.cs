#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.Zenject
{
    using System;
    using System.Collections.Generic;
    using global::Zenject;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;
    using WallstopStudios.UnityHelpers.Tags;

    public sealed class RelationalComponentsZenjectTests
    {
        private DiContainer Container;

        [SetUp]
        public void Setup()
        {
            Container = new DiContainer();
        }

        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void Cleanup()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_spawned[i]);
                }
            }
            _spawned.Clear();
        }

        [Test]
        public void ContainerExtensionsUseBoundAssigner()
        {
            RecordingAssigner assigner = new();
            Container.Bind<IRelationalComponentAssigner>().FromInstance(assigner);

            ZenjectRelationalTester tester = CreateHierarchy();

            Container.AssignRelationalComponents(tester);

            Assert.That(
                assigner.CallCount,
                Is.EqualTo(1),
                "Assigner should be called exactly once when bound"
            );
            Assert.That(
                assigner.LastComponent,
                Is.SameAs(tester),
                "Assigner should receive the same component instance"
            );
            Assert.That(
                tester.parentBody,
                Is.Not.Null,
                "ParentComponent assignment should set parentBody"
            );
            Assert.That(
                tester.childCollider,
                Is.Not.Null,
                "ChildComponent assignment should set childCollider"
            );
        }

        [Test]
        public void ContainerExtensionsFallBackWhenAssignerMissing()
        {
            ZenjectRelationalTester tester = CreateHierarchy();

            Container.AssignRelationalComponents(tester);

            Assert.That(
                tester.parentBody,
                Is.Not.Null,
                "Fallback should assign parentBody without a bound assigner"
            );
            Assert.That(
                tester.childCollider,
                Is.Not.Null,
                "Fallback should assign childCollider without a bound assigner"
            );
        }

        [UnityTest]
        public System.Collections.IEnumerator SceneInitializerAssignsActiveSceneComponents()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(ZenjectRelationalTester));
            try
            {
                Container.BindInstance(cache);
                RecordingAssigner assigner = new();
                Container.Bind<IRelationalComponentAssigner>().FromInstance(assigner);
                Container.BindInstance(RelationalSceneAssignmentOptions.Default);
                Container.BindInterfacesTo<RelationalComponentSceneInitializer>().AsSingle();

                Scene scene = SceneManager.CreateScene("ZenjectTestScene_Active");
                SceneManager.SetActiveScene(scene);
                ZenjectRelationalTester tester = CreateHierarchy();
                GameObject root = tester.transform.root.gameObject;
                SceneManager.MoveGameObjectToScene(root, scene);
                yield return null;

                IInitializable initializer = Container.Resolve<IInitializable>();
                initializer.Initialize();
                yield return null;

                Assert.That(
                    assigner.CallCount,
                    Is.EqualTo(1),
                    "Initializer should invoke assigner exactly once for the tester component"
                );
                Assert.That(
                    assigner.LastComponent,
                    Is.SameAs(tester),
                    "Initializer should target the created tester instance"
                );
            }
            finally
            {
                if (cache != null)
                {
                    UnityEngine.Object.DestroyImmediate(cache);
                }
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator SceneInitializerSkipsInactiveWhenOptionDisabled()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(ZenjectRelationalTester));
            try
            {
                Container.BindInstance(cache);
                RecordingAssigner assigner = new();
                Container.Bind<IRelationalComponentAssigner>().FromInstance(assigner);
                Container.BindInstance(new RelationalSceneAssignmentOptions(false));
                Container.BindInterfacesTo<RelationalComponentSceneInitializer>().AsSingle();

                Scene scene = SceneManager.CreateScene("ZenjectTestScene_InactiveFalse");
                SceneManager.SetActiveScene(scene);
                ZenjectRelationalTester tester = CreateHierarchy();
                tester.gameObject.SetActive(false);
                GameObject root = tester.transform.root.gameObject;
                SceneManager.MoveGameObjectToScene(root, scene);
                yield return null;

                IInitializable initializer = Container.Resolve<IInitializable>();
                initializer.Initialize();
                yield return null;

                Assert.That(
                    assigner.CallCount,
                    Is.EqualTo(0),
                    "Initializer should skip inactive tester when IncludeInactive is false"
                );
            }
            finally
            {
                if (cache != null)
                {
                    UnityEngine.Object.DestroyImmediate(cache);
                }
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator SceneInitializerIncludesInactiveWhenOptionEnabled()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(ZenjectRelationalTester));
            try
            {
                Container.BindInstance(cache);
                RecordingAssigner assigner = new();
                Container.Bind<IRelationalComponentAssigner>().FromInstance(assigner);
                Container.BindInstance(new RelationalSceneAssignmentOptions(true));
                Container.BindInterfacesTo<RelationalComponentSceneInitializer>().AsSingle();

                Scene scene = SceneManager.CreateScene("ZenjectTestScene_InactiveTrue");
                SceneManager.SetActiveScene(scene);
                ZenjectRelationalTester tester = CreateHierarchy();
                tester.gameObject.SetActive(false);
                GameObject root = tester.transform.root.gameObject;
                SceneManager.MoveGameObjectToScene(root, scene);
                yield return null;

                IInitializable initializer = Container.Resolve<IInitializable>();
                initializer.Initialize();
                yield return null;

                Assert.That(
                    assigner.CallCount,
                    Is.EqualTo(1),
                    "Initializer should include inactive tester when IncludeInactive is true"
                );
                Assert.That(
                    assigner.LastComponent,
                    Is.SameAs(tester),
                    "Initializer should target the inactive tester component"
                );
            }
            finally
            {
                if (cache != null)
                {
                    UnityEngine.Object.DestroyImmediate(cache);
                }
            }
        }

        [Test]
        public void ContainerAssignRelationalHierarchyAssignsFields()
        {
            ZenjectRelationalTester tester = CreateHierarchy();
            Container.AssignRelationalHierarchy(tester.gameObject, includeInactiveChildren: false);
            Assert.That(
                tester.parentBody,
                Is.Not.Null,
                "AssignRelationalHierarchy should assign parentBody"
            );
            Assert.That(
                tester.childCollider,
                Is.Not.Null,
                "AssignRelationalHierarchy should assign childCollider"
            );
        }

        [UnityTest]
        public System.Collections.IEnumerator SceneInitializerIgnoresNonActiveScenes()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(ZenjectRelationalTester));
            try
            {
                RecordingAssigner assigner = new();
                Container.BindInstance(cache);
                Container.Bind<IRelationalComponentAssigner>().FromInstance(assigner);
                Container.BindInstance(new RelationalSceneAssignmentOptions(true));
                Container.BindInterfacesTo<RelationalComponentSceneInitializer>().AsSingle();

                Scene active = SceneManager.CreateScene("ZenjectActiveScene_Sep");
                SceneManager.SetActiveScene(active);
                ZenjectRelationalTester testerA = CreateHierarchy();
                SceneManager.MoveGameObjectToScene(testerA.transform.root.gameObject, active);

                Scene secondary = SceneManager.CreateScene("ZenjectSecondaryScene_Sep");
                ZenjectRelationalTester testerB = CreateHierarchy();
                SceneManager.MoveGameObjectToScene(testerB.transform.root.gameObject, secondary);
                yield return null;

                IInitializable initializer = Container.Resolve<IInitializable>();
                initializer.Initialize();
                yield return null;

                Assert.That(
                    assigner.CallCount,
                    Is.EqualTo(1),
                    "Initializer should only process components from the active scene"
                );
                Assert.That(
                    assigner.LastComponent,
                    Is.SameAs(testerA),
                    "Active scene tester should be assigned"
                );
            }
            finally
            {
                if (cache != null)
                {
                    UnityEngine.Object.DestroyImmediate(cache);
                }
            }
        }

        [Test]
        public void ContainerAssignRelationalHierarchyRespectsIncludeInactiveChildren()
        {
            ZenjectRelationalTester rootTester = CreateHierarchy();

            // Create a second tester under an INACTIVE child to verify enumeration filter
            GameObject subTesterGO = Track(new GameObject("ZenjectSubTester"));
            subTesterGO.transform.SetParent(rootTester.transform);
            ZenjectRelationalTester subTester = subTesterGO.AddComponent<ZenjectRelationalTester>();
            // Provide a descendant collider for the sub tester
            GameObject subChild = Track(new GameObject("ZenjectSubChild"));
            subChild.AddComponent<CapsuleCollider>();
            subChild.transform.SetParent(subTesterGO.transform);
            // Make the tester itself inactive
            subTesterGO.SetActive(false);

            // includeInactiveChildren = false -> root tester is assigned (root is always included), inactive sub-tester skipped
            Container.AssignRelationalHierarchy(
                rootTester.gameObject,
                includeInactiveChildren: false
            );
            Assert.That(
                rootTester.parentBody,
                Is.Not.Null,
                "Root tester should be assigned even when includeInactiveChildren is false"
            );
            Assert.That(
                rootTester.childCollider,
                Is.Not.Null,
                "Root tester should be assigned even when includeInactiveChildren is false"
            );
            Assert.That(
                subTester.parentBody,
                Is.Null,
                "Inactive sub tester should be skipped when includeInactiveChildren is false"
            );
            Assert.That(
                subTester.childCollider,
                Is.Null,
                "Inactive sub tester should be skipped when includeInactiveChildren is false"
            );

            // includeInactiveChildren = true -> sub tester now gets assigned
            Container.AssignRelationalHierarchy(
                rootTester.gameObject,
                includeInactiveChildren: true
            );
            Assert.That(
                subTester.parentBody,
                Is.Not.Null,
                "Inactive sub tester should be assigned when includeInactiveChildren is true"
            );
            Assert.That(
                subTester.childCollider,
                Is.Not.Null,
                "Inactive sub tester should be assigned when includeInactiveChildren is true"
            );
        }

        [Test]
        public void ContainerAssignRelationalHierarchyUsesBoundAssigner()
        {
            RecordingAssigner assigner = new();
            Container.Bind<IRelationalComponentAssigner>().FromInstance(assigner);
            ZenjectRelationalTester tester = CreateHierarchy();
            Container.AssignRelationalHierarchy(tester.gameObject, includeInactiveChildren: true);
            Assert.That(
                assigner.HierarchyCallCount,
                Is.EqualTo(1),
                "AssignRelationalHierarchy should use bound assigner"
            );
        }

        private ZenjectRelationalTester CreateHierarchy()
        {
            GameObject parent = Track(new GameObject("ZenjectParent"));
            parent.AddComponent<Rigidbody>();

            GameObject middle = Track(new GameObject("ZenjectMiddle"));
            ZenjectRelationalTester tester = middle.AddComponent<ZenjectRelationalTester>();
            middle.transform.SetParent(parent.transform);

            GameObject child = Track(new GameObject("ZenjectChild"));
            child.AddComponent<CapsuleCollider>();
            child.transform.SetParent(middle.transform);

            return tester;
        }

        private GameObject Track(GameObject gameObject)
        {
            _spawned.Add(gameObject);
            return gameObject;
        }

        private static AttributeMetadataCache CreateCacheFor(Type componentType)
        {
            AttributeMetadataCache cache =
                ScriptableObject.CreateInstance<AttributeMetadataCache>();

            AttributeMetadataCache.RelationalFieldMetadata[] fields =
            {
                new AttributeMetadataCache.RelationalFieldMetadata(
                    nameof(ZenjectRelationalTester.parentBody),
                    AttributeMetadataCache.RelationalAttributeKind.Parent,
                    AttributeMetadataCache.FieldKind.Single,
                    typeof(Rigidbody).AssemblyQualifiedName,
                    isInterface: false
                ),
                new AttributeMetadataCache.RelationalFieldMetadata(
                    nameof(ZenjectRelationalTester.childCollider),
                    AttributeMetadataCache.RelationalAttributeKind.Child,
                    AttributeMetadataCache.FieldKind.Single,
                    typeof(CapsuleCollider).AssemblyQualifiedName,
                    isInterface: false
                ),
            };

            AttributeMetadataCache.RelationalTypeMetadata[] relationalTypes =
            {
                new AttributeMetadataCache.RelationalTypeMetadata(
                    componentType.AssemblyQualifiedName,
                    fields
                ),
            };

            cache._relationalTypeMetadata = relationalTypes;
            cache.ForceRebuildForTests();
            return cache;
        }

        private sealed class RecordingAssigner : IRelationalComponentAssigner
        {
            public int CallCount { get; private set; }

            public Component LastComponent { get; private set; }

            public int HierarchyCallCount { get; private set; }

            public bool HasRelationalAssignments(Type componentType)
            {
                return true;
            }

            public void Assign(Component component)
            {
                LastComponent = component;
                CallCount++;
                component?.AssignRelationalComponents();
            }

            public void Assign(IEnumerable<Component> components)
            {
                if (components == null)
                {
                    return;
                }

                foreach (Component component in components)
                {
                    Assign(component);
                }
            }

            public void AssignHierarchy(GameObject root, bool includeInactiveChildren = true)
            {
                if (root == null)
                {
                    return;
                }

                HierarchyCallCount++;
                Component[] components = root.GetComponentsInChildren<Component>(
                    includeInactiveChildren
                );
                foreach (Component component in components)
                {
                    Assign(component);
                }
            }
        }

        private sealed class ZenjectRelationalTester : MonoBehaviour
        {
            [ParentComponent(OnlyAncestors = true)]
            public Rigidbody parentBody;

            [ChildComponent(OnlyDescendants = true)]
            public CapsuleCollider childCollider;
        }
    }
}
#endif
