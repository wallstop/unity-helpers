namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class SerializableDictionaryPropertyDrawerTests : CommonTestBase
    {
        private sealed class TestDictionaryHost : ScriptableObject
        {
            public IntStringDictionary dictionary = new();
        }

        private sealed class StringDictionaryHost : ScriptableObject
        {
            public StringStringDictionary dictionary = new();
        }

        private sealed class TestSortedDictionaryHost : ScriptableObject
        {
            public SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();
        }

        private sealed class RectDictionaryHost : ScriptableObject
        {
            public RectIntDictionary dictionary = new RectIntDictionary();
        }

        [Serializable]
        private sealed class IntStringDictionary : SerializableDictionary<int, string> { }

        [Serializable]
        private sealed class StringStringDictionary : SerializableDictionary<string, string> { }

        [Serializable]
        private sealed class RectIntDictionary : SerializableDictionary<Rect, int> { }

        [Test]
        public void PageSizeClampPreventsExcessiveCacheGrowth()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            for (int i = 0; i < 512; i++)
            {
                host.dictionary.Add(i, $"Value {i}");
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();

            SerializableDictionaryPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(dictionaryProperty);
            pagination.pageSize = 512;

            drawer.GetOrCreateList(dictionaryProperty, keysProperty, valuesProperty);

            Assert.AreEqual(SerializableDictionaryPropertyDrawer.MaxPageSize, pagination.pageSize);
        }

        [Test]
        public void RowHeightReflectsSerializedPropertyHeights()
        {
            RectDictionaryHost host = CreateScriptableObject<RectDictionaryHost>();
            host.dictionary.Add(new Rect(1f, 2f, 3f, 4f), 42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(RectDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            ReorderableList list = drawer.GetOrCreateList(
                dictionaryProperty,
                keysProperty,
                valuesProperty
            );

            Assert.IsNotNull(
                list.elementHeightCallback,
                "Expected dynamic element height callback."
            );
            float resolvedHeight = list.elementHeightCallback.Invoke(0);

            SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(0);
            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(0);

            float expectedHeight =
                SerializableDictionaryPropertyDrawer.CalculateDictionaryRowHeight(
                    keyProperty,
                    valueProperty
                );

            Assert.That(resolvedHeight, Is.EqualTo(expectedHeight));
        }

        [Test]
        public void ExpandDictionaryRowRectExtendsSelectionArea()
        {
            Rect baseRect = new Rect(2f, 6f, 40f, 18f);
            Rect expanded = SerializableDictionaryPropertyDrawer.ExpandDictionaryRowRect(baseRect);

            Assert.That(expanded.yMin, Is.LessThan(baseRect.yMin));
            Assert.That(expanded.yMax, Is.GreaterThan(baseRect.yMax));
            Assert.That(expanded.width, Is.EqualTo(baseRect.width));
        }

        [Test]
        public void SyncSelectionKeepsIndexWithinVisiblePage()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            for (int i = 0; i < 30; i++)
            {
                host.dictionary.Add(i, $"Item {i}");
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();

            SerializableDictionaryPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(dictionaryProperty);
            pagination.pageSize = 10;
            pagination.pageIndex = 0;

            ReorderableList list = drawer.GetOrCreateList(
                dictionaryProperty,
                keysProperty,
                valuesProperty
            );

            string listKey = SerializableDictionaryPropertyDrawer.GetListKey(dictionaryProperty);

            SerializableDictionaryPropertyDrawer.ListPageCache cache = drawer.EnsurePageCache(
                listKey,
                keysProperty,
                pagination
            );

            pagination.selectedIndex = 25;
            pagination.pageIndex = 2;
            cache = drawer.EnsurePageCache(listKey, keysProperty, pagination);
            SerializableDictionaryPropertyDrawer.SyncListSelectionWithPagination(
                list,
                pagination,
                cache
            );

            Assert.AreEqual(5, list.index);
            Assert.AreEqual(25, pagination.selectedIndex);

            pagination.pageIndex = 0;
            cache = drawer.EnsurePageCache(listKey, keysProperty, pagination);
            SerializableDictionaryPropertyDrawer.SyncListSelectionWithPagination(
                list,
                pagination,
                cache
            );

            Assert.AreEqual(0, list.index);
            Assert.AreEqual(0, pagination.selectedIndex);
        }

        [Test]
        public void MarkListCacheDirtyClearsCachedEntries()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            for (int i = 0; i < 20; i++)
            {
                host.dictionary.Add(i, $"Entry {i}");
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();

            SerializableDictionaryPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(dictionaryProperty);

            drawer.GetOrCreateList(dictionaryProperty, keysProperty, valuesProperty);

            string listKey = SerializableDictionaryPropertyDrawer.GetListKey(dictionaryProperty);

            SerializableDictionaryPropertyDrawer.ListPageCache cache = drawer.EnsurePageCache(
                listKey,
                keysProperty,
                pagination
            );

            IList entries = cache.entries;
            Assert.Greater(entries.Count, 0);
            Assert.IsFalse(cache.dirty);

            drawer.MarkListCacheDirty(listKey);

            Assert.AreEqual(0, entries.Count);
            Assert.IsTrue(cache.dirty);
        }

        [Test]
        public void RemoveEntryAdjustsSelectionWithinPage()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            for (int i = 0; i < 30; i++)
            {
                host.dictionary.Add(i, $"Item {i}");
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();

            SerializableDictionaryPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(dictionaryProperty);
            pagination.pageSize = 10;
            pagination.pageIndex = 2;
            pagination.selectedIndex = 25;

            ReorderableList list = drawer.GetOrCreateList(
                dictionaryProperty,
                keysProperty,
                valuesProperty
            );

            string listKey = SerializableDictionaryPropertyDrawer.GetListKey(dictionaryProperty);
            SerializableDictionaryPropertyDrawer.ListPageCache cache = drawer.EnsurePageCache(
                listKey,
                keysProperty,
                pagination
            );
            SerializableDictionaryPropertyDrawer.SyncListSelectionWithPagination(
                list,
                pagination,
                cache
            );

            drawer.RemoveEntryAtIndex(
                25,
                list,
                dictionaryProperty,
                keysProperty,
                valuesProperty,
                pagination
            );

            cache = drawer.EnsurePageCache(listKey, keysProperty, pagination);
            SerializableDictionaryPropertyDrawer.SyncListSelectionWithPagination(
                list,
                pagination,
                cache
            );

            Assert.AreEqual(25, pagination.selectedIndex);
            Assert.AreEqual(5, list.index);
            Assert.AreEqual(2, pagination.pageIndex);
        }

        [Test]
        public void EvaluateDuplicateTweenOffsetHonorsCycleLimit()
        {
            const double startTime = 0d;
            const double activeTime = 0.1432d;

            float activeOffset = SerializableDictionaryPropertyDrawer.EvaluateDuplicateTweenOffset(
                0,
                startTime,
                activeTime,
                2
            );

            Assert.That(Mathf.Abs(activeOffset), Is.GreaterThan(1e-3f));

            float exhaustedOffset =
                SerializableDictionaryPropertyDrawer.EvaluateDuplicateTweenOffset(
                    0,
                    startTime,
                    startTime + 10d,
                    1
                );

            Assert.AreEqual(0f, exhaustedOffset);
        }

        [Test]
        public void EvaluateDuplicateTweenOffsetSupportsInfiniteCycles()
        {
            float offset = SerializableDictionaryPropertyDrawer.EvaluateDuplicateTweenOffset(
                2,
                0d,
                100d,
                -1
            );

            Assert.That(Mathf.Abs(offset), Is.GreaterThan(1e-3f));
        }

        [Test]
        public void SortDictionaryEntriesReordersKeysAscending()
        {
            TestSortedDictionaryHost host = CreateScriptableObject<TestSortedDictionaryHost>();
            host.dictionary.Add(5, "five");
            host.dictionary.Add(1, "one");
            host.dictionary.Add(3, "three");

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestSortedDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            keysProperty.GetArrayElementAtIndex(0).intValue = 5;
            valuesProperty.GetArrayElementAtIndex(0).stringValue = "five";
            keysProperty.GetArrayElementAtIndex(1).intValue = 1;
            valuesProperty.GetArrayElementAtIndex(1).stringValue = "one";
            keysProperty.GetArrayElementAtIndex(2).intValue = 3;
            valuesProperty.GetArrayElementAtIndex(2).stringValue = "three";
            serializedObject.ApplyModifiedProperties();

            SerializableDictionaryPropertyDrawer drawer =
                new SerializableDictionaryPropertyDrawer();
            SerializableDictionaryPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(dictionaryProperty);
            pagination.selectedIndex = 0;

            ReorderableList list = drawer.GetOrCreateList(
                dictionaryProperty,
                keysProperty,
                valuesProperty
            );
            string listKey = SerializableDictionaryPropertyDrawer.GetListKey(dictionaryProperty);
            Func<object, object, int> comparison = delegate(object left, object right)
            {
                int leftValue = left is int leftInt ? leftInt : Convert.ToInt32(left);
                int rightValue = right is int rightInt ? rightInt : Convert.ToInt32(right);
                return Comparer<int>.Default.Compare(leftValue, rightValue);
            };
            Func<SerializableDictionaryPropertyDrawer.ListPageCache> cacheProvider = () =>
                drawer.EnsurePageCache(listKey, keysProperty, pagination);

            drawer.SortDictionaryEntries(
                dictionaryProperty,
                keysProperty,
                valuesProperty,
                typeof(int),
                typeof(string),
                comparison,
                pagination,
                list
            );

            serializedObject.Update();

            Assert.AreEqual(1, keysProperty.GetArrayElementAtIndex(0).intValue);
            Assert.AreEqual("one", valuesProperty.GetArrayElementAtIndex(0).stringValue);
            Assert.AreEqual(3, keysProperty.GetArrayElementAtIndex(1).intValue);
            Assert.AreEqual("three", valuesProperty.GetArrayElementAtIndex(1).stringValue);
            Assert.AreEqual(5, keysProperty.GetArrayElementAtIndex(2).intValue);
            Assert.AreEqual("five", valuesProperty.GetArrayElementAtIndex(2).stringValue);

            int[] expectedKeys = new int[] { 1, 3, 5 };
            int index = 0;
            foreach (KeyValuePair<int, string> pair in host.dictionary)
            {
                Assert.Less(index, expectedKeys.Length);
                Assert.AreEqual(expectedKeys[index], pair.Key);
                index++;
            }

            Assert.AreEqual(expectedKeys.Length, index);
            Assert.AreEqual(2, pagination.selectedIndex);
        }

        [Test]
        public void KeysAreSortedDetectsOrderedState()
        {
            TestSortedDictionaryHost host = CreateScriptableObject<TestSortedDictionaryHost>();
            host.dictionary.Add(2, "two");
            host.dictionary.Add(4, "four");
            host.dictionary.Add(6, "six");

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestSortedDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            Func<object, object, int> comparison = delegate(object left, object right)
            {
                int leftValue = left is int leftInt ? leftInt : Convert.ToInt32(left);
                int rightValue = right is int rightInt ? rightInt : Convert.ToInt32(right);
                return Comparer<int>.Default.Compare(leftValue, rightValue);
            };

            bool initiallySorted = SerializableDictionaryPropertyDrawer.KeysAreSorted(
                keysProperty,
                typeof(int),
                comparison
            );
            Assert.IsTrue(initiallySorted);

            keysProperty.GetArrayElementAtIndex(0).intValue = 6;
            valuesProperty.GetArrayElementAtIndex(0).stringValue = "six";
            keysProperty.GetArrayElementAtIndex(2).intValue = 2;
            valuesProperty.GetArrayElementAtIndex(2).stringValue = "two";
            serializedObject.ApplyModifiedProperties();

            bool sortedAfterSwap = SerializableDictionaryPropertyDrawer.KeysAreSorted(
                keysProperty,
                typeof(int),
                comparison
            );
            Assert.IsFalse(sortedAfterSwap);
        }

        [Test]
        public void EvaluateDuplicateTweenOffsetHandlesCurrentTimeBeforeStart()
        {
            float offset = SerializableDictionaryPropertyDrawer.EvaluateDuplicateTweenOffset(
                1,
                10d,
                9d,
                3
            );
            float baseline = SerializableDictionaryPropertyDrawer.EvaluateDuplicateTweenOffset(
                1,
                9d,
                9d,
                3
            );

            Assert.AreEqual(baseline, offset);
        }

        [Test]
        public void EvaluateDuplicateTweenOffsetReturnsZeroWhenCycleLimitZero()
        {
            float offset = SerializableDictionaryPropertyDrawer.EvaluateDuplicateTweenOffset(
                0,
                0d,
                1d,
                0
            );

            Assert.AreEqual(0f, offset);
        }
    }
}
