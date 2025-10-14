#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.VContainer
{
    using System.Collections;
    using global::VContainer;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Editor.Utils;

    public sealed class VContainerRelationalHelpersTests : CommonTestBase
    {
        private sealed class TestComponent : MonoBehaviour
        {
            [ParentComponent(OnlyAncestors = true)]
            public Rigidbody parentBody;

            [ChildComponent(OnlyDescendants = true)]
            public CapsuleCollider childCollider;
        }

        [Test]
        public void InjectWithRelationsAssignsFields()
        {
            ContainerBuilder builder = new ContainerBuilder();
            IObjectResolver resolver = builder.Build();

            // Build hierarchy: Parent(Rigidbody) -> Middle(TestComponent) -> Child(CapsuleCollider)
            GameObject parent = NewGameObject("Parent");
            parent.AddComponent<Rigidbody>();
            GameObject middle = NewGameObject("Middle");
            middle.transform.SetParent(parent.transform);
            TestComponent comp = middle.AddComponent<TestComponent>();
            GameObject child = NewGameObject("Child");
            child.transform.SetParent(middle.transform);
            child.AddComponent<CapsuleCollider>();

            TestComponent result = resolver.InjectWithRelations(comp);

            Assert.That(result, Is.SameAs(comp));
            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);
        }

        [Test]
        public void InstantiateComponentWithRelationsAssignsFields()
        {
            ContainerBuilder builder = new ContainerBuilder();
            IObjectResolver resolver = builder.Build();

            // Build prefab hierarchy: Root(Rigidbody) -> Middle(TestComponent) -> Child(CapsuleCollider)
            GameObject prefabRoot = NewGameObject("PrefabRoot");
            prefabRoot.AddComponent<Rigidbody>();
            GameObject prefabMiddle = NewGameObject("PrefabMiddle");
            prefabMiddle.transform.SetParent(prefabRoot.transform);
            TestComponent prefabComp = prefabMiddle.AddComponent<TestComponent>();
            GameObject prefabChild = NewGameObject("PrefabChild");
            prefabChild.transform.SetParent(prefabMiddle.transform);
            prefabChild.AddComponent<CapsuleCollider>();

            // Instantiate the full hierarchy and hydrate via GO helper
            GameObject instanceRoot = resolver.InstantiateGameObjectWithRelations(prefabRoot);
            TestComponent instance = instanceRoot.GetComponentInChildren<TestComponent>(true);

            Assert.IsTrue(instance != null);
            Assert.IsTrue(instance.parentBody != null);
            Assert.IsTrue(instance.childCollider != null);
        }

        [Test]
        public void InjectGameObjectWithRelationsAssignsHierarchy()
        {
            ContainerBuilder builder = new ContainerBuilder();
            IObjectResolver resolver = builder.Build();

            GameObject root = NewGameObject("Root");
            root.AddComponent<Rigidbody>();
            GameObject middle = NewGameObject("Middle");
            middle.transform.SetParent(root.transform);
            TestComponent comp = middle.AddComponent<TestComponent>();
            GameObject child = NewGameObject("Child");
            child.transform.SetParent(middle.transform);
            child.AddComponent<CapsuleCollider>();

            resolver.InjectGameObjectWithRelations(root);

            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);
        }

        [Test]
        public void InstantiateGameObjectWithRelationsAssignsHierarchy()
        {
            ContainerBuilder builder = new ContainerBuilder();
            IObjectResolver resolver = builder.Build();

            GameObject prefab = NewGameObject("PrefabRoot");
            prefab.AddComponent<Rigidbody>();
            GameObject mid = NewGameObject("PrefabMiddle");
            mid.transform.SetParent(prefab.transform);
            TestComponent prefabComp = mid.AddComponent<TestComponent>();
            GameObject child = NewGameObject("PrefabChild");
            child.transform.SetParent(mid.transform);
            child.AddComponent<CapsuleCollider>();

            GameObject instance = resolver.InstantiateGameObjectWithRelations(prefab);
            TestComponent comp = instance.GetComponentInChildren<TestComponent>(true);

            Assert.IsTrue(comp != null);
            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);
        }

        [Test]
        public void AssignerRecognizesBaseTypeMetadataForDerivedComponents()
        {
            AttributeMetadataCache cache = CreateScriptableObject<AttributeMetadataCache>();

            AttributeMetadataCache.RelationalFieldMetadata[] fields =
            {
                new AttributeMetadataCache.RelationalFieldMetadata(
                    nameof(BaseWithSibling._spriteRenderer),
                    AttributeMetadataCache.RelationalAttributeKind.Sibling,
                    AttributeMetadataCache.FieldKind.Single,
                    typeof(SpriteRenderer).AssemblyQualifiedName,
                    false
                ),
            };
            AttributeMetadataCache.RelationalTypeMetadata[] relational =
            {
                new AttributeMetadataCache.RelationalTypeMetadata(
                    typeof(BaseWithSibling).AssemblyQualifiedName,
                    fields
                ),
            };
            cache._relationalTypeMetadata = relational;
            cache.ForceRebuildForTests();

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(cache).AsSelf();
            builder
                .RegisterInstance(new RelationalComponentAssigner(cache))
                .As<IRelationalComponentAssigner>();
            IObjectResolver resolver = builder.Build();

            GameObject go = NewGameObject("Root");
            go.AddComponent<SpriteRenderer>();
            DerivedWithSibling comp = go.AddComponent<DerivedWithSibling>();

            resolver.AssignRelationalComponents(comp);

            Assert.IsTrue(comp.SR != null);
        }

        private class BaseWithSibling : MonoBehaviour
        {
            [SiblingComponent]
            protected internal SpriteRenderer _spriteRenderer;

            public SpriteRenderer SR => _spriteRenderer;
        }

        private sealed class DerivedWithSibling : BaseWithSibling { }
    }
}
#endif
