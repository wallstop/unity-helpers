#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.Zenject
{
    using System.Collections;
    using global::Zenject;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Editor.Utils;

    public sealed class ZenjectRelationalHelpersTests : CommonTestBase
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
            DiContainer container = new DiContainer();

            // Build hierarchy: Parent(Rigidbody) -> Middle(TestComponent) -> Child(CapsuleCollider)
            GameObject parent = NewGameObject("Root");
            parent.AddComponent<Rigidbody>();
            GameObject middle = NewGameObject("Middle");
            middle.transform.SetParent(parent.transform);
            TestComponent comp = middle.AddComponent<TestComponent>();
            GameObject child = NewGameObject("Child");
            child.transform.SetParent(middle.transform);
            child.AddComponent<CapsuleCollider>();

            TestComponent result = container.InjectWithRelations(comp);

            Assert.That(result, Is.SameAs(comp));
            Assert.IsTrue(comp.parentBody != null);
        }

        [Test]
        public void InjectGameObjectWithRelationsAssignsHierarchy()
        {
            DiContainer container = new DiContainer();

            GameObject root = NewGameObject("Root");
            root.AddComponent<Rigidbody>();
            GameObject middle = NewGameObject("Middle");
            middle.transform.SetParent(root.transform);
            TestComponent comp = middle.AddComponent<TestComponent>();
            GameObject child = NewGameObject("Child");
            child.transform.SetParent(middle.transform);
            child.AddComponent<CapsuleCollider>();

            container.InjectGameObjectWithRelations(root);

            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);
        }

        [Test]
        public void InstantiateGameObjectWithRelationsAssignsHierarchy()
        {
            DiContainer container = new DiContainer();

            GameObject prefab = NewGameObject("PrefabRoot");
            prefab.AddComponent<Rigidbody>();
            GameObject mid = NewGameObject("PrefabMiddle");
            mid.transform.SetParent(prefab.transform);
            TestComponent prefabComp = mid.AddComponent<TestComponent>();
            GameObject child = NewGameObject("PrefabChild");
            child.transform.SetParent(mid.transform);
            child.AddComponent<CapsuleCollider>();

            GameObject instance = container.InstantiateGameObjectWithRelations(prefab);
            TestComponent comp = instance.GetComponentInChildren<TestComponent>(true);

            Assert.IsTrue(comp != null);
            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);
        }

        [Test]
        public void RelationalMemoryPoolAssignsOnSpawn()
        {
            // Create a pool and inject a container into the private field using reflection
            RelationalMemoryPool<TestComponent> pool = new RelationalMemoryPool<TestComponent>();
            DiContainer container = new DiContainer();

            pool._container = container;

            GameObject go = NewGameObject("PooledRoot");
            go.AddComponent<Rigidbody>();
            GameObject middle = NewGameObject("PooledMiddle");
            middle.transform.SetParent(go.transform);
            TestComponent comp = middle.AddComponent<TestComponent>();
            GameObject child = NewGameObject("PooledChild");
            child.transform.SetParent(middle.transform);
            child.AddComponent<CapsuleCollider>();

            // Call OnSpawned indirectly via protected method using a small helper
            PoolHarness harness = new PoolHarness(pool);
            harness.InvokeOnSpawned(comp);

            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);
        }

        private sealed class PoolHarness
        {
            private readonly RelationalMemoryPool<TestComponent> _pool;

            public PoolHarness(RelationalMemoryPool<TestComponent> pool)
            {
                _pool = pool;
            }

            public void InvokeOnSpawned(TestComponent item)
            {
                _pool.InternalOnSpawned(item);
            }
        }
    }
}
#endif
