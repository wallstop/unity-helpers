namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections;
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

        [Serializable]
        private sealed class IntStringDictionary : SerializableDictionary<int, string> { }

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
