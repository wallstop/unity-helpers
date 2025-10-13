#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.VContainer
{
    using System;
    using global::VContainer;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.Pool;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;

    public sealed class RelationalObjectPoolsVContainerTests
    {
        [Test]
        public void ComponentPoolGetWithRelationsInjectsAndAssigns()
        {
            ContainerBuilder builder = new ContainerBuilder();
            IObjectResolver resolver = builder.Build();

            ObjectPool<TestComponent> pool = RelationalObjectPools.CreatePoolWithRelations(
                createFunc: () =>
                {
                    GameObject go = new GameObject("PooledRoot");
                    go.AddComponent<Rigidbody>();
                    GameObject middle = new GameObject("PooledMiddle");
                    middle.transform.SetParent(go.transform);
                    TestComponent tester = middle.AddComponent<TestComponent>();
                    GameObject child = new GameObject("PooledChild");
                    child.transform.SetParent(middle.transform);
                    child.AddComponent<CapsuleCollider>();
                    return tester;
                }
            );

            TestComponent comp = pool.GetWithRelations(resolver);

            Assert.IsTrue(comp != null);
            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);

            GameObject root = comp.transform.root.gameObject;
            pool.Release(comp);
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void GameObjectPoolGetWithRelationsInjectsAndAssigns()
        {
            ContainerBuilder builder = new ContainerBuilder();
            IObjectResolver resolver = builder.Build();

            GameObject prefab = new GameObject("PrefabRoot");
            prefab.AddComponent<Rigidbody>();
            GameObject mid = new GameObject("PrefabMiddle");
            mid.transform.SetParent(prefab.transform);
            TestComponent prefabComp = mid.AddComponent<TestComponent>();
            GameObject child = new GameObject("PrefabChild");
            child.transform.SetParent(mid.transform);
            child.AddComponent<CapsuleCollider>();

            ObjectPool<GameObject> pool = RelationalObjectPools.CreateGameObjectPoolWithRelations(
                prefab
            );

            GameObject instance = pool.GetWithRelations(resolver);
            TestComponent comp = instance.GetComponentInChildren<TestComponent>(true);

            Assert.IsTrue(comp != null);
            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);

            pool.Release(instance);
            UnityEngine.Object.DestroyImmediate(prefab);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private sealed class TestComponent : MonoBehaviour
        {
            [ParentComponent(OnlyAncestors = true)]
            public Rigidbody parentBody;

            [ChildComponent(OnlyDescendants = true)]
            public CapsuleCollider childCollider;
        }
    }
}
#endif
