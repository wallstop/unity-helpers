#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.VContainer
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using VContainer;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;
    using WallstopStudios.UnityHelpers.Tags;

    public sealed class RelationalComponentsVContainerTests
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
        public void ResolverExtensions_UseBoundAssigner()
        {
            RecordingAssigner assigner = new();
            ContainerBuilder builder = new();
            builder.RegisterInstance(assigner).As<IRelationalComponentAssigner>();
            IObjectResolver resolver = builder.Build();

            VContainerRelationalTester tester = CreateHierarchy();

            resolver.AssignRelationalComponents(tester);

            Assert.That(assigner.CallCount, Is.EqualTo(1));
            Assert.That(assigner.LastComponent, Is.SameAs(tester));
            Assert.That(tester.parentBody, Is.Not.Null);
            Assert.That(tester.childCollider, Is.Not.Null);
        }

        [Test]
        public void ResolverExtensions_FallBackWhenAssignerMissing()
        {
            ContainerBuilder builder = new();
            IObjectResolver resolver = builder.Build();

            VContainerRelationalTester tester = CreateHierarchy();

            resolver.AssignRelationalComponents(tester);

            Assert.That(tester.parentBody, Is.Not.Null);
            Assert.That(tester.childCollider, Is.Not.Null);
        }

        [Test]
        public void EntryPoint_AssignsActiveSceneComponents()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(VContainerRelationalTester));
            Lazy<AttributeMetadataCache> previousLazy = OverrideCacheLazy(cache);

            try
            {
                ContainerBuilder builder = new();
                builder.RegisterRelationalComponents();
                IObjectResolver resolver = builder.Build();

                VContainerRelationalTester tester = CreateHierarchy();

                RelationalComponentEntryPoint entryPoint =
                    resolver.Resolve<RelationalComponentEntryPoint>();
                entryPoint.Initialize();

                Assert.That(tester.parentBody, Is.Not.Null);
                Assert.That(tester.childCollider, Is.Not.Null);
            }
            finally
            {
                RestoreCacheLazy(previousLazy);
                if (cache != null)
                {
                    UnityEngine.Object.DestroyImmediate(cache);
                }
            }
        }

        private VContainerRelationalTester CreateHierarchy()
        {
            GameObject parent = Track(new GameObject("VContainerParent"));
            parent.AddComponent<Rigidbody>();

            GameObject middle = Track(new GameObject("VContainerMiddle"));
            VContainerRelationalTester tester = middle.AddComponent<VContainerRelationalTester>();
            middle.transform.SetParent(parent.transform);

            GameObject child = Track(new GameObject("VContainerChild"));
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
                    nameof(VContainerRelationalTester.parentBody),
                    AttributeMetadataCache.RelationalAttributeKind.Parent,
                    AttributeMetadataCache.FieldKind.Single,
                    typeof(Rigidbody).AssemblyQualifiedName,
                    isInterface: false
                ),
                new AttributeMetadataCache.RelationalFieldMetadata(
                    nameof(VContainerRelationalTester.childCollider),
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

        private static Lazy<AttributeMetadataCache> OverrideCacheLazy(AttributeMetadataCache cache)
        {
            System.Reflection.FieldInfo field = typeof(AttributeMetadataCache).BaseType.GetField(
                "LazyInstance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );
            Lazy<AttributeMetadataCache> previous =
                (Lazy<AttributeMetadataCache>)field.GetValue(null);
            field.SetValue(null, new Lazy<AttributeMetadataCache>(() => cache));
            return previous;
        }

        private static void RestoreCacheLazy(Lazy<AttributeMetadataCache> previous)
        {
            System.Reflection.FieldInfo field = typeof(AttributeMetadataCache).BaseType.GetField(
                "LazyInstance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );
            field.SetValue(null, previous);
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

        private sealed class VContainerRelationalTester : MonoBehaviour
        {
            [ParentComponent(OnlyAncestors = true)]
            public Rigidbody parentBody;

            [ChildComponent(OnlyDescendants = true)]
            public CapsuleCollider childCollider;
        }
    }
}
#endif
