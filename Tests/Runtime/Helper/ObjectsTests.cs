namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;

    [TestFixture]
    public sealed class ObjectsTests
    {
        // Test class for WeakReference tests
        private sealed class TestClass
        {
            public int Value { get; set; }
        }

        // Custom class for hash code testing
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

        // Custom enumerable for testing
        private sealed class CustomEnumerable : IEnumerable
        {
            private readonly List<object> _items;

            public CustomEnumerable(params object[] items)
            {
                _items = new List<object>(items);
            }

            public IEnumerator GetEnumerator()
            {
                return _items.GetEnumerator();
            }
        }

        // Disposable enumerator to test disposal
        private sealed class DisposableEnumerator : IEnumerator, IDisposable
        {
            private readonly List<object> _items;
            private int _position = -1;
            public bool WasDisposed { get; private set; }

            public DisposableEnumerator(List<object> items)
            {
                _items = items;
            }

            public object Current => _items[_position];

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

        private sealed class DisposableEnumerable : IEnumerable
        {
            private readonly List<object> _items;
            public DisposableEnumerator LastEnumerator { get; private set; }

            public DisposableEnumerable(params object[] items)
            {
                _items = new List<object>(items);
            }

            public IEnumerator GetEnumerator()
            {
                LastEnumerator = new DisposableEnumerator(_items);
                return LastEnumerator;
            }
        }

        [Test]
        public void FromWeakReferenceReturnsTargetWhenAlive()
        {
            TestClass testObj = new() { Value = 42 };
            WeakReference weakRef = new(testObj);

            TestClass result = Objects.FromWeakReference<TestClass>(weakRef);

            Assert.IsNotNull(result);
            Assert.AreEqual(42, result.Value);
        }

        [Test]
        public void FromWeakReferenceReturnsNullWhenTargetIsNull()
        {
            WeakReference weakRef = new(null);

            TestClass result = Objects.FromWeakReference<TestClass>(weakRef);

            Assert.IsNull(result);
        }

        [Test]
        public void FromWeakReferenceThrowsWhenWeakReferenceIsNull()
        {
            Assert.Throws<NullReferenceException>(() =>
            {
                Objects.FromWeakReference<TestClass>(null);
            });
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
            GameObject gameObject = new("Test");

            bool result = Objects.Null(gameObject);

            Assert.IsFalse(result);
            UnityEngine.Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NullReturnsTrueForDestroyedUnityObject()
        {
            GameObject gameObject = new("Test");
            UnityEngine.Object.Destroy(gameObject);
            yield return null;

            bool result = Objects.Null(gameObject);

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
        public void NullReturnsFalseForValueTypeBoxed()
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
            GameObject gameObject = new("Test");

            bool result = Objects.NotNull(gameObject);

            Assert.IsTrue(result);
            UnityEngine.Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NotNullReturnsFalseForDestroyedUnityObject()
        {
            GameObject gameObject = new("Test");
            UnityEngine.Object.Destroy(gameObject);
            yield return null;

            bool result = Objects.NotNull(gameObject);

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
        public void NotNullReturnsTrueForValueTypeBoxed()
        {
            object boxedInt = 42;

            bool result = Objects.NotNull(boxedInt);

            Assert.IsTrue(result);
        }

        [Test]
        public void NullSafeHashCodeReturnsHashForValueType()
        {
            int value = 42;

            int result = Objects.NullSafeHashCode(value);

            Assert.AreEqual(value.GetHashCode(), result);
        }

        [Test]
        public void NullSafeHashCodeReturnsHashForReferenceType()
        {
            CustomClass obj = new() { Value = 123 };

            int result = Objects.NullSafeHashCode(obj);

            Assert.AreEqual(obj.GetHashCode(), result);
        }

        [Test]
        public void NullSafeHashCodeReturnsTypeHashForNullReferenceType()
        {
            CustomClass obj = null;

            int result = Objects.NullSafeHashCode(obj);

            Assert.AreEqual(typeof(CustomClass).GetHashCode(), result);
        }

        [Test]
        public void NullSafeHashCodeHandlesString()
        {
            string str = "test";

            int result = Objects.NullSafeHashCode(str);

            Assert.AreEqual(str.GetHashCode(), result);
        }

        [Test]
        public void NullSafeHashCodeHandlesNullString()
        {
            string str = null;

            int result = Objects.NullSafeHashCode(str);

            Assert.AreEqual(typeof(string).GetHashCode(), result);
        }

        [Test]
        public void NullSafeHashCodeConsistentForSameValue()
        {
            CustomClass obj = new() { Value = 456 };

            int hash1 = Objects.NullSafeHashCode(obj);
            int hash2 = Objects.NullSafeHashCode(obj);

            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void ValueTypeHashCodeSingleParameter()
        {
            int value = 42;

            int result = Objects.ValueTypeHashCode(value);

            // Should not be zero and should be consistent
            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(value));
        }

        [Test]
        public void ValueTypeHashCodeTwoParameters()
        {
            int a = 1;
            int b = 2;

            int result = Objects.ValueTypeHashCode(a, b);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(a, b));
        }

        [Test]
        public void ValueTypeHashCodeThreeParameters()
        {
            int a = 1;
            int b = 2;
            int c = 3;

            int result = Objects.ValueTypeHashCode(a, b, c);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(a, b, c));
        }

        [Test]
        public void ValueTypeHashCodeFourParameters()
        {
            int a = 1;
            int b = 2;
            int c = 3;
            int d = 4;

            int result = Objects.ValueTypeHashCode(a, b, c, d);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(a, b, c, d));
        }

        [Test]
        public void ValueTypeHashCodeDifferentForDifferentValues()
        {
            int hash1 = Objects.ValueTypeHashCode(1, 2);
            int hash2 = Objects.ValueTypeHashCode(2, 1);

            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void ValueTypeHashCodeSameForSameValues()
        {
            int hash1 = Objects.ValueTypeHashCode(5, 10, 15);
            int hash2 = Objects.ValueTypeHashCode(5, 10, 15);

            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void ValueTypeHashCodeHandlesMixedTypes()
        {
            int a = 1;
            float b = 2.5f;
            bool c = true;

            int result = Objects.ValueTypeHashCode(a, b, c);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void ValueTypeHashCodeHandlesZeroValues()
        {
            int result = Objects.ValueTypeHashCode(0, 0, 0);

            Assert.AreNotEqual(0, result); // Should still produce non-zero hash
        }

        [Test]
        public void ValueTypeHashCodeFiveParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(1, 2, 3, 4, 5));
        }

        [Test]
        public void ValueTypeHashCodeSixParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6));
        }

        [Test]
        public void ValueTypeHashCodeSevenParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7));
        }

        [Test]
        public void ValueTypeHashCodeEightParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8));
        }

        [Test]
        public void ValueTypeHashCodeNineParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9));
        }

        [Test]
        public void ValueTypeHashCodeTenParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10));
        }

        [Test]
        public void ValueTypeHashCodeElevenParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));
        }

        [Test]
        public void ValueTypeHashCodeTwelveParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(
                result,
                Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)
            );
        }

        [Test]
        public void ValueTypeHashCodeThirteenParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(
                result,
                Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13)
            );
        }

        [Test]
        public void ValueTypeHashCodeFourteenParameters()
        {
            int result = Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(
                result,
                Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)
            );
        }

        [Test]
        public void ValueTypeHashCodeFifteenParameters()
        {
            int result = Objects.ValueTypeHashCode(
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15
            );

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(
                result,
                Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)
            );
        }

        [Test]
        public void ValueTypeHashCodeSixteenParameters()
        {
            int result = Objects.ValueTypeHashCode(
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
                16
            );

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(
                result,
                Objects.ValueTypeHashCode(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)
            );
        }

        [Test]
        public void HashCodeSingleParameter()
        {
            CustomClass obj = new() { Value = 42 };

            int result = Objects.HashCode(obj);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.HashCode(obj));
        }

        [Test]
        public void HashCodeSingleParameterWithNull()
        {
            CustomClass obj = null;

            int result = Objects.HashCode(obj);

            // Should handle null gracefully
            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void HashCodeTwoParameters()
        {
            CustomClass obj1 = new() { Value = 1 };
            CustomClass obj2 = new() { Value = 2 };

            int result = Objects.HashCode(obj1, obj2);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.HashCode(obj1, obj2));
        }

        [Test]
        public void HashCodeTwoParametersWithNull()
        {
            CustomClass obj1 = new() { Value = 1 };
            CustomClass obj2 = null;

            int result = Objects.HashCode(obj1, obj2);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void HashCodeThreeParameters()
        {
            CustomClass obj1 = new() { Value = 1 };
            CustomClass obj2 = new() { Value = 2 };
            CustomClass obj3 = new() { Value = 3 };

            int result = Objects.HashCode(obj1, obj2, obj3);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.HashCode(obj1, obj2, obj3));
        }

        [Test]
        public void HashCodeFourParameters()
        {
            CustomClass obj1 = new() { Value = 1 };
            CustomClass obj2 = new() { Value = 2 };
            CustomClass obj3 = new() { Value = 3 };
            CustomClass obj4 = new() { Value = 4 };

            int result = Objects.HashCode(obj1, obj2, obj3, obj4);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.HashCode(obj1, obj2, obj3, obj4));
        }

        [Test]
        public void HashCodeDifferentForDifferentObjects()
        {
            CustomClass obj1 = new() { Value = 1 };
            CustomClass obj2 = new() { Value = 2 };

            int hash1 = Objects.HashCode(obj1, obj2);
            int hash2 = Objects.HashCode(obj2, obj1);

            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void HashCodeHandlesMixedNullAndNonNull()
        {
            CustomClass obj1 = new() { Value = 1 };
            CustomClass obj2 = null;
            CustomClass obj3 = new() { Value = 3 };

            int result = Objects.HashCode(obj1, obj2, obj3);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void HashCodeFiveParameters()
        {
            CustomClass[] objs = new CustomClass[5];
            for (int i = 0; i < 5; i++)
            {
                objs[i] = new CustomClass { Value = i + 1 };
            }

            int result = Objects.HashCode(objs[0], objs[1], objs[2], objs[3], objs[4]);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(result, Objects.HashCode(objs[0], objs[1], objs[2], objs[3], objs[4]));
        }

        [Test]
        public void HashCodeSixParameters()
        {
            CustomClass[] objs = new CustomClass[6];
            for (int i = 0; i < 6; i++)
            {
                objs[i] = new CustomClass { Value = i + 1 };
            }

            int result = Objects.HashCode(objs[0], objs[1], objs[2], objs[3], objs[4], objs[5]);

            Assert.AreNotEqual(0, result);
            Assert.AreEqual(
                result,
                Objects.HashCode(objs[0], objs[1], objs[2], objs[3], objs[4], objs[5])
            );
        }

        [Test]
        public void HashCodeSevenParameters()
        {
            CustomClass[] objs = new CustomClass[7];
            for (int i = 0; i < 7; i++)
            {
                objs[i] = new CustomClass { Value = i + 1 };
            }

            int result = Objects.HashCode(
                objs[0],
                objs[1],
                objs[2],
                objs[3],
                objs[4],
                objs[5],
                objs[6]
            );

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void HashCodeEightParameters()
        {
            CustomClass[] objs = new CustomClass[8];
            for (int i = 0; i < 8; i++)
            {
                objs[i] = new CustomClass { Value = i + 1 };
            }

            int result = Objects.HashCode(
                objs[0],
                objs[1],
                objs[2],
                objs[3],
                objs[4],
                objs[5],
                objs[6],
                objs[7]
            );

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void HashCodeHandlesAllNullParameters()
        {
            CustomClass obj1 = null,
                obj2 = null,
                obj3 = null;

            int result = Objects.HashCode(obj1, obj2, obj3);

            // Should produce consistent hash even with all nulls
            Assert.AreEqual(result, Objects.HashCode(obj1, obj2, obj3));
        }

        [Test]
        public void EnumerableHashCodeReturnsZeroForNull()
        {
            IEnumerable enumerable = null;

            int result = Objects.EnumerableHashCode(enumerable);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void EnumerableHashCodeHandlesEmptyEnumerable()
        {
            CustomEnumerable enumerable = new();

            int result = Objects.EnumerableHashCode(enumerable);

            Assert.AreNotEqual(0, result); // Should return base hash
        }

        [Test]
        public void EnumerableHashCodeHandlesSingleElement()
        {
            CustomEnumerable enumerable = new(42);

            int result = Objects.EnumerableHashCode(enumerable);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void EnumerableHashCodeHandlesMultipleElements()
        {
            CustomEnumerable enumerable = new(1, 2, 3, 4, 5);

            int result = Objects.EnumerableHashCode(enumerable);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void EnumerableHashCodeConsistentForSameElements()
        {
            CustomEnumerable enumerable1 = new(1, 2, 3);
            CustomEnumerable enumerable2 = new(1, 2, 3);

            int hash1 = Objects.EnumerableHashCode(enumerable1);
            int hash2 = Objects.EnumerableHashCode(enumerable2);

            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void EnumerableHashCodeDifferentForDifferentElements()
        {
            CustomEnumerable enumerable1 = new(1, 2, 3);
            CustomEnumerable enumerable2 = new(3, 2, 1);

            int hash1 = Objects.EnumerableHashCode(enumerable1);
            int hash2 = Objects.EnumerableHashCode(enumerable2);

            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void EnumerableHashCodeHandlesNullElements()
        {
            CustomEnumerable enumerable = new(1, null, 3);

            int result = Objects.EnumerableHashCode(enumerable);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void EnumerableHashCodeHandlesList()
        {
            List<int> list = new() { 1, 2, 3, 4, 5 };

            int result = Objects.EnumerableHashCode(list);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void EnumerableHashCodeHandlesArray()
        {
            int[] array = new[] { 1, 2, 3, 4, 5 };

            int result = Objects.EnumerableHashCode(array);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void EnumerableHashCodeHandlesMixedTypes()
        {
            CustomEnumerable enumerable = new(1, "two", 3.0f, true);

            int result = Objects.EnumerableHashCode(enumerable);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void EnumerableHashCodeHandlesLargeCollection()
        {
            List<int> list = new();
            for (int i = 0; i < 1000; i++)
            {
                list.Add(i);
            }

            int result = Objects.EnumerableHashCode(list);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void EnumerableHashCodeDisposesEnumerator()
        {
            DisposableEnumerable enumerable = new(1, 2, 3);

            int result = Objects.EnumerableHashCode(enumerable);

            Assert.AreNotEqual(0, result);
            Assert.IsTrue(enumerable.LastEnumerator.WasDisposed);
        }

        [Test]
        public void ValueTypeHashCodeConsistencyAcrossOverloads()
        {
            // Test that chaining works correctly
            int hash1 = Objects.ValueTypeHashCode(1);
            int hash22 = Objects.ValueTypeHashCode(1, 2);
            int hash33 = Objects.ValueTypeHashCode(1, 2, 3);

            // All should be different
            Assert.AreNotEqual(hash1, hash22);
            Assert.AreNotEqual(hash22, hash33);
            Assert.AreNotEqual(hash1, hash33);
        }

        [Test]
        public void HashCodeConsistencyAcrossOverloads()
        {
            CustomClass obj1 = new() { Value = 1 };
            CustomClass obj2 = new() { Value = 2 };
            CustomClass obj3 = new() { Value = 3 };

            int hash1 = Objects.HashCode(obj1);
            int hash2 = Objects.HashCode(obj1, obj2);
            int hash3 = Objects.HashCode(obj1, obj2, obj3);

            // All should be different
            Assert.AreNotEqual(hash1, hash2);
            Assert.AreNotEqual(hash2, hash3);
            Assert.AreNotEqual(hash1, hash3);
        }

        [Test]
        public void NullSafeHashCodeWorksWithValueTypes()
        {
            int intVal = 42;
            float floatVal = 3.14f;
            bool boolVal = true;

            Assert.AreEqual(intVal.GetHashCode(), Objects.NullSafeHashCode(intVal));
            Assert.AreEqual(floatVal.GetHashCode(), Objects.NullSafeHashCode(floatVal));
            Assert.AreEqual(boolVal.GetHashCode(), Objects.NullSafeHashCode(boolVal));
        }

        [Test]
        public void ValueTypeHashCodeWorksWithDifferentValueTypes()
        {
            int a = 1;
            long b = 2L;
            float c = 3.0f;
            double d = 4.0;

            int result = Objects.ValueTypeHashCode(a, b, c, d);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void HashCodeHandlesCustomObjectsWithSameHashCode()
        {
            CustomClass obj1 = new() { Value = 100 };
            CustomClass obj2 = new() { Value = 100 };

            // Same hash from objects, but should still produce consistent results
            int hash1 = Objects.HashCode(obj1);
            int hash2 = Objects.HashCode(obj2);

            // Since objects have same hash code, results should be equal
            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void EnumerableHashCodeOrderMatters()
        {
            CustomEnumerable enum1 = new(1, 2, 3);
            CustomEnumerable enum2 = new(3, 2, 1);

            int hash1 = Objects.EnumerableHashCode(enum1);
            int hash2 = Objects.EnumerableHashCode(enum2);

            // Order should affect hash
            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void ValueTypeHashCodeHandlesNegativeNumbers()
        {
            int result = Objects.ValueTypeHashCode(-1, -2, -3);

            Assert.AreNotEqual(0, result);
        }

        [Test]
        public void ValueTypeHashCodeHandlesMaxAndMinValues()
        {
            int resultMax = Objects.ValueTypeHashCode(int.MaxValue, long.MaxValue);
            int resultMin = Objects.ValueTypeHashCode(int.MinValue, long.MinValue);

            Assert.AreNotEqual(0, resultMax);
            Assert.AreNotEqual(0, resultMin);
            Assert.AreNotEqual(resultMax, resultMin);
        }
    }
}
