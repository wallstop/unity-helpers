#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.VContainer
{
    using System;
    using System.Collections.Generic;
    using global::VContainer;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
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
        public void ResolverExtensionsUseBoundAssigner()
        {
            RecordingAssigner assigner = new();
            ContainerBuilder builder = new();
            builder.RegisterInstance(assigner).As<IRelationalComponentAssigner>();
            IObjectResolver resolver = builder.Build();

            VContainerRelationalTester tester = CreateHierarchy();

            resolver.AssignRelationalComponents(tester);

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
        public void ResolverExtensionsFallBackWhenAssignerMissing()
        {
            ContainerBuilder builder = new();
            IObjectResolver resolver = builder.Build();

            VContainerRelationalTester tester = CreateHierarchy();

            resolver.AssignRelationalComponents(tester);

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
        public System.Collections.IEnumerator EntryPointAssignsActiveSceneComponents()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(VContainerRelationalTester));
            try
            {
                ContainerBuilder builder = new();
                builder.RegisterInstance(cache).AsSelf();
                var assigner = new RecordingAssigner();
                builder.RegisterInstance(assigner).As<IRelationalComponentAssigner>();
                IObjectResolver resolver = builder.Build();

                VContainerRelationalTester tester = CreateHierarchy();
                yield return null;

                var entryPoint = new RelationalComponentEntryPoint(
                    resolver.Resolve<IRelationalComponentAssigner>(),
                    cache,
                    RelationalSceneAssignmentOptions.Default
                );
                entryPoint.Initialize();
                yield return null;

                Assert.That(
                    tester.parentBody,
                    Is.Not.Null,
                    "Entry point should assign parentBody in the active scene"
                );
                Assert.That(
                    tester.childCollider,
                    Is.Not.Null,
                    "Entry point should assign childCollider in the active scene"
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
        public void BuildUpWithRelationsAssignsFields()
        {
            ContainerBuilder builder = new();
            IObjectResolver resolver = builder.Build();

            VContainerRelationalTester tester = CreateHierarchy();

            var result = resolver.BuildUpWithRelations(tester);

            Assert.That(
                result,
                Is.SameAs(tester),
                "BuildUpWithRelations should return the same component instance"
            );
            Assert.That(
                tester.parentBody,
                Is.Not.Null,
                "BuildUpWithRelations should assign parentBody"
            );
            Assert.That(
                tester.childCollider,
                Is.Not.Null,
                "BuildUpWithRelations should assign childCollider"
            );
        }

        [Test]
        public void RegisterRelationalComponentsRegistersAssignerSingleton()
        {
            ContainerBuilder builder = new();
            builder.RegisterRelationalComponents();
            IObjectResolver resolver = builder.Build();

            var a = resolver.Resolve<IRelationalComponentAssigner>();
            var b = resolver.Resolve<IRelationalComponentAssigner>();
            Assert.That(
                a,
                Is.SameAs(b),
                "RegisterRelationalComponents should register assigner as singleton"
            );
        }

        [Test]
        public void ResolverExtensionsAssignRelationalHierarchyAssignsFields()
        {
            ContainerBuilder builder = new();
            IObjectResolver resolver = builder.Build();
            VContainerRelationalTester tester = CreateHierarchy();
            resolver.AssignRelationalHierarchy(tester.gameObject, includeInactiveChildren: false);
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
        public void ResolverExtensionsAssignRelationalHierarchyUsesBoundAssigner()
        {
            RecordingAssigner assigner = new();
            ContainerBuilder builder = new();
            builder.RegisterInstance(assigner).As<IRelationalComponentAssigner>();
            IObjectResolver resolver = builder.Build();
            VContainerRelationalTester tester = CreateHierarchy();
            resolver.AssignRelationalHierarchy(tester.gameObject, includeInactiveChildren: true);
            Assert.That(
                assigner.HierarchyCallCount,
                Is.EqualTo(1),
                "AssignRelationalHierarchy should use bound assigner"
            );
        }

        [UnityTest]
        public System.Collections.IEnumerator EntryPointRespectsIncludeInactiveOption()
        {
            AttributeMetadataCache cache = CreateCacheFor(typeof(VContainerRelationalTester));
            try
            {
                ContainerBuilder builder = new();
                builder.RegisterInstance(cache).AsSelf();
                var assigner = new RecordingAssigner();
                builder.RegisterInstance(assigner).As<IRelationalComponentAssigner>();
                IObjectResolver resolver = builder.Build();

                VContainerRelationalTester tester = CreateHierarchy();
                tester.gameObject.SetActive(false);
                yield return null;

                var disabledEntryPoint = new RelationalComponentEntryPoint(
                    resolver.Resolve<IRelationalComponentAssigner>(),
                    cache,
                    new RelationalSceneAssignmentOptions(includeInactive: false)
                );
                disabledEntryPoint.Initialize();
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

                var enabledEntryPoint = new RelationalComponentEntryPoint(
                    resolver.Resolve<IRelationalComponentAssigner>(),
                    cache,
                    new RelationalSceneAssignmentOptions(includeInactive: true)
                );
                enabledEntryPoint.Initialize();
                yield return null;
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

            cache._relationalTypeMetadata = relationalTypes;
            return cache;
        }

        // No reflection needed: tests provide a cache instance to the assigner via DI

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
