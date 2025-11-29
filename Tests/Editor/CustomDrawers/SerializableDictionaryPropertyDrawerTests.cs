namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.Utils;
    using WallstopStudios.UnityHelpers.Utils;

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

        private sealed class ComplexValueDictionaryHost : ScriptableObject
        {
            public StringComplexDictionary dictionary = new();
        }

        private sealed class ScriptableObjectDictionaryHost : ScriptableObject
        {
            public StringScriptableDictionary dictionary = new();
        }

        private sealed class PrivateComplexDictionaryHost : ScriptableObject
        {
            public PrivateComplexDictionary dictionary = new();
        }

        private sealed class TestSortedDictionaryHost : ScriptableObject
        {
            public SerializableSortedDictionary<int, string> dictionary = new();
        }

        private sealed class RectDictionaryHost : ScriptableObject
        {
            public RectIntDictionary dictionary = new();
        }

        private sealed class UnityObjectDictionaryHost : ScriptableObject
        {
            public GameObjectStringDictionary dictionary = new();
        }

        private sealed class PrivateCtorDictionaryHost : ScriptableObject
        {
            public PrivateCtorDictionary dictionary = new();
        }

        [Serializable]
        private sealed class IntStringDictionary : SerializableDictionary<int, string> { }

        [Serializable]
        private sealed class StringStringDictionary : SerializableDictionary<string, string> { }

        [Serializable]
        private sealed class StringComplexDictionary
            : SerializableDictionary<string, ComplexValue> { }

        [Serializable]
        private sealed class StringScriptableDictionary
            : SerializableDictionary<string, SampleScriptableObject> { }

        [Serializable]
        private sealed class PrivateComplexDictionary
            : SerializableDictionary<string, PrivateComplexValue> { }

        [Serializable]
        private sealed class GameObjectStringDictionary
            : SerializableDictionary<GameObject, string> { }

        [Serializable]
        private sealed class ComplexValue
        {
            public Color button;
            public Color text;
        }

        [Serializable]
        private sealed class SampleScriptableObject : ScriptableObject { }

        [Serializable]
        private sealed class PrivateComplexValue
        {
            [SerializeField]
            private Color primary = Color.white;

            [SerializeField]
            private Color secondary = Color.black;

            public Color Primary
            {
                get => primary;
                set => primary = value;
            }

            public Color Secondary
            {
                get => secondary;
                set => secondary = value;
            }
        }

        [Serializable]
        private sealed class RectIntDictionary : SerializableDictionary<Rect, int> { }

        [Serializable]
        private sealed class PrivateCtorDictionary
            : SerializableDictionary<PrivateCtorKey, PrivateCtorValue> { }

        [Serializable]
        private sealed class PrivateCtorKey
        {
            [SerializeField]
            private string token;

            public string Token => token;

            private PrivateCtorKey()
            {
                token = Guid.NewGuid().ToString();
            }
        }

        [Serializable]
        private sealed class PrivateCtorValue
        {
            [SerializeField]
            private Color accent = Color.magenta;

            [SerializeField]
            private float intensity = 1f;

            private PrivateCtorValue() { }

            public Color Accent => accent;
            public float Intensity => intensity;
        }

        [Serializable]
        private struct PendingStructValue
        {
            public string label;
            public Color tint;
        }

        [Serializable]
        private struct ColorData
        {
            public Color color1;
            public Color color2;
            public Color color3;
            public Color color4;
            public Color[] otherColors;
        }

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

            SerializableDictionaryPropertyDrawer drawer = new();
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalPageSize = settings.SerializableDictionaryPageSize;

            try
            {
                int configuredSize = UnityHelpersSettings.MinPageSize + 23;
                settings.SerializableDictionaryPageSize = configuredSize;

                SerializableDictionaryPropertyDrawer.PaginationState pagination =
                    drawer.GetOrCreatePaginationState(dictionaryProperty);
                drawer.GetOrCreateList(dictionaryProperty);

                string listKey = SerializableDictionaryPropertyDrawer.GetListKey(
                    dictionaryProperty
                );
                SerializableDictionaryPropertyDrawer.ListPageCache cache = drawer.EnsurePageCache(
                    listKey,
                    keysProperty,
                    pagination
                );

                int expectedSize = UnityHelpersSettings.GetSerializableDictionaryPageSize();
                Assert.AreEqual(
                    expectedSize,
                    pagination.pageSize,
                    "Pagination state should mirror the configured page size."
                );
                Assert.That(
                    cache.entries.Count,
                    Is.EqualTo(Mathf.Min(expectedSize, keysProperty.arraySize)),
                    "List cache should never allocate more entries than the configured page size."
                );
            }
            finally
            {
                settings.SerializableDictionaryPageSize = originalPageSize;
            }
        }

        [Test]
        public void PaginationStateUsesSerializableDictionaryPageSizeSetting()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            for (int i = 0; i < 64; i++)
            {
                host.dictionary.Add(i, $"Value {i}");
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalPageSize = settings.SerializableDictionaryPageSize;

            try
            {
                int configuredSize = UnityHelpersSettings.MinPageSize + 7;
                settings.SerializableDictionaryPageSize = configuredSize;

                SerializableDictionaryPropertyDrawer drawer = new();
                SerializableDictionaryPropertyDrawer.PaginationState pagination =
                    drawer.GetOrCreatePaginationState(dictionaryProperty);

                Assert.AreEqual(
                    UnityHelpersSettings.GetSerializableDictionaryPageSize(),
                    pagination.pageSize,
                    "Pagination should respect the configured SerializableDictionary page size."
                );
            }
            finally
            {
                settings.SerializableDictionaryPageSize = originalPageSize;
            }
        }

        [Test]
        public void PaginationStateResetsIndexWhenPageSizeChanges()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            for (int i = 0; i < 120; i++)
            {
                host.dictionary.Add(i, $"Value {i}");
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalPageSize = settings.SerializableDictionaryPageSize;

            try
            {
                settings.SerializableDictionaryPageSize = 20;

                SerializableDictionaryPropertyDrawer drawer = new();
                SerializableDictionaryPropertyDrawer.PaginationState pagination =
                    drawer.GetOrCreatePaginationState(dictionaryProperty);
                pagination.pageIndex = 3;

                settings.SerializableDictionaryPageSize = 30;

                pagination = drawer.GetOrCreatePaginationState(dictionaryProperty);

                Assert.AreEqual(
                    UnityHelpersSettings.GetSerializableDictionaryPageSize(),
                    pagination.pageSize
                );
                Assert.AreEqual(
                    0,
                    pagination.pageIndex,
                    "Changing the global page size should reset the cached pagination index."
                );
            }
            finally
            {
                settings.SerializableDictionaryPageSize = originalPageSize;
            }
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
            ReorderableList list = drawer.GetOrCreateList(dictionaryProperty);

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
        public void SetPropertyValueHandlesSerializableClassValues()
        {
            ComplexValueDictionaryHost host = CreateScriptableObject<ComplexValueDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ComplexValueDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            keysProperty.arraySize = 1;
            valuesProperty.arraySize = 1;

            SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(0);
            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(0);

            SerializableDictionaryPropertyDrawer.SetPropertyValue(
                keyProperty,
                "Alert",
                typeof(string)
            );

            ComplexValue complexValue = new() { button = Color.magenta, text = Color.white };

            SerializableDictionaryPropertyDrawer.SetPropertyValue(
                valueProperty,
                complexValue,
                typeof(ComplexValue)
            );

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            host.dictionary.EditorAfterDeserialize();

            Assert.That(host.dictionary.Count, Is.EqualTo(1));

            ComplexValue stored = host.dictionary["Alert"];
            Assert.That(stored.button, Is.EqualTo(complexValue.button));
            Assert.That(stored.text, Is.EqualTo(complexValue.text));

            object roundTrip = SerializableDictionaryPropertyDrawer.GetPropertyValue(
                valueProperty,
                typeof(ComplexValue)
            );

            ComplexValue roundTripValue = roundTrip as ComplexValue;
            Assert.IsNotNull(roundTripValue);
            Assert.That(roundTripValue.button, Is.EqualTo(complexValue.button));
            Assert.That(roundTripValue.text, Is.EqualTo(complexValue.text));
        }

        [Test]
        public void ExpandDictionaryRowRectExtendsSelectionArea()
        {
            Rect baseRect = new(2f, 6f, 40f, 18f);
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

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalPageSize = settings.SerializableDictionaryPageSize;

            try
            {
                settings.SerializableDictionaryPageSize = 10;

                SerializableDictionaryPropertyDrawer drawer = new();

                SerializableDictionaryPropertyDrawer.PaginationState pagination =
                    drawer.GetOrCreatePaginationState(dictionaryProperty);
                pagination.pageIndex = 0;

                ReorderableList list = drawer.GetOrCreateList(dictionaryProperty);

                string listKey = SerializableDictionaryPropertyDrawer.GetListKey(
                    dictionaryProperty
                );

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
            finally
            {
                settings.SerializableDictionaryPageSize = originalPageSize;
            }
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

            SerializableDictionaryPropertyDrawer drawer = new();

            SerializableDictionaryPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(dictionaryProperty);

            drawer.GetOrCreateList(dictionaryProperty);

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

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalPageSize = settings.SerializableDictionaryPageSize;

            try
            {
                settings.SerializableDictionaryPageSize = 10;

                SerializableDictionaryPropertyDrawer drawer = new();

                SerializableDictionaryPropertyDrawer.PaginationState pagination =
                    drawer.GetOrCreatePaginationState(dictionaryProperty);
                pagination.pageIndex = 2;
                pagination.selectedIndex = 25;

                ReorderableList list = drawer.GetOrCreateList(dictionaryProperty);

                string listKey = SerializableDictionaryPropertyDrawer.GetListKey(
                    dictionaryProperty
                );
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
            finally
            {
                settings.SerializableDictionaryPageSize = originalPageSize;
            }
        }

        [Test]
        public void PageCacheRebuildsWhenGlobalPageSizeChanges()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            for (int i = 0; i < 40; i++)
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

            SerializableDictionaryPropertyDrawer drawer = new();
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalPageSize = settings.SerializableDictionaryPageSize;

            try
            {
                int initialPageSize = UnityHelpersSettings.MinPageSize + 10;
                int reducedPageSize = UnityHelpersSettings.MinPageSize + 2;
                settings.SerializableDictionaryPageSize = initialPageSize;

                SerializableDictionaryPropertyDrawer.PaginationState pagination =
                    drawer.GetOrCreatePaginationState(dictionaryProperty);
                string listKey = SerializableDictionaryPropertyDrawer.GetListKey(
                    dictionaryProperty
                );

                SerializableDictionaryPropertyDrawer.ListPageCache cache = drawer.EnsurePageCache(
                    listKey,
                    keysProperty,
                    pagination
                );
                Assert.AreEqual(
                    UnityHelpersSettings.GetSerializableDictionaryPageSize(),
                    pagination.pageSize,
                    "Initial pagination should use the configured size."
                );
                Assert.That(cache.entries.Count, Is.EqualTo(initialPageSize));

                settings.SerializableDictionaryPageSize = reducedPageSize;

                pagination = drawer.GetOrCreatePaginationState(dictionaryProperty);
                cache = drawer.EnsurePageCache(listKey, keysProperty, pagination);

                Assert.AreEqual(
                    UnityHelpersSettings.GetSerializableDictionaryPageSize(),
                    pagination.pageSize,
                    "Pagination should refresh when the global size changes."
                );
                Assert.That(cache.entries.Count, Is.EqualTo(reducedPageSize));
                Assert.AreEqual(
                    0,
                    pagination.pageIndex,
                    "Page index should reset after a size change."
                );
            }
            finally
            {
                settings.SerializableDictionaryPageSize = originalPageSize;
            }
        }

        [Test]
        public void RemoveEntryBacktracksToPreviousPageWhenLastPageIsRemoved()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            for (int i = 0; i < 21; i++)
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

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            int originalPageSize = settings.SerializableDictionaryPageSize;

            try
            {
                settings.SerializableDictionaryPageSize = 10;

                SerializableDictionaryPropertyDrawer drawer = new();
                SerializableDictionaryPropertyDrawer.PaginationState pagination =
                    drawer.GetOrCreatePaginationState(dictionaryProperty);
                pagination.pageIndex = 2;
                pagination.selectedIndex = 20;

                ReorderableList list = drawer.GetOrCreateList(dictionaryProperty);
                string listKey = SerializableDictionaryPropertyDrawer.GetListKey(
                    dictionaryProperty
                );

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
                    20,
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

                Assert.AreEqual(
                    1,
                    pagination.pageIndex,
                    "Removing the lone element on the last page should move to the previous page."
                );
                Assert.AreEqual(
                    19,
                    pagination.selectedIndex,
                    "Selection should clamp to the new last element."
                );
                Assert.AreEqual(
                    9,
                    list.index,
                    "Relative list selection should point to the last item on the page."
                );
            }
            finally
            {
                settings.SerializableDictionaryPageSize = originalPageSize;
            }
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

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(dictionaryProperty);
            pagination.selectedIndex = 0;

            ReorderableList list = drawer.GetOrCreateList(dictionaryProperty);

            drawer.SortDictionaryEntries(
                dictionaryProperty,
                keysProperty,
                valuesProperty,
                typeof(int),
                typeof(string),
                Comparison,
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

            int[] expectedKeys = { 1, 3, 5 };
            int index = 0;
            foreach (KeyValuePair<int, string> pair in host.dictionary)
            {
                Assert.Less(index, expectedKeys.Length);
                Assert.AreEqual(expectedKeys[index], pair.Key);
                index++;
            }

            Assert.AreEqual(expectedKeys.Length, index);
            Assert.AreEqual(2, pagination.selectedIndex);
            return;

            int Comparison(object left, object right)
            {
                int leftValue = left is int leftInt ? leftInt : Convert.ToInt32(left);
                int rightValue = right is int rightInt ? rightInt : Convert.ToInt32(right);
                return Comparer<int>.Default.Compare(leftValue, rightValue);
            }
        }

        [Test]
        public void SortDictionaryEntriesUsesUnityObjectNameComparerForObjectKeys()
        {
            UnityObjectDictionaryHost host = CreateScriptableObject<UnityObjectDictionaryHost>();
            GameObject beta = Track(new GameObject("Beta"));
            GameObject alpha = Track(new GameObject("Alpha"));

            host.dictionary.Add(beta, "beta");
            host.dictionary.Add(alpha, "alpha");

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(UnityObjectDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            keysProperty.GetArrayElementAtIndex(0).objectReferenceValue = beta;
            valuesProperty.GetArrayElementAtIndex(0).stringValue = "beta";
            keysProperty.GetArrayElementAtIndex(1).objectReferenceValue = alpha;
            valuesProperty.GetArrayElementAtIndex(1).stringValue = "alpha";
            serializedObject.ApplyModifiedProperties();

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(dictionaryProperty);
            pagination.selectedIndex = 0;

            ReorderableList list = drawer.GetOrCreateList(dictionaryProperty);

            drawer.SortDictionaryEntries(
                dictionaryProperty,
                keysProperty,
                valuesProperty,
                typeof(GameObject),
                typeof(string),
                Comparison,
                pagination,
                list
            );

            serializedObject.Update();

            Assert.AreSame(alpha, keysProperty.GetArrayElementAtIndex(0).objectReferenceValue);
            Assert.AreEqual("alpha", valuesProperty.GetArrayElementAtIndex(0).stringValue);
            Assert.AreSame(beta, keysProperty.GetArrayElementAtIndex(1).objectReferenceValue);
            Assert.AreEqual("beta", valuesProperty.GetArrayElementAtIndex(1).stringValue);

            GameObject[] expectedOrder = { alpha, beta };
            int index = 0;
            foreach (KeyValuePair<GameObject, string> pair in host.dictionary)
            {
                Assert.Less(index, expectedOrder.Length);
                Assert.AreSame(expectedOrder[index], pair.Key);
                index++;
            }

            Assert.AreEqual(expectedOrder.Length, index);
            return;

            int Comparison(object left, object right)
            {
                return UnityObjectNameComparer<GameObject>.Instance.Compare(
                    left as GameObject,
                    right as GameObject
                );
            }
        }

        [Test]
        public void DictionarySortButtonVisibilityReflectsOrdering()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary.Add(2, "two");
            host.dictionary.Add(1, "one");

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );

            Func<object, object, int> comparison = delegate(object left, object right)
            {
                int leftValue = left is int leftInt ? leftInt : Convert.ToInt32(left);
                int rightValue = right is int rightInt ? rightInt : Convert.ToInt32(right);
                return leftValue.CompareTo(rightValue);
            };

            bool showBefore = SerializableDictionaryPropertyDrawer.ShouldShowDictionarySortButton(
                keysProperty,
                typeof(int),
                keysProperty.arraySize,
                comparison
            );
            Assert.IsTrue(showBefore);

            keysProperty.GetArrayElementAtIndex(0).intValue = 1;
            keysProperty.GetArrayElementAtIndex(1).intValue = 2;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );

            bool showAfter = SerializableDictionaryPropertyDrawer.ShouldShowDictionarySortButton(
                keysProperty,
                typeof(int),
                keysProperty.arraySize,
                comparison
            );
            Assert.IsFalse(showAfter);
        }

        [Test]
        public void SortedDictionaryManualReorderShowsSortButton()
        {
            TestSortedDictionaryHost host = CreateScriptableObject<TestSortedDictionaryHost>();
            host.dictionary.Add(1, "one");
            host.dictionary.Add(2, "two");
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

            keysProperty.MoveArrayElement(0, 2);
            valuesProperty.MoveArrayElement(0, 2);
            serializedObject.ApplyModifiedProperties();

            bool beforeDeserialize =
                SerializableDictionaryPropertyDrawer.ShouldShowDictionarySortButton(
                    keysProperty,
                    typeof(int),
                    keysProperty.arraySize,
                    Comparison
                );
            Assert.IsTrue(
                beforeDeserialize,
                $"Sort button should be visible before the sorted dictionary rehydrates. Keys: {DumpIntArray(keysProperty)}"
            );

            host.dictionary.OnAfterDeserialize();
            serializedObject.Update();
            dictionaryProperty = serializedObject.FindProperty(
                nameof(TestSortedDictionaryHost.dictionary)
            );
            keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );

            bool showSort = SerializableDictionaryPropertyDrawer.ShouldShowDictionarySortButton(
                keysProperty,
                typeof(int),
                keysProperty.arraySize,
                Comparison
            );

            Assert.IsFalse(
                showSort,
                $"Sorted dictionaries reorder entries immediately. Keys: {DumpIntArray(keysProperty)}"
            );
            Assert.IsFalse(
                host.dictionary.PreserveSerializedEntries,
                "Sorted dictionary should not preserve serialized entries after it reorders keys automatically."
            );
            return;

            int Comparison(object left, object right)
            {
                int leftValue = left is int leftInt ? leftInt : Convert.ToInt32(left);
                int rightValue = right is int rightInt ? rightInt : Convert.ToInt32(right);
                return leftValue.CompareTo(rightValue);
            }
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

        [Test]
        public void CommitEntryAddsComplexValue()
        {
            ComplexValueDictionaryHost host = CreateScriptableObject<ComplexValueDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ComplexValueDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(ComplexValue),
                "Alert",
                new ComplexValue { button = Color.magenta, text = Color.white },
                dictionaryProperty
            );
            Assert.IsTrue(result.added, "Expected CommitEntry to add a new element.");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            host.dictionary.EditorAfterDeserialize();

            Assert.That(host.dictionary.Count, Is.EqualTo(1));
            Assert.That(host.dictionary.ContainsKey("Alert"), Is.True);

            ReorderableList list = drawer.GetOrCreateList(dictionaryProperty);
            Assert.That(list.count, Is.EqualTo(1));
        }

        [Test]
        public void GetDefaultValueSupportsPrivateSerializableTypes()
        {
            object value = SerializableDictionaryPropertyDrawer.GetDefaultValue(
                typeof(PrivateComplexValue)
            );

            Assert.IsNotNull(value);
            Assert.IsInstanceOf<PrivateComplexValue>(value);
        }

        [Test]
        public void GetDefaultValueReturnsNullForUnityObjectTypes()
        {
            object gameObjectDefault = SerializableDictionaryPropertyDrawer.GetDefaultValue(
                typeof(GameObject)
            );
            object scriptableDefault = SerializableDictionaryPropertyDrawer.GetDefaultValue(
                typeof(SampleScriptableObject)
            );

            Assert.IsNull(gameObjectDefault);
            Assert.IsNull(scriptableDefault);
        }

        [Test]
        public void CommitEntryAddsPrivateComplexValue()
        {
            PrivateComplexDictionaryHost host =
                CreateScriptableObject<PrivateComplexDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(PrivateComplexDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            PrivateComplexValue valueInstance = new()
            {
                Primary = Color.yellow,
                Secondary = Color.green,
            };

            SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(PrivateComplexValue),
                "Accent",
                valueInstance,
                dictionaryProperty
            );
            Assert.IsTrue(result.added, "Expected CommitEntry to add the new complex value.");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializedProperty valueElement = valuesProperty.GetArrayElementAtIndex(0);
            Assert.IsNotNull(valueElement, "Expected value element to exist after commit.");

            SerializedProperty primaryProperty = valueElement.FindPropertyRelative("primary");
            SerializedProperty secondaryProperty = valueElement.FindPropertyRelative("secondary");

            Assert.IsNotNull(
                primaryProperty,
                "Expected primary color property to exist on committed value."
            );
            Assert.IsNotNull(
                secondaryProperty,
                "Expected secondary color property to exist on committed value."
            );

            Assert.That(primaryProperty.colorValue, Is.EqualTo(Color.yellow));
            Assert.That(secondaryProperty.colorValue, Is.EqualTo(Color.green));

            host.dictionary.EditorAfterDeserialize();
            Assert.That(host.dictionary.Count, Is.EqualTo(1));
            Assert.That(host.dictionary.ContainsKey("Accent"), Is.True);
        }

        [Test]
        public void GetDefaultValueSupportsTypesWithPrivateConstructors()
        {
            object keyDefault = SerializableDictionaryPropertyDrawer.GetDefaultValue(
                typeof(PrivateCtorKey)
            );
            object valueDefault = SerializableDictionaryPropertyDrawer.GetDefaultValue(
                typeof(PrivateCtorValue)
            );

            Assert.IsInstanceOf<PrivateCtorKey>(keyDefault);
            Assert.IsInstanceOf<PrivateCtorValue>(valueDefault);
        }

        [Test]
        public void PendingEntryDefaultsIncludePrivateConstructorTypes()
        {
            PrivateCtorDictionaryHost host = CreateScriptableObject<PrivateCtorDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(PrivateCtorDictionaryHost.dictionary)
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                drawer,
                dictionaryProperty,
                typeof(PrivateCtorKey),
                typeof(PrivateCtorValue),
                isSortedDictionary: false
            );

            Assert.IsInstanceOf<PrivateCtorKey>(
                pending.key,
                "Pending key should use private constructor default."
            );
            Assert.IsInstanceOf<PrivateCtorValue>(
                pending.value,
                "Pending value should use private constructor default."
            );
        }

        [Test]
        public void PendingEntryDefaultsRemainNullForUnityObjects()
        {
            ScriptableObjectDictionaryHost host =
                CreateScriptableObject<ScriptableObjectDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ScriptableObjectDictionaryHost.dictionary)
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                drawer,
                dictionaryProperty,
                typeof(string),
                typeof(SampleScriptableObject),
                isSortedDictionary: false
            );

            Assert.IsNull(
                pending.value,
                "UnityEngine.Object values should remain null so the object picker can be used."
            );
        }

        [UnityTest]
        public IEnumerator ManualEntryUsesObjectPickerForScriptableObjectKeys()
        {
            SerializableDictionaryPropertyDrawer.PendingEntry pending = new();
            Rect rect = new(0f, 0f, 200f, EditorGUIUtility.singleLineHeight);

            yield return TestIMGUIExecutor.Run(() =>
            {
                SerializableDictionaryPropertyDrawer.DrawFieldForType(
                    rect,
                    "Key",
                    null,
                    typeof(SampleScriptableObject),
                    pending,
                    isValueField: false
                );
            });

            Assert.IsNull(
                pending.keyWrapper,
                "ScriptableObject keys should not allocate PendingValueWrappers."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntrySupportsStructValues()
        {
            SerializableDictionaryPropertyDrawer.PendingEntry pending = new();
            PendingStructValue initial = new() { label = "Alpha", tint = Color.cyan };
            Rect rect = new(0f, 0f, 260f, EditorGUIUtility.singleLineHeight * 3f);

            yield return TestIMGUIExecutor.Run(() =>
            {
                pending.value = initial;
                pending.value = SerializableDictionaryPropertyDrawer.DrawFieldForType(
                    rect,
                    "Value",
                    pending.value,
                    typeof(PendingStructValue),
                    pending,
                    isValueField: true
                );
            });

            Assert.IsInstanceOf<PendingStructValue>(
                pending.value,
                "Struct values should remain intact after drawing the pending entry."
            );

            PendingStructValue result = (PendingStructValue)pending.value;
            Assert.AreEqual(initial.label, result.label);
            Assert.AreEqual(initial.tint, result.tint);

            Assert.IsNotNull(
                pending.valueWrapper,
                "Struct editing should allocate a PendingValueWrapper instance."
            );
            Assert.IsNotNull(pending.valueWrapperSerialized);
            Assert.IsNotNull(pending.valueWrapperProperty);
            Assert.AreEqual(
                SerializedPropertyType.ManagedReference,
                pending.valueWrapperProperty.propertyType,
                "Struct wrappers should expose a managed reference so nested fields are drawn."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryStructColorDataSupportsEditing()
        {
            SerializableDictionaryPropertyDrawer.PendingEntry pending = new();
            ColorData initial = new()
            {
                color1 = Color.red,
                color2 = Color.green,
                color3 = Color.blue,
                color4 = Color.white,
                otherColors = new[] { Color.cyan },
            };
            Rect rect = new(0f, 0f, 320f, EditorGUIUtility.singleLineHeight * 6f);

            yield return TestIMGUIExecutor.Run(() =>
            {
                pending.value = initial;
                pending.value = SerializableDictionaryPropertyDrawer.DrawFieldForType(
                    rect,
                    "Value",
                    pending.value,
                    typeof(ColorData),
                    pending,
                    isValueField: true
                );
            });

            Assert.IsNotNull(
                pending.valueWrapperProperty,
                "Managed reference property should exist for struct value wrappers."
            );
            Assert.IsTrue(
                pending.valueWrapperProperty.editable,
                "Pending value wrapper property should remain editable."
            );

            SerializedProperty color1Property = pending.valueWrapperProperty.FindPropertyRelative(
                nameof(ColorData.color1)
            );
            Assert.IsNotNull(color1Property, "Color fields should be reachable for struct values.");

            color1Property.colorValue = Color.magenta;
            pending.valueWrapperSerialized.ApplyModifiedPropertiesWithoutUndo();
            pending.valueWrapperSerialized.Update();

            ColorData updated = (ColorData)pending.valueWrapper.GetValue();
            AssertColorsApproximately(Color.magenta, updated.color1);
        }

        [UnityTest]
        public IEnumerator PendingEntryPrimitiveValueDrawsInlineField()
        {
            SerializableDictionaryPropertyDrawer.PendingEntry pending = new();
            Rect rect = new(0f, 0f, 200f, EditorGUIUtility.singleLineHeight);

            yield return TestIMGUIExecutor.Run(() =>
            {
                pending.value = 5;
                pending.value = SerializableDictionaryPropertyDrawer.DrawFieldForType(
                    rect,
                    "Value",
                    pending.value,
                    typeof(int),
                    pending,
                    isValueField: true
                );
            });

            Assert.IsInstanceOf<int>(pending.value, "Primitive value should remain an int.");
            Assert.IsNull(pending.valueWrapper, "Primitive values should not allocate wrappers.");
            Assert.IsNull(
                pending.valueWrapperSerialized,
                "Primitive values should not allocate serialized wrappers."
            );
            Assert.IsNull(
                pending.valueWrapperProperty,
                "Primitive values should not allocate wrapper properties."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryComplexValueAllocatesFoldoutGutter()
        {
            ComplexValueDictionaryHost host = CreateScriptableObject<ComplexValueDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ComplexValueDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ComplexValueDictionaryHost),
                nameof(ComplexValueDictionaryHost.dictionary)
            );

            SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                drawer,
                dictionaryProperty,
                typeof(string),
                typeof(ComplexValue),
                isSortedDictionary: false
            );
            pending.isExpanded = true;

            Rect controlRect = new(0f, 0f, 360f, 520f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.LastPendingValueUsedFoldoutLabel,
                "Pending value should reserve a foldout gutter for complex value types."
            );
            Assert.That(
                SerializableDictionaryPropertyDrawer.LastPendingValueFoldoutOffset,
                Is.EqualTo(SerializableDictionaryPropertyDrawer.PendingExpandableValueFoldoutGutter)
                    .Within(0.0001f),
                "Pending value foldout gutter should match the configured width."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryRowComplexValueAllocatesFoldoutGutter()
        {
            ComplexValueDictionaryHost host = CreateScriptableObject<ComplexValueDictionaryHost>();
            host.dictionary.Add(
                "Accent",
                new ComplexValue { button = Color.cyan, text = Color.black }
            );

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ComplexValueDictionaryHost.dictionary)
            );
            ForcePopulateComplexDictionarySerializedData(host, dictionaryProperty);
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ComplexValueDictionaryHost),
                nameof(ComplexValueDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 520f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.LastRowValueUsedFoldoutLabel,
                "Row values should reserve a foldout gutter for complex value types."
            );
            Assert.That(
                SerializableDictionaryPropertyDrawer.LastRowValueFoldoutOffset,
                Is.EqualTo(SerializableDictionaryPropertyDrawer.RowExpandableValueFoldoutGutter)
                    .Within(0.0001f),
                "Row value foldout gutter should match the configured width."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryHeaderIgnoresEditorIndent()
        {
            const int IndentDepth = 3;

            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(TestDictionaryHost),
                nameof(TestDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 420f);
            GUIContent label = new("Dictionary");

            float baselineOffset = 0f;
            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
                baselineOffset =
                    SerializableDictionaryPropertyDrawer.LastPendingFoldoutToggleRect.xMin
                    - SerializableDictionaryPropertyDrawer.LastPendingHeaderRect.xMin;
            });

            float indentedOffset = 0f;
            int indentLevelAfter = -1;
            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                EditorGUI.indentLevel = IndentDepth;
                drawer.OnGUI(controlRect, dictionaryProperty, label);
                indentedOffset =
                    SerializableDictionaryPropertyDrawer.LastPendingFoldoutToggleRect.xMin
                    - SerializableDictionaryPropertyDrawer.LastPendingHeaderRect.xMin;
                indentLevelAfter = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
            });

            Assert.That(
                indentedOffset,
                Is.EqualTo(baselineOffset).Within(0.0001f),
                "Pending header offset should be stable regardless of external indent."
            );
            Assert.That(
                indentLevelAfter,
                Is.EqualTo(IndentDepth),
                "Drawer should restore the caller's indent level."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryToggleOffsetAdjustsForSettingsContext()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            SerializedObject hostSerialized = TrackDisposable(new SerializedObject(host));
            hostSerialized.Update();
            SerializedProperty hostDictionary = hostSerialized.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(TestDictionaryHost),
                nameof(TestDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 420f);
            GUIContent label = new("Dictionary");
            hostDictionary.isExpanded = true;
            hostSerialized.ApplyModifiedPropertiesWithoutUndo();

            float hostOffset = 0f;
            yield return TestIMGUIExecutor.Run(() =>
            {
                hostDictionary.serializedObject.UpdateIfRequiredOrScript();
                SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                drawer.OnGUI(controlRect, hostDictionary, label);
                hostOffset =
                    SerializableDictionaryPropertyDrawer.LastPendingFoldoutToggleRect.xMin
                    - SerializableDictionaryPropertyDrawer.LastPendingHeaderRect.xMin;
            });

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject settingsSerialized = TrackDisposable(new SerializedObject(settings));
            settingsSerialized.Update();
            SerializedProperty paletteProperty = settingsSerialized.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            AssignDictionaryFieldInfo(
                drawer,
                typeof(UnityHelpersSettings),
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            paletteProperty.isExpanded = true;
            settingsSerialized.ApplyModifiedPropertiesWithoutUndo();

            float settingsOffset = 0f;
            yield return TestIMGUIExecutor.Run(() =>
            {
                paletteProperty.serializedObject.UpdateIfRequiredOrScript();
                SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                drawer.OnGUI(controlRect, paletteProperty, new GUIContent("Palette"));
                settingsOffset =
                    SerializableDictionaryPropertyDrawer.LastPendingFoldoutToggleRect.xMin
                    - SerializableDictionaryPropertyDrawer.LastPendingHeaderRect.xMin;
            });

            Assert.That(
                hostOffset,
                Is.EqualTo(SerializableDictionaryPropertyDrawer.PendingFoldoutToggleOffset)
                    .Within(0.0001f),
                "Default context should use the standard pending toggle offset."
            );
            Assert.That(
                settingsOffset,
                Is.EqualTo(
                        SerializableDictionaryPropertyDrawer.PendingFoldoutToggleOffsetProjectSettings
                    )
                    .Within(0.0001f),
                "UnityHelpersSettings context should use the project-settings offset."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryFoldoutToggleRespectsOffset()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(TestDictionaryHost),
                nameof(TestDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 480f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingHeaderRect,
                "Expected pending header layout to be tracked."
            );

            float offset =
                SerializableDictionaryPropertyDrawer.LastPendingFoldoutToggleRect.xMin
                - SerializableDictionaryPropertyDrawer.LastPendingHeaderRect.xMin;

            Assert.That(
                offset,
                Is.EqualTo(SerializableDictionaryPropertyDrawer.PendingFoldoutToggleOffset)
                    .Within(0.0001f),
                "Pending header toggle should honor the configured offset."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryRowFieldsApplyPadding()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary.Add(1, "One");
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            ForcePopulateTestDictionarySerializedData(host, dictionaryProperty);
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(TestDictionaryHost),
                nameof(TestDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 520f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowRects,
                "Expected dictionary row layout to be tracked."
            );

            float keyPadding =
                SerializableDictionaryPropertyDrawer.LastRowKeyRect.xMin
                - SerializableDictionaryPropertyDrawer.LastRowOriginalRect.xMin;
            Assert.That(
                keyPadding,
                Is.EqualTo(SerializableDictionaryPropertyDrawer.DictionaryRowFieldPadding)
                    .Within(0.0001f),
                "Key column should include the configured row padding."
            );

            float valuePadding =
                SerializableDictionaryPropertyDrawer.LastRowValueRect.xMin
                - SerializableDictionaryPropertyDrawer.LastRowValueBaseX;
            Assert.That(
                valuePadding,
                Is.EqualTo(SerializableDictionaryPropertyDrawer.DictionaryRowFieldPadding)
                    .Within(0.0001f),
                "Value column should include the configured row padding."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryHeaderHonorsGroupPadding()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(TestDictionaryHost),
                nameof(TestDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 420f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingHeaderRect,
                "Baseline draw should capture pending header layout."
            );

            Rect baselineHeader = SerializableDictionaryPropertyDrawer.LastPendingHeaderRect;

            const float LeftPadding = 20f;
            const float RightPadding = 12f;
            float horizontalPadding = LeftPadding + RightPadding;

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                    drawer.OnGUI(controlRect, dictionaryProperty, label);
                }
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingHeaderRect,
                "Grouped draw should capture pending header layout."
            );

            Rect groupedHeader = SerializableDictionaryPropertyDrawer.LastPendingHeaderRect;

            Assert.That(
                groupedHeader.xMin,
                Is.EqualTo(baselineHeader.xMin + LeftPadding).Within(0.0001f),
                "Pending header should respect the configured group padding."
            );
            Assert.That(
                groupedHeader.width,
                Is.EqualTo(Mathf.Max(0f, baselineHeader.width - horizontalPadding)).Within(0.0001f),
                "Pending header width should shrink by the applied padding."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryRowsHonorGroupPadding()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary.Add(1, "One");
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            ForcePopulateTestDictionarySerializedData(host, dictionaryProperty);
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(TestDictionaryHost),
                nameof(TestDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 520f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowRects,
                "Baseline draw should capture dictionary row layout."
            );

            Rect baselineRowRect = SerializableDictionaryPropertyDrawer.LastRowOriginalRect;
            Rect baselineKeyRect = SerializableDictionaryPropertyDrawer.LastRowKeyRect;
            Rect baselineValueRect = SerializableDictionaryPropertyDrawer.LastRowValueRect;

            const float LeftPadding = 24f;
            const float RightPadding = 10f;
            float horizontalPadding = LeftPadding + RightPadding;

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                    drawer.OnGUI(controlRect, dictionaryProperty, label);
                }
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowRects,
                "Grouped draw should capture dictionary row layout."
            );

            Rect groupedRowRect = SerializableDictionaryPropertyDrawer.LastRowOriginalRect;
            Rect groupedKeyRect = SerializableDictionaryPropertyDrawer.LastRowKeyRect;
            Rect groupedValueRect = SerializableDictionaryPropertyDrawer.LastRowValueRect;

            Assert.That(
                groupedRowRect.xMin,
                Is.EqualTo(baselineRowRect.xMin + LeftPadding).Within(0.0001f),
                "Row origin should respect group padding."
            );
            Assert.That(
                groupedRowRect.width,
                Is.EqualTo(Mathf.Max(0f, baselineRowRect.width - horizontalPadding))
                    .Within(0.0001f),
                "Row width should shrink by the applied padding."
            );
            Assert.That(
                groupedKeyRect.xMin,
                Is.EqualTo(baselineKeyRect.xMin + LeftPadding).Within(0.0001f),
                "Key field should shift by the group padding."
            );
            Assert.That(
                groupedValueRect.xMin,
                Is.EqualTo(baselineValueRect.xMin + LeftPadding).Within(0.0001f),
                "Value field should shift by the group padding."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryRowComplexValueReservesGapAndWidth()
        {
            ComplexValueDictionaryHost host = CreateScriptableObject<ComplexValueDictionaryHost>();
            host.dictionary.Add(
                "Accent",
                new ComplexValue { button = Color.cyan, text = Color.black }
            );

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ComplexValueDictionaryHost.dictionary)
            );
            ForcePopulateComplexDictionarySerializedData(host, dictionaryProperty);
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ComplexValueDictionaryHost),
                nameof(ComplexValueDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 640f, 520f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowRects,
                "Expected dictionary row layout to be tracked."
            );

            float actualGap =
                SerializableDictionaryPropertyDrawer.LastRowValueRect.xMin
                - SerializableDictionaryPropertyDrawer.LastRowKeyRect.xMax;
            float expectedGap =
                SerializableDictionaryPropertyDrawer.DictionaryRowKeyValueGap
                + SerializableDictionaryPropertyDrawer.DictionaryRowFoldoutGapBoost;

            Assert.That(
                actualGap,
                Is.EqualTo(expectedGap).Within(0.0001f),
                "Complex value rows should reserve the configured key/value gap."
            );

            float minValueWidth =
                SerializableDictionaryPropertyDrawer.DictionaryRowComplexValueMinWidth;
            Assert.That(
                SerializableDictionaryPropertyDrawer.LastRowValueRect.width,
                Is.GreaterThanOrEqualTo(minValueWidth - 0.0001f),
                "Complex value rows should preserve the minimum value column width."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryRowFoldoutCollapseAdjustsHeight()
        {
            ComplexValueDictionaryHost host = CreateScriptableObject<ComplexValueDictionaryHost>();
            host.dictionary.Add(
                "Accent",
                new ComplexValue { button = Color.blue, text = Color.white }
            );

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ComplexValueDictionaryHost.dictionary)
            );
            ForcePopulateComplexDictionarySerializedData(host, dictionaryProperty);
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ComplexValueDictionaryHost),
                nameof(ComplexValueDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 520f);
            GUIContent label = new("Dictionary");
            string cacheKey = SerializableDictionaryPropertyDrawer.GetListKey(dictionaryProperty);

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowRects,
                "Baseline draw should capture dictionary row layout."
            );
            float expandedHeight = SerializableDictionaryPropertyDrawer.LastRowValueRect.height;

            drawer.SetRowFoldoutStateForTests(cacheKey, 0, false);
            drawer.MarkListCacheDirty(cacheKey);

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            float collapsedHeight = SerializableDictionaryPropertyDrawer.LastRowValueRect.height;
            Assert.Less(
                collapsedHeight,
                expandedHeight - 0.0001f,
                "Collapsing the value foldout should reduce the rendered height."
            );

            drawer.SetRowFoldoutStateForTests(cacheKey, 0, true);
            drawer.MarkListCacheDirty(cacheKey);

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            float reopenedHeight = SerializableDictionaryPropertyDrawer.LastRowValueRect.height;
            Assert.That(
                reopenedHeight,
                Is.EqualTo(expandedHeight).Within(0.0001f),
                "Re-expanding the value foldout should restore the original height."
            );
        }

        [Test]
        public void GetOrCreateListRebuildsAfterCommit()
        {
            ComplexValueDictionaryHost host = CreateScriptableObject<ComplexValueDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ComplexValueDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            ReorderableList initialList = drawer.GetOrCreateList(dictionaryProperty);
            Assert.IsNotNull(initialList, "Initial call should create a ReorderableList instance.");
            Assert.That(initialList.count, Is.EqualTo(0));

            SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(ComplexValue),
                "Inline",
                new ComplexValue { button = Color.cyan, text = Color.black },
                dictionaryProperty
            );
            Assert.IsTrue(result.added, "Expected CommitEntry to add a new entry.");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            dictionaryProperty = serializedObject.FindProperty(
                nameof(ComplexValueDictionaryHost.dictionary)
            );
            keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            ReorderableList refreshedList = drawer.GetOrCreateList(dictionaryProperty);
            Assert.IsNotNull(refreshedList);
            Assert.AreNotSame(
                initialList,
                refreshedList,
                "ReorderableList cache should rebuild after committing new entries."
            );
            Assert.That(refreshedList.count, Is.EqualTo(1));

            float resolvedHeight =
                refreshedList.elementHeightCallback?.Invoke(0) ?? refreshedList.elementHeight;
            Assert.That(
                resolvedHeight,
                Is.GreaterThan(0f),
                "Row height should resolve using up-to-date serialized properties."
            );

            SerializedProperty keyElement = keysProperty.GetArrayElementAtIndex(0);
            SerializedProperty valueElement = valuesProperty.GetArrayElementAtIndex(0);

            Assert.That(keyElement.stringValue, Is.EqualTo("Inline"));
            SerializedProperty buttonColorProperty = valueElement.FindPropertyRelative("button");
            Assert.IsNotNull(buttonColorProperty);
            Assert.That(buttonColorProperty.colorValue, Is.EqualTo(Color.cyan));
        }

        [UnityTest]
        public IEnumerator HonorsGroupPaddingWithinGroups()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary.Add(1, "One");
            host.dictionary.Add(2, "Two");

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            ForcePopulateTestDictionarySerializedData(host, dictionaryProperty);
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(TestDictionaryHost),
                nameof(TestDictionaryHost.dictionary)
            );
            Rect controlRect = new(0f, 0f, 360f, 600f);
            GUIContent label = new("Dictionary");
            const int IndentDepth = 2;

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                dictionaryProperty.isExpanded = true;
                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = IndentDepth;
                try
                {
                    drawer.OnGUI(controlRect, dictionaryProperty, label);
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndent;
                }
            });

            Assert.IsTrue(
                drawer.HasLastListRect,
                $"Baseline draw should render the reorderable list. {BuildDictionaryDrawerDiagnostics(dictionaryProperty, drawer)}"
            );

            int snapshotIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = IndentDepth;
            Rect expectedBaselineRect = EditorGUI.IndentedRect(controlRect);
            EditorGUI.indentLevel = snapshotIndent;
            Assert.That(
                drawer.LastResolvedPosition.xMin,
                Is.EqualTo(expectedBaselineRect.xMin).Within(0.0001f),
                "Indentation should be reflected in the resolved content rectangle."
            );

            const float LeftPadding = 18f;
            const float RightPadding = 12f;
            const float HorizontalPadding = LeftPadding + RightPadding;

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                dictionaryProperty.isExpanded = true;
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        HorizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    int previousIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = IndentDepth;
                    try
                    {
                        drawer.OnGUI(controlRect, dictionaryProperty, label);
                    }
                    finally
                    {
                        EditorGUI.indentLevel = previousIndent;
                    }
                }
            });

            Rect paddedRect = drawer.LastResolvedPosition;
            Assert.That(
                paddedRect.xMin,
                Is.EqualTo(expectedBaselineRect.xMin + LeftPadding).Within(0.0001f),
                "Group padding should shift content by the configured left margin."
            );
            Assert.That(
                paddedRect.width,
                Is.EqualTo(Mathf.Max(0f, expectedBaselineRect.width - HorizontalPadding))
                    .Within(0.0001f),
                "Group padding should reduce the usable width."
            );
            Assert.That(
                drawer.LastListRect.xMin,
                Is.EqualTo(paddedRect.xMin).Within(0.0001f),
                $"List rect should align with the padded content area. {BuildDictionaryDrawerDiagnostics(dictionaryProperty, drawer)}"
            );
        }

        private static string BuildDictionaryDrawerDiagnostics(
            SerializedProperty dictionaryProperty,
            SerializableDictionaryPropertyDrawer drawer
        )
        {
            if (dictionaryProperty == null)
            {
                return "[DictionaryProperty=null]";
            }

            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            string keysSummary =
                keysProperty == null
                    ? "keys:null"
                    : $"keys:array={keysProperty.isArray},size={keysProperty.arraySize}";
            string valuesSummary =
                valuesProperty == null
                    ? "values:null"
                    : $"values:array={valuesProperty.isArray},size={valuesProperty.arraySize}";
            Rect lastRect = drawer.HasLastListRect ? drawer.LastListRect : Rect.zero;
            return $"[expanded={dictionaryProperty.isExpanded},propertyPath={dictionaryProperty.propertyPath},{keysSummary},{valuesSummary},hasLastRect={drawer.HasLastListRect},lastRect={lastRect}]";
        }

        private static void AssertColorsApproximately(
            Color expected,
            Color actual,
            float tolerance = 0.001f
        )
        {
            Assert.That(Mathf.Abs(expected.r - actual.r), Is.LessThanOrEqualTo(tolerance));
            Assert.That(Mathf.Abs(expected.g - actual.g), Is.LessThanOrEqualTo(tolerance));
            Assert.That(Mathf.Abs(expected.b - actual.b), Is.LessThanOrEqualTo(tolerance));
            Assert.That(Mathf.Abs(expected.a - actual.a), Is.LessThanOrEqualTo(tolerance));
        }

        internal static void AssignDictionaryFieldInfo(
            SerializableDictionaryPropertyDrawer drawer,
            Type hostType,
            string fieldName
        )
        {
            if (drawer == null || hostType == null || string.IsNullOrEmpty(fieldName))
            {
                return;
            }

            FieldInfo hostField = hostType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (hostField == null)
            {
                return;
            }

            FieldInfo drawerField = typeof(PropertyDrawer).GetField(
                "m_FieldInfo",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            drawerField?.SetValue(drawer, hostField);
        }

        private static void ForcePopulateTestDictionarySerializedData(
            TestDictionaryHost host,
            SerializedProperty dictionaryProperty
        )
        {
            if (host == null || dictionaryProperty == null)
            {
                return;
            }

            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            if (keysProperty == null || valuesProperty == null)
            {
                return;
            }

            List<KeyValuePair<int, string>> entries = host.dictionary.ToList();
            keysProperty.arraySize = entries.Count;
            valuesProperty.arraySize = entries.Count;

            for (int i = 0; i < entries.Count; i++)
            {
                AssignKey(keysProperty.GetArrayElementAtIndex(i), entries[i].Key);
                AssignValue(valuesProperty.GetArrayElementAtIndex(i), entries[i].Value);
            }

            dictionaryProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();

            static void AssignKey(SerializedProperty property, int value)
            {
                if (property != null)
                {
                    property.intValue = value;
                }
            }

            static void AssignValue(SerializedProperty property, string value)
            {
                if (property != null)
                {
                    property.stringValue = value ?? string.Empty;
                }
            }
        }

        private static void ForcePopulateComplexDictionarySerializedData(
            ComplexValueDictionaryHost host,
            SerializedProperty dictionaryProperty
        )
        {
            if (host == null || dictionaryProperty == null)
            {
                return;
            }

            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            if (keysProperty == null || valuesProperty == null)
            {
                return;
            }

            List<KeyValuePair<string, ComplexValue>> entries = host.dictionary.ToList();
            keysProperty.arraySize = entries.Count;
            valuesProperty.arraySize = entries.Count;

            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(i);
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(i);
                SerializableDictionaryPropertyDrawer.SetPropertyValue(
                    keyProperty,
                    entries[i].Key,
                    typeof(string)
                );
                SerializableDictionaryPropertyDrawer.SetPropertyValue(
                    valueProperty,
                    entries[i].Value,
                    typeof(ComplexValue)
                );
            }

            dictionaryProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
        }

        private static string DumpIntArray(SerializedProperty property)
        {
            if (property == null || !property.isArray)
            {
                return "<null>";
            }

            List<int> values = new(property.arraySize);
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                values.Add(element?.intValue ?? 0);
            }

            return string.Join(", ", values);
        }

        private static SerializableDictionaryPropertyDrawer.PendingEntry GetPendingEntry(
            SerializableDictionaryPropertyDrawer drawer,
            SerializedProperty dictionaryProperty,
            Type keyType,
            Type valueType,
            bool isSortedDictionary
        )
        {
            SerializableDictionaryPropertyDrawer.PendingEntry pending =
                drawer.GetOrCreatePendingEntry(
                    dictionaryProperty,
                    keyType,
                    valueType,
                    isSortedDictionary
                );
            Assert.IsNotNull(pending, "Pending entry instance should not be null.");
            return pending;
        }
    }
}
