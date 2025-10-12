#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.Zenject
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;
    using WallstopStudios.UnityHelpers.Tags;
    using Zenject;

    public sealed class RelationalComponentsZenjectTests : ZenjectUnitTestFixture
    {
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
        public void ContainerExtensions_UseBoundAssigner()
        {
            RecordingAssigner assigner = new();
            Container.Bind<IRelationalComponentAssigner>().FromInstance(assigner);

            ZenjectRelationalTester tester = CreateHierarchy();

            Container.AssignRelationalComponents(tester);

            Assert.That(assigner.CallCount, Is.EqualTo(1));
            Assert.That(assigner.LastComponent, Is.SameAs(tester));
            Assert.That(tester.parentBody, Is.Not.Null);
            Assert.That(tester.childCollider, Is.Not.Null);
        }

        [Test]
        public void ContainerExtensions_FallBackWhenAssignerMissing()
        {
            ZenjectRelationalTester tester = CreateHierarchy();

            Container.AssignRelationalComponents(tester);

            Assert.That(tester.parentBody, Is.Not.Null);
            Assert.That(tester.childCollider, Is.Not.Null);
        }

        [Test]
        public void SceneInitializer_AssignsActiveSceneComponents()
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

                Assert.That(tester.parentBody, Is.Not.Null);
                Assert.That(tester.childCollider, Is.Not.Null);
            }
            finally
            {
                if (cache != null)
                {
                    UnityEngine.Object.DestroyImmediate(cache);
                }
            }
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

            typeof(AttributeMetadataCache)
                .GetField(
                    "_relationalTypeMetadata",
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                )
                .SetValue(cache, relationalTypes);

            return cache;
        }

        private sealed class RecordingAssigner : IRelationalComponentAssigner
        {
            public int CallCount { get; private set; }

            public Component LastComponent { get; private set; }

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
