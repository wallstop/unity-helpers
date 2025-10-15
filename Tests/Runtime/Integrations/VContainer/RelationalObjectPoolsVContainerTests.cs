#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.VContainer
{
    using global::VContainer;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.Pool;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class RelationalObjectPoolsVContainerTests : CommonTestBase
    {
        [SetUp]
        public void CommonSetup()
        {
            ReflexTestSupport.EnsureReflexSettings();
        }

        [Test]
        public void ComponentPoolGetWithRelationsInjectsAndAssigns()
        {
            ContainerBuilder builder = new();
            IObjectResolver resolver = builder.Build();

            ObjectPool<TestComponent> pool = RelationalObjectPools.CreatePoolWithRelations(
                createFunc: () =>
                {
                    GameObject go = Track(new GameObject("PooledRoot"));
                    go.AddComponent<Rigidbody>();
                    GameObject middle = Track(new GameObject("PooledMiddle"));
                    middle.transform.SetParent(go.transform);
                    TestComponent tester = middle.AddComponent<TestComponent>();
                    GameObject child = Track(new GameObject("PooledChild"));
                    child.transform.SetParent(middle.transform);
                    child.AddComponent<CapsuleCollider>();
                    return tester;
                }
            );

            TestComponent comp = pool.GetWithRelations(resolver);

            Assert.IsTrue(comp != null);
            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);

            pool.Release(comp);
            pool.Clear();
        }

        [Test]
        public void GameObjectPoolGetWithRelationsInjectsAndAssigns()
        {
            ContainerBuilder builder = new();
            IObjectResolver resolver = builder.Build();

            GameObject prefab = Track(new GameObject("PrefabRoot"));
            prefab.AddComponent<Rigidbody>();
            GameObject mid = Track(new GameObject("PrefabMiddle"));
            mid.transform.SetParent(prefab.transform);
            TestComponent prefabComp = mid.AddComponent<TestComponent>();
            GameObject child = Track(new GameObject("PrefabChild"));
            child.transform.SetParent(mid.transform);
            child.AddComponent<CapsuleCollider>();

            ObjectPool<GameObject> pool = RelationalObjectPools.CreateGameObjectPoolWithRelations(
                prefab
            );

            GameObject instance = Track(pool.GetWithRelations(resolver));
            TestComponent comp = instance.GetComponentInChildren<TestComponent>(true);

            Assert.IsTrue(comp != null);
            Assert.IsTrue(comp.parentBody != null);
            Assert.IsTrue(comp.childCollider != null);

            pool.Release(instance);
            pool.Clear();
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
