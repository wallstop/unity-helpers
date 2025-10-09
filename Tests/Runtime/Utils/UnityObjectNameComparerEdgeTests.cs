namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class UnityObjectNameComparerEdgeTests
    {
        private readonly System.Collections.Generic.List<UnityEngine.Object> _spawned = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (UnityEngine.Object obj in _spawned)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                    yield return null;
                }
            }

            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator CompareTreatsOnlyTrailingNumbersAsNumeric()
        {
            GameObject a = Create("Item001");
            GameObject b = Create("Item10");

            int comparison = UnityObjectNameComparer<GameObject>.Instance.Compare(a, b);
            Assert.Less(comparison, 0);
            yield break;
        }

        [UnityTest]
        public IEnumerator CompareFallsBackToCaseInsensitiveWhenNoNumbers()
        {
            GameObject a = Create("alpha");
            GameObject b = Create("Beta");

            int comparison = UnityObjectNameComparer<GameObject>.Instance.Compare(a, b);
            Assert.Less(comparison, 0);
            yield break;
        }

        [UnityTest]
        public IEnumerator CompareOrdersWhenOnlyOneHasTrailingNumber()
        {
            GameObject a = Create("Item");
            GameObject b = Create("Item2");

            int comparison = UnityObjectNameComparer<GameObject>.Instance.Compare(a, b);
            Assert.Less(comparison, 0);
            yield break;
        }

        [UnityTest]
        public IEnumerator CompareOrdersByPrefixBeforeNumeric()
        {
            GameObject a = Create("Item2");
            GameObject b = Create("Another10");

            int comparison = UnityObjectNameComparer<GameObject>.Instance.Compare(a, b);
            Assert.Greater(comparison, 0);
            yield break;
        }

        private GameObject Create(string name)
        {
            GameObject go = new(name);
            _spawned.Add(go);
            return go;
        }
    }
}
