namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    [TestFixture]
    public sealed class ObjectsTests : CommonTestBase
    {
        private sealed class CustomClass
        {
            public int Value { get; set; }

            public override int GetHashCode()
            {
                return Value;
            }

            public override bool Equals(object obj)
            {
                return obj is CustomClass other && Value == other.Value;
            }
        }

        private sealed class CustomEnumerable : IEnumerable<int>
        {
            private readonly List<int> _items;

            public CustomEnumerable(params int[] items)
            {
                _items = new List<int>(items);
            }

            public IEnumerator<int> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class DisposableEnumerator : IEnumerator<int>
        {
            private readonly List<int> _items;
            private int _position = -1;

            public DisposableEnumerator(List<int> items)
            {
                _items = items;
            }

            public int Current => _items[_position];

            object IEnumerator.Current => Current;

            public bool WasDisposed { get; private set; }

            public bool MoveNext()
            {
                _position++;
                return _position < _items.Count;
            }

            public void Reset()
            {
                _position = -1;
            }

            public void Dispose()
            {
                WasDisposed = true;
            }
        }

        private sealed class DisposableEnumerable : IEnumerable<int>
        {
            private readonly List<int> _items;

            public DisposableEnumerable(params int[] items)
            {
                _items = new List<int>(items);
            }

            public DisposableEnumerator LastEnumerator { get; private set; }

            public IEnumerator<int> GetEnumerator()
            {
                LastEnumerator = new DisposableEnumerator(_items);
                return LastEnumerator;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [UnityTest]
        public IEnumerator NullReturnsTrueForNullUnityObject()
        {
            GameObject nullObject = null;

            bool result = Objects.Null(nullObject);

            Assert.IsTrue(result);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NullReturnsFalseForValidUnityObject()
        {
            GameObject gameObject = Track(new GameObject("Test"));

            bool result = Objects.Null(gameObject);

            Assert.IsFalse(result);
            UnityEngine.Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NullReturnsTrueForDestroyedUnityObject()
        {
            GameObject gameObject = Track(new GameObject("Test"));
            UnityEngine.Object.Destroy(gameObject);
            yield return null;

            bool result = Objects.Null(gameObject);

            Assert.IsTrue(result);
        }

        [UnityTest]
        public IEnumerator NullDetectsDestroyedUnityObjectWhenBoxed()
        {
            GameObject gameObject = Track(new GameObject("Test"));
            object boxed = gameObject;
            UnityEngine.Object.Destroy(gameObject);
            yield return null;

            bool result = Objects.Null(boxed);

            Assert.IsTrue(result);
        }

        [Test]
        public void NullReturnsTrueForNullSystemObject()
        {
            object nullObject = null;

            bool result = Objects.Null(nullObject);

            Assert.IsTrue(result);
        }

        [Test]
        public void NullReturnsFalseForValidSystemObject()
        {
            object obj = new();

            bool result = Objects.Null(obj);

            Assert.IsFalse(result);
        }

        [Test]
        public void NullReturnsFalseForBoxedValueType()
        {
            object boxedInt = 42;

            bool result = Objects.Null(boxedInt);

            Assert.IsFalse(result);
        }

        [UnityTest]
        public IEnumerator NotNullReturnsFalseForNullUnityObject()
        {
            GameObject nullObject = null;

            bool result = Objects.NotNull(nullObject);

            Assert.IsFalse(result);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NotNullReturnsTrueForValidUnityObject()
        {
            GameObject gameObject = Track(new GameObject("Test"));

            bool result = Objects.NotNull(gameObject);

            Assert.IsTrue(result);
            UnityEngine.Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NotNullReturnsFalseForDestroyedUnityObject()
        {
            GameObject gameObject = Track(new GameObject("Test"));
            UnityEngine.Object.Destroy(gameObject);
            yield return null;

            bool result = Objects.NotNull(gameObject);

            Assert.IsFalse(result);
        }

        [UnityTest]
        public IEnumerator NotNullDetectsDestroyedUnityObjectWhenBoxed()
        {
            GameObject gameObject = Track(new GameObject("Test"));
            object boxed = gameObject;
            UnityEngine.Object.Destroy(gameObject);
            yield return null;

            bool result = Objects.NotNull(boxed);

            Assert.IsFalse(result);
        }

        [Test]
        public void NotNullReturnsFalseForNullSystemObject()
        {
            object nullObject = null;

            bool result = Objects.NotNull(nullObject);

            Assert.IsFalse(result);
        }

        [Test]
        public void NotNullReturnsTrueForValidSystemObject()
        {
            object obj = new();

            bool result = Objects.NotNull(obj);

            Assert.IsTrue(result);
        }

        [Test]
        public void HashCodeReturnsZeroForNullReference()
        {
            int result = Objects.HashCode((object)null);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void HashCodeReturnsZeroForNullReferenceUnityObject()
        {
            int result = Objects.HashCode<object>((UnityEngine.Object)null);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void HashCodeReturnsZeroForNullReferenceUnityObjec2t()
        {
            int result = Objects.HashCode((UnityEngine.Object)null);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void HashCodeDistinguishesDifferentObjects()
        {
            CustomClass a = new() { Value = 1 };
            CustomClass b = new() { Value = 2 };

            int first = Objects.HashCode(a, b);
            int second = Objects.HashCode(b, a);

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void HashCodeSpanMatchesVariadic()
        {
            CustomClass a = new() { Value = 1 };
            CustomClass b = new() { Value = 2 };
            CustomClass c = new() { Value = 3 };
            CustomClass[] array = { a, b, c };
            ReadOnlySpan<CustomClass> span = array;

            int spanHash = Objects.HashCode(span);
            int variadicHash = Objects.HashCode(a, b, c);

            Assert.AreEqual(variadicHash, spanHash);
        }

        [Test]
        public void HashCodeReturnsDeterministicValueForIntegers()
        {
            const int expected = 1456420779;

            int first = Objects.HashCode(1, 2, 3);
            int second = Objects.HashCode(1, 2, 3);

            Assert.AreEqual(expected, first);
            Assert.AreEqual(expected, second);
        }

        [Test]
        public void HashCodeIncludesNullOrderingDeterministically()
        {
            const int expected = 1175002007;

            int hash = Objects.HashCode<object, int, int>(null, 1, 2);

            Assert.AreEqual(expected, hash);
        }

        [Test]
        public void EnumerableHashCodeMatchesDeterministicHash()
        {
            const int expected = 1456420779;

            int enumerableHash = Objects.EnumerableHashCode(new[] { 1, 2, 3 });

            Assert.AreEqual(expected, enumerableHash);
        }

        [Test]
        public void EnumerableHashCodeReturnsZeroForNull()
        {
            Assert.AreEqual(0, Objects.EnumerableHashCode<int>(null));
        }

        [Test]
        public void EnumerableHashCodeRespectsOrder()
        {
            CustomEnumerable ascending = new(1, 2, 3);
            CustomEnumerable descending = new(3, 2, 1);

            int ascHash = Objects.EnumerableHashCode<int>(ascending);
            int descHash = Objects.EnumerableHashCode<int>(descending);

            Assert.AreNotEqual(ascHash, descHash);
        }

        [Test]
        public void EnumerableHashCodeDisposesEnumerator()
        {
            DisposableEnumerable enumerable = new(1, 2, 3);

            int result = Objects.EnumerableHashCode<int>(enumerable);

            Assert.AreNotEqual(0, result);
            Assert.IsTrue(enumerable.LastEnumerator.WasDisposed);
        }
    }
}
