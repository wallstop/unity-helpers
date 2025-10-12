#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.Zenject
{
    using System;
    using System.Collections.Generic;
    using global::Zenject;
    using NUnit.Framework;
    using UnityEngine;
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

        [Test]
        public void SceneInitializerAssignsActiveSceneComponents()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(ZenjectRelationalTester));
            try
            {
                Container.BindInstance(cache);
                Container
                    .Bind<IRelationalComponentAssigner>()
                    .FromMethod(_ => new RelationalComponentAssigner(cache))
                    .AsSingle();
                Container.BindInstance(RelationalSceneAssignmentOptions.Default);
                Container.BindInterfacesTo<RelationalComponentSceneInitializer>().AsSingle();

                ZenjectRelationalTester tester = CreateHierarchy();

                IInitializable initializer = Container.Resolve<IInitializable>();
                initializer.Initialize();

                Assert.That(
                    tester.parentBody,
                    Is.Not.Null,
                    "Scene initializer should assign parentBody in the active scene"
                );
                Assert.That(
                    tester.childCollider,
                    Is.Not.Null,
                    "Scene initializer should assign childCollider in the active scene"
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
        public void SceneInitializerSkipsInactiveWhenOptionDisabled()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(ZenjectRelationalTester));
            try
            {
                Container.BindInstance(cache);
                Container
                    .Bind<IRelationalComponentAssigner>()
                    .FromMethod(_ => new RelationalComponentAssigner(cache))
                    .AsSingle();
                Container.BindInstance(new RelationalSceneAssignmentOptions(false));
                Container.BindInterfacesTo<RelationalComponentSceneInitializer>().AsSingle();

                ZenjectRelationalTester tester = CreateHierarchy();
                tester.gameObject.SetActive(false);

                IInitializable initializer = Container.Resolve<IInitializable>();
                initializer.Initialize();

                Assert.That(
                    tester.parentBody,
                    Is.Null,
                    "Disabled option should skip inactive components"
                );
                Assert.That(
                    tester.childCollider,
                    Is.Null,
                    "Disabled option should skip inactive components"
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
        public void SceneInitializerIncludesInactiveWhenOptionEnabled()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(ZenjectRelationalTester));
            try
            {
                Container.BindInstance(cache);
                Container
                    .Bind<IRelationalComponentAssigner>()
                    .FromMethod(_ => new RelationalComponentAssigner(cache))
                    .AsSingle();
                Container.BindInstance(new RelationalSceneAssignmentOptions(true));
                Container.BindInterfacesTo<RelationalComponentSceneInitializer>().AsSingle();

                ZenjectRelationalTester tester = CreateHierarchy();
                tester.gameObject.SetActive(false);

                IInitializable initializer = Container.Resolve<IInitializable>();
                initializer.Initialize();

                Assert.That(
                    tester.parentBody,
                    Is.Not.Null,
                    "Enabled option should include inactive components"
                );
                Assert.That(
                    tester.childCollider,
                    Is.Not.Null,
                    "Enabled option should include inactive components"
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
