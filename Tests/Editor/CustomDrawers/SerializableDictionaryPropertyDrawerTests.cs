namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    public sealed class SerializableDictionaryPropertyDrawerTests : CommonTestBase
    {
        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            GroupGUIWidthUtility.ResetForTests();
        }

        private sealed class TestDictionaryHost : ScriptableObject
        {
            public IntStringDictionary dictionary = new();
        }

        private sealed class StringDictionaryHost : ScriptableObject
        {
            // ReSharper disable once NotAccessedField.Local
            public StringStringDictionary dictionary = new();
        }

        private sealed class ComplexValueDictionaryHost : ScriptableObject
        {
            public StringComplexDictionary dictionary = new();
        }

        private sealed class ColorDataDictionaryHost : ScriptableObject
        {
            public StringColorDataDictionary dictionary = new();
        }

        private sealed class ColorListDictionaryHost : ScriptableObject
        {
            public StringColorListDictionary dictionary = new();
        }

        private sealed class LabelStressDictionaryHost : ScriptableObject
        {
            public StringLabelStressDictionary dictionary = new();
        }

        private sealed class MixedFieldsDictionaryHost : ScriptableObject
        {
            public int scalarValue;
            public IntStringDictionary dictionary = new();
        }

        private sealed class DictionaryScalarAfterHost : ScriptableObject
        {
            public IntStringDictionary dictionary = new();
            public int trailingScalar;
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

        private sealed class GroupedPaletteHost : ScriptableObject
        {
            [WGroup(
                "Palette",
                displayName: "Palette Colors",
                autoIncludeCount: 1,
                collapsible: true
            )]
            public GroupPaletteDictionary palette = new();
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
        private sealed class StringColorDataDictionary
            : SerializableDictionary<string, ColorData> { }

        [Serializable]
        private sealed class StringColorListDictionary
            : SerializableDictionary<string, ColorListData> { }

        [Serializable]
        private sealed class StringLabelStressDictionary
            : SerializableDictionary<string, LabelStressValue> { }

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
            [FormerlySerializedAs("primary")]
            [SerializeField]
            private Color _primary = Color.white;

            [FormerlySerializedAs("secondary")]
            [SerializeField]
            private Color _secondary = Color.black;

            public Color Primary
            {
                get => _primary;
                set => _primary = value;
            }

            public Color Secondary
            {
                get => _secondary;
                set => _secondary = value;
            }
        }

        [Serializable]
        private sealed class RectIntDictionary : SerializableDictionary<Rect, int> { }

        [Serializable]
        private sealed class PrivateCtorDictionary
            : SerializableDictionary<PrivateCtorKey, PrivateCtorValue> { }

        [Serializable]
        private sealed class GroupPaletteDictionary
            : SerializableDictionary<string, UnityHelpersSettings.WGroupCustomColor> { }

        [Serializable]
        private sealed class PrivateCtorKey
        {
            [FormerlySerializedAs("token")]
            [SerializeField]
            private string _token;

            // ReSharper disable once UnusedMember.Local
            public string Token => _token;

            private PrivateCtorKey()
            {
                _token = Guid.NewGuid().ToString();
            }
        }

        [Serializable]
        private sealed class PrivateCtorValue
        {
            [FormerlySerializedAs("accent")]
            [SerializeField]
            private Color _accent = Color.magenta;

            [FormerlySerializedAs("intensity")]
            [SerializeField]
            private float _intensity = 1f;

            private PrivateCtorValue() { }

            // ReSharper disable once UnusedMember.Local
            public Color Accent => _accent;

            // ReSharper disable once UnusedMember.Local
            public float Intensity => _intensity;
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

            // ReSharper disable once NotAccessedField.Local
            public Color color2;

            // ReSharper disable once NotAccessedField.Local
            public Color color3;

            // ReSharper disable once NotAccessedField.Local
            public Color color4;
            public Color[] otherColors;
        }

        [Serializable]
        private struct ColorListData
        {
            public List<Color> colors;
        }

        [Serializable]
        private struct LabelStressValue
        {
            // ReSharper disable once NotAccessedField.Local
            public float shortName;

            [FormerlySerializedAs("RidiculouslyVerboseFieldNameRequiringSpace")]
            // ReSharper disable once NotAccessedField.Local
            public float ridiculouslyVerboseFieldNameRequiringSpace;
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

                string listKey = drawer.GetListKey(dictionaryProperty);
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

                string listKey = drawer.GetListKey(dictionaryProperty);

                // ReSharper disable once RedundantAssignment
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

            string listKey = drawer.GetListKey(dictionaryProperty);

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

                string listKey = drawer.GetListKey(dictionaryProperty);
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
                string listKey = drawer.GetListKey(dictionaryProperty);

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
                string listKey = drawer.GetListKey(dictionaryProperty);

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
        public void SortDictionaryEntriesOrdersPaletteKeysInGroupedInspector()
        {
            GroupedPaletteHost host = CreateScriptableObject<GroupedPaletteHost>();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(GroupedPaletteHost.palette)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            keysProperty.arraySize = 3;
            valuesProperty.arraySize = 3;
            SetPaletteRow(0, "SortZeta", new Color(0.85f, 0.2f, 0.2f, 1f), Color.white);
            SetPaletteRow(1, "SortAlpha", new Color(0.2f, 0.75f, 0.35f, 1f), Color.black);
            SetPaletteRow(2, "SortMid", new Color(0.25f, 0.35f, 0.9f, 1f), Color.white);
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
                typeof(string),
                typeof(UnityHelpersSettings.WGroupCustomColor),
                Comparison,
                pagination,
                list
            );

            serializedObject.Update();
            Assert.AreEqual("SortAlpha", keysProperty.GetArrayElementAtIndex(0).stringValue);
            Assert.AreEqual("SortMid", keysProperty.GetArrayElementAtIndex(1).stringValue);
            Assert.AreEqual("SortZeta", keysProperty.GetArrayElementAtIndex(2).stringValue);

            string[] runtimeOrder = host.palette.Select(pair => pair.Key).ToArray();
            string[] expectedOrder = { "SortAlpha", "SortMid", "SortZeta" };
            CollectionAssert.AreEqual(expectedOrder, runtimeOrder);

            void SetPaletteRow(int index, string key, Color background, Color text)
            {
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(index);
                keyProperty.stringValue = key;
                SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(index);
                valueProperty
                    .FindPropertyRelative(
                        UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorBackground
                    )
                    .colorValue = background;
                valueProperty
                    .FindPropertyRelative(
                        UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColorText
                    )
                    .colorValue = text;
            }

            int Comparison(object left, object right)
            {
                string leftKey = left as string ?? left?.ToString();
                string rightKey = right as string ?? right?.ToString();
                return string.CompareOrdinal(leftKey, rightKey);
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
        public void PageEntriesNeedSortingOnlyFlagsCurrentPage()
        {
            SerializableDictionaryPropertyDrawer drawer = new();
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary.Add(1, "one");
            host.dictionary.Add(2, "two");
            host.dictionary.Add(4, "four");
            host.dictionary.Add(3, "three");

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );

            SerializableDictionaryPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(dictionaryProperty);
            pagination.pageIndex = 0;
            pagination.pageSize = 2;

            string listKey = drawer.GetListKey(dictionaryProperty);
            SerializableDictionaryPropertyDrawer.ListPageCache cache = drawer.EnsurePageCache(
                listKey,
                keysProperty,
                pagination
            );

            Func<object, object, int> comparison = static (left, right) =>
            {
                int leftValue = left is int li ? li : Convert.ToInt32(left);
                int rightValue = right is int ri ? ri : Convert.ToInt32(right);
                return leftValue.CompareTo(rightValue);
            };

            bool firstPageNeedsSorting =
                SerializableDictionaryPropertyDrawer.PageEntriesNeedSorting(
                    cache,
                    keysProperty,
                    typeof(int),
                    comparison
                );
            Assert.IsFalse(
                firstPageNeedsSorting,
                $"First page (keys {DumpIntArray(keysProperty)} indexes {DumpPageEntries(cache)}) should already be sorted."
            );

            pagination.pageIndex = 1;
            drawer.MarkListCacheDirty(listKey);
            cache = drawer.EnsurePageCache(listKey, keysProperty, pagination);
            bool secondPageNeedsSorting =
                SerializableDictionaryPropertyDrawer.PageEntriesNeedSorting(
                    cache,
                    keysProperty,
                    typeof(int),
                    comparison
                );
            Assert.IsTrue(
                secondPageNeedsSorting,
                $"Second page should report unsorted entries. Keys {DumpIntArray(keysProperty)} indexes {DumpPageEntries(cache)}"
            );
        }

        [Test]
        public void CommitEntryPreservesColorDataArrayValues()
        {
            ColorDataDictionaryHost host = CreateScriptableObject<ColorDataDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ColorDataDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            ColorData entry = new()
            {
                color1 = Color.red,
                color2 = Color.green,
                color3 = Color.blue,
                color4 = Color.white,
                otherColors = new[] { Color.yellow, Color.cyan },
            };

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(ColorData),
                "Accent",
                entry,
                dictionaryProperty
            );

            Assert.IsTrue(result.added, "Expected CommitEntry to add a new entry.");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            host.dictionary.EditorAfterDeserialize();

            Assert.That(host.dictionary.Count, Is.EqualTo(1));
            Assert.IsTrue(host.dictionary.ContainsKey("Accent"));
            ColorData stored = host.dictionary["Accent"];
            Assert.That(stored.otherColors, Is.Not.Null);
            Assert.That(stored.otherColors.Length, Is.EqualTo(2));
            AssertColorsApproximately(Color.yellow, stored.otherColors[0]);
            AssertColorsApproximately(Color.cyan, stored.otherColors[1]);

            SerializedProperty storedValue = valuesProperty.GetArrayElementAtIndex(result.index);
            SerializedProperty otherColorsProperty = storedValue.FindPropertyRelative(
                nameof(ColorData.otherColors)
            );
            Assert.IsNotNull(
                otherColorsProperty,
                "Serialized ColorData should expose otherColors."
            );
            Assert.That(otherColorsProperty.arraySize, Is.EqualTo(2));
            AssertColorsApproximately(
                Color.yellow,
                otherColorsProperty.GetArrayElementAtIndex(0).colorValue
            );
            AssertColorsApproximately(
                Color.cyan,
                otherColorsProperty.GetArrayElementAtIndex(1).colorValue
            );
        }

        [Test]
        public void CommitEntryPreservesColorListValues()
        {
            ColorListDictionaryHost host = CreateScriptableObject<ColorListDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ColorListDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            ColorListData entry = new()
            {
                colors = new List<Color> { Color.red, Color.blue },
            };

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(ColorListData),
                "Palette",
                entry,
                dictionaryProperty
            );

            Assert.IsTrue(result.added, "Expected CommitEntry to add a new entry.");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            host.dictionary.EditorAfterDeserialize();

            Assert.That(host.dictionary.ContainsKey("Palette"), Is.True);
            ColorListData stored = host.dictionary["Palette"];
            Assert.IsNotNull(stored.colors);
            Assert.That(stored.colors.Count, Is.EqualTo(2));
            AssertColorsApproximately(Color.red, stored.colors[0]);
            AssertColorsApproximately(Color.blue, stored.colors[1]);
        }

        [Test]
        public void PendingEntryCommitPreservesStringKey()
        {
            ColorDataDictionaryHost host = CreateScriptableObject<ColorDataDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ColorDataDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ColorDataDictionaryHost),
                nameof(ColorDataDictionaryHost.dictionary)
            );

            SerializableDictionaryPropertyDrawer.PendingEntry pending =
                drawer.GetOrCreatePendingEntry(
                    dictionaryProperty,
                    typeof(string),
                    typeof(ColorData),
                    isSortedDictionary: false
                );
            pending.key = "test-input";
            pending.value = new ColorData { color1 = Color.white };

            SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(ColorData),
                pending.key,
                pending.value,
                dictionaryProperty,
                existingIndex: -1
            );

            Assert.IsTrue(result.added, "Expected CommitEntry to add a new entry.");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            host.dictionary.EditorAfterDeserialize();

            Assert.IsTrue(host.dictionary.ContainsKey("test-input"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PendingDuplicateCacheInvalidatesAfterDictionaryValueChange(
            bool initializeWrapper
        )
        {
            ColorDataDictionaryHost host = CreateScriptableObject<ColorDataDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ColorDataDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ColorDataDictionaryHost),
                nameof(ColorDataDictionaryHost.dictionary)
            );

            SerializableDictionaryPropertyDrawer.CommitResult initialEntry = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(ColorData),
                "Accent",
                new ColorData { color1 = Color.white },
                dictionaryProperty
            );
            Assert.IsTrue(initialEntry.added, "Expected initial commit to succeed.");
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializableDictionaryPropertyDrawer.PendingEntry pending =
                drawer.GetOrCreatePendingEntry(
                    dictionaryProperty,
                    typeof(string),
                    typeof(ColorData),
                    isSortedDictionary: false
                );
            pending.key = "Accent";
            int storedIndex = initialEntry.index >= 0 ? initialEntry.index : 0;
            pending.value = new ColorData { color1 = Color.white };
            TestContext.WriteLine(
                $"[DuplicateCache] Pending value preset: {DescribeColorData((ColorData)pending.value)}"
            );

            if (initializeWrapper)
            {
                MethodInfo ensureWrapperMethod =
                    typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                        "EnsurePendingWrapper",
                        BindingFlags.NonPublic | BindingFlags.Static
                    );
                Assert.IsNotNull(
                    ensureWrapperMethod,
                    "EnsurePendingWrapper reflection lookup failed."
                );
                ensureWrapperMethod.Invoke(null, new object[] { pending, typeof(ColorData), true });
                Assert.IsNotNull(
                    pending.valueWrapperProperty,
                    "Pending wrapper should initialize when requested."
                );
                pending.valueWrapperSerialized?.Update();
                if (pending.valueWrapperProperty != null && pending.valueWrapperSerialized != null)
                {
                    pending.valueWrapperProperty.managedReferenceValue = pending.value;
                    pending.valueWrapperSerialized.ApplyModifiedPropertiesWithoutUndo();
                    pending.valueWrapperSerialized.Update();
                }
            }

            MethodInfo matchesMethod = typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                "EntryMatchesExisting",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.IsNotNull(matchesMethod, "EntryMatchesExisting reflection lookup failed.");

            object[] args =
            {
                keysProperty,
                valuesProperty,
                storedIndex,
                typeof(string),
                typeof(ColorData),
                pending,
            };

            bool initialMatch = (bool)matchesMethod.Invoke(null, args);
            TestContext.WriteLine(
                $"[DuplicateCache] Initial match (wrapper initialized: {initializeWrapper}) = {initialMatch}"
            );
            Assert.IsTrue(
                initialMatch,
                "Pending entry should match the existing dictionary entry."
            );

            SerializedProperty storedValue = valuesProperty.GetArrayElementAtIndex(storedIndex);
            ColorData serializedBeforeChange = ReadColorData(storedValue);
            TestContext.WriteLine(
                $"[DuplicateCache] Serialized value before mutation: {DescribeColorData(serializedBeforeChange)}"
            );
            SerializedProperty colorProperty = storedValue.FindPropertyRelative(
                nameof(ColorData.color1)
            );
            colorProperty.colorValue = Color.magenta;
            valuesProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            valuesProperty.serializedObject.Update();
            ColorData serializedAfterChange = ReadColorData(storedValue);
            TestContext.WriteLine(
                $"[DuplicateCache] Serialized value after mutation: {DescribeColorData(serializedAfterChange)}"
            );

            bool cachedMatch = (bool)matchesMethod.Invoke(null, args);
            TestContext.WriteLine(
                $"[DuplicateCache] Cached match before invalidation = {cachedMatch}"
            );
            Assert.IsTrue(
                cachedMatch,
                "Cached duplicate check should remain true until the cache is invalidated."
            );

            string cacheKey = drawer.GetListKey(dictionaryProperty);
            drawer.InvalidatePendingDuplicateCache(cacheKey);

            bool refreshedMatch = (bool)matchesMethod.Invoke(null, args);
            TestContext.WriteLine(
                $"[DuplicateCache] Refreshed match after invalidation = {refreshedMatch}"
            );
            Assert.IsFalse(
                refreshedMatch,
                "Invalidating the cache should force a fresh duplicate comparison."
            );
        }

        [Test]
        public void ValuesEqualTreatsNullAndEmptyCollectionsAsEqualInStructs()
        {
            ColorData nullColors = new() { color1 = Color.red, otherColors = null };
            ColorData emptyColors = new()
            {
                color1 = Color.red,
                otherColors = Array.Empty<Color>(),
            };

            bool result = InvokeValuesEqual(nullColors, emptyColors);
            Assert.IsTrue(
                result,
                "ValuesEqual should treat null and empty collections as equivalent for struct fields."
            );
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
            Assert.IsNotNull(
                dictionaryProperty,
                $"Expected to find dictionary property at '{nameof(PrivateComplexDictionaryHost.dictionary)}'"
            );

            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            Assert.IsNotNull(keysProperty, "Expected to find keys property.");
            Assert.IsNotNull(valuesProperty, "Expected to find values property.");

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

            SerializedProperty primaryProperty = valueElement.FindPropertyRelative("_primary");
            SerializedProperty secondaryProperty = valueElement.FindPropertyRelative("_secondary");

            Assert.IsNotNull(
                primaryProperty,
                $"Expected '_primary' color property to exist on committed value. ValueElement type: {valueElement.type}"
            );
            Assert.IsNotNull(
                secondaryProperty,
                $"Expected '_secondary' color property to exist on committed value. ValueElement type: {valueElement.type}"
            );

            Assert.That(primaryProperty.colorValue, Is.EqualTo(Color.yellow));
            Assert.That(secondaryProperty.colorValue, Is.EqualTo(Color.green));

            host.dictionary.EditorAfterDeserialize();
            Assert.That(host.dictionary.Count, Is.EqualTo(1));
            Assert.That(host.dictionary.ContainsKey("Accent"), Is.True);
        }

        [Test]
        public void CommitEntryAddsPrivateComplexValueWithDefaultColors()
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
            PrivateComplexValue valueInstance = new();

            SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(PrivateComplexValue),
                "Default",
                valueInstance,
                dictionaryProperty
            );
            Assert.IsTrue(result.added, "Expected CommitEntry to add the default complex value.");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializedProperty valueElement = valuesProperty.GetArrayElementAtIndex(0);
            Assert.IsNotNull(valueElement, "Expected value element to exist after commit.");

            SerializedProperty primaryProperty = valueElement.FindPropertyRelative("_primary");
            SerializedProperty secondaryProperty = valueElement.FindPropertyRelative("_secondary");

            Assert.IsNotNull(primaryProperty, "Expected '_primary' property for default value.");
            Assert.IsNotNull(
                secondaryProperty,
                "Expected '_secondary' property for default value."
            );

            Assert.That(
                primaryProperty.colorValue,
                Is.EqualTo(Color.white),
                "Default primary color should be white"
            );
            Assert.That(
                secondaryProperty.colorValue,
                Is.EqualTo(Color.black),
                "Default secondary color should be black"
            );
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
        public IEnumerator PendingEntryListValueSupportsEditing()
        {
            SerializableDictionaryPropertyDrawer.PendingEntry pending = new();
            ColorListData initial = new() { colors = new List<Color> { Color.red } };
            Rect rect = new(0f, 0f, 320f, EditorGUIUtility.singleLineHeight * 5f);

            yield return TestIMGUIExecutor.Run(() =>
            {
                pending.value = initial;
                pending.value = SerializableDictionaryPropertyDrawer.DrawFieldForType(
                    rect,
                    "Value",
                    pending.value,
                    typeof(ColorListData),
                    pending,
                    isValueField: true
                );
            });

            Assert.IsNotNull(
                pending.valueWrapperProperty,
                "List-backed pending values should allocate wrapper properties."
            );

            SerializedProperty colorsProperty = pending.valueWrapperProperty.FindPropertyRelative(
                nameof(ColorListData.colors)
            );
            Assert.IsNotNull(colorsProperty, "ColorListData should expose the colors list.");
            Assert.IsTrue(colorsProperty.isArray, "List fields should be serialized as arrays.");

            colorsProperty.arraySize = 2;
            colorsProperty.GetArrayElementAtIndex(0).colorValue = Color.magenta;
            colorsProperty.GetArrayElementAtIndex(1).colorValue = Color.yellow;
            pending.valueWrapperSerialized.ApplyModifiedPropertiesWithoutUndo();
            pending.valueWrapperSerialized.Update();

            ColorListData updated = (ColorListData)pending.valueWrapper.GetValue();
            Assert.IsNotNull(updated.colors);
            Assert.That(updated.colors.Count, Is.EqualTo(2));
            AssertColorsApproximately(Color.magenta, updated.colors[0]);
            AssertColorsApproximately(Color.yellow, updated.colors[1]);
        }

        [UnityTest]
        public IEnumerator PendingEntryListValueCommitsChanges()
        {
            ColorListDictionaryHost host = CreateScriptableObject<ColorListDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ColorListDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ColorListDictionaryHost),
                nameof(ColorListDictionaryHost.dictionary)
            );

            SerializableDictionaryPropertyDrawer.PendingEntry pending =
                drawer.GetOrCreatePendingEntry(
                    dictionaryProperty,
                    typeof(string),
                    typeof(ColorListData),
                    isSortedDictionary: false
                );
            pending.key = "Palette";
            pending.value = new ColorListData
            {
                colors = new List<Color> { Color.red, Color.cyan },
            };

            SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(ColorListData),
                pending.key,
                pending.value,
                dictionaryProperty,
                existingIndex: -1,
                isSortedDictionary: false
            );
            Assert.IsTrue(result.added, "Expected the pending list value to be committed.");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            host.dictionary.EditorAfterDeserialize();

            Assert.That(host.dictionary.ContainsKey("Palette"));
            ColorListData stored = host.dictionary["Palette"];
            Assert.IsNotNull(stored.colors);
            Assert.That(stored.colors.Count, Is.EqualTo(2));
            AssertColorsApproximately(Color.red, stored.colors[0]);
            AssertColorsApproximately(Color.cyan, stored.colors[1]);
            yield break;
        }

        [Test]
        public void CommitEntryPreservesScriptableObjectReference()
        {
            ScriptableObjectDictionaryHost host =
                CreateScriptableObject<ScriptableObjectDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ScriptableObjectDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            Object scriptable = Track(ScriptableObject.CreateInstance<SampleScriptableObject>());
            scriptable.hideFlags = HideFlags.HideAndDontSave;

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ScriptableObjectDictionaryHost),
                nameof(ScriptableObjectDictionaryHost.dictionary)
            );

            SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(SampleScriptableObject),
                "Asset",
                scriptable,
                dictionaryProperty
            );

            Assert.IsTrue(
                result.added,
                "Expected CommitEntry to add the scriptable object reference."
            );

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            host.dictionary.EditorAfterDeserialize();

            Assert.IsTrue(host.dictionary.ContainsKey("Asset"));
            Assert.AreSame(
                scriptable,
                host.dictionary["Asset"],
                "Dictionary should retain the committed scriptable object reference."
            );

            SerializedProperty committedValue = valuesProperty.GetArrayElementAtIndex(result.index);
            Assert.AreSame(
                scriptable,
                committedValue.objectReferenceValue,
                "Serialized array should reference the committed scriptable object."
            );
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

        [Test]
        public void GetPropertyHeightAutoExpandsComplexRowsOnFirstDraw()
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

            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            Assert.Greater(valuesProperty.arraySize, 0, "Dictionary should contain test entries.");
            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(0);
            valueProperty.isExpanded = false;

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ComplexValueDictionaryHost),
                nameof(ComplexValueDictionaryHost.dictionary)
            );

            drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);

            serializedObject.Update();
            SerializedProperty refreshedValues = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            SerializedProperty refreshedValue = refreshedValues.GetArrayElementAtIndex(0);
            Assert.IsTrue(
                refreshedValue.isExpanded,
                "GetPropertyHeight should expand complex dictionary rows before the first draw so layout reserves enough space."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryRowComplexValueChildControlsHaveSpaceOnFirstDraw()
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
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            Assert.Greater(valuesProperty.arraySize, 0, "Dictionary should contain test entries.");
            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(0);
            valueProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ComplexValueDictionaryHost),
                nameof(ComplexValueDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 520f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            // Simulate Unity's layout pass.
            drawer.GetPropertyHeight(dictionaryProperty, label);

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowChildContentRect,
                "First draw should record child layout information for complex values."
            );
            float valueHeight = SerializableDictionaryPropertyDrawer.LastRowValueRect.height;
            Assert.Greater(
                valueHeight,
                EditorGUIUtility.singleLineHeight * 2f,
                $"Complex value rows should reserve space for expanded children. Observed height: {valueHeight:F3}"
            );
            Assert.Greater(
                SerializableDictionaryPropertyDrawer.LastRowChildContentRect.width,
                160f,
                "Complex value child drawers should reserve a reasonable amount of width on the first draw."
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
        public IEnumerator DictionaryDrawerDoesNotResetUnappliedScalarChanges()
        {
            MixedFieldsDictionaryHost host = Track(
                ScriptableObject.CreateInstance<MixedFieldsDictionaryHost>()
            );
            SerializedObject serializedHost = TrackDisposable(new SerializedObject(host));
            serializedHost.Update();

            SerializedProperty scalarProperty = serializedHost.FindProperty(
                nameof(MixedFieldsDictionaryHost.scalarValue)
            );
            SerializedProperty dictionaryProperty = serializedHost.FindProperty(
                nameof(MixedFieldsDictionaryHost.dictionary)
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(MixedFieldsDictionaryHost),
                nameof(MixedFieldsDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 320f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            drawer.GetPropertyHeight(dictionaryProperty, label);

            scalarProperty.intValue = 1337;

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            serializedHost.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(host.scalarValue, Is.EqualTo(1337));
        }

        [UnityTest]
        public IEnumerator DictionaryDrawerMultiObjectEditKeepsScalarChanges()
        {
            MixedFieldsDictionaryHost first = Track(
                ScriptableObject.CreateInstance<MixedFieldsDictionaryHost>()
            );
            MixedFieldsDictionaryHost second = Track(
                ScriptableObject.CreateInstance<MixedFieldsDictionaryHost>()
            );
            SerializedObject serializedHosts = TrackDisposable(
                new SerializedObject(new Object[] { first, second })
            );
            serializedHosts.Update();

            SerializedProperty scalarProperty = serializedHosts.FindProperty(
                nameof(MixedFieldsDictionaryHost.scalarValue)
            );
            SerializedProperty dictionaryProperty = serializedHosts.FindProperty(
                nameof(MixedFieldsDictionaryHost.dictionary)
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(MixedFieldsDictionaryHost),
                nameof(MixedFieldsDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 320f);
            GUIContent label = new("Dictionary");

            drawer.GetPropertyHeight(dictionaryProperty, label);

            scalarProperty.intValue = 2112;

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            serializedHosts.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(
                first.scalarValue,
                Is.EqualTo(2112),
                "First target should keep scalar edit."
            );
            Assert.That(
                second.scalarValue,
                Is.EqualTo(2112),
                "Second target should keep scalar edit."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryDrawerDoesNotResetTrailingScalarField()
        {
            DictionaryScalarAfterHost host = Track(
                ScriptableObject.CreateInstance<DictionaryScalarAfterHost>()
            );
            SerializedObject serializedHost = TrackDisposable(new SerializedObject(host));
            serializedHost.Update();

            SerializedProperty dictionaryProperty = serializedHost.FindProperty(
                nameof(DictionaryScalarAfterHost.dictionary)
            );
            SerializedProperty trailingScalarProperty = serializedHost.FindProperty(
                nameof(DictionaryScalarAfterHost.trailingScalar)
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(DictionaryScalarAfterHost),
                nameof(DictionaryScalarAfterHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 320f);
            GUIContent label = new("Dictionary");

            drawer.GetPropertyHeight(dictionaryProperty, label);

            trailingScalarProperty.intValue = 9001;

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            serializedHost.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(host.trailingScalar, Is.EqualTo(9001));
        }

        [Test]
        public void PaletteCommitTriggersUnityHelpersSettingsSave()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            settings.EnsureWButtonCustomColorDefaults();

            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();
            SerializedProperty dictionaryProperty = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            Assert.IsNotNull(dictionaryProperty, "Expected to find wbuttonCustomColors property.");

            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            Assert.IsNotNull(keysProperty);
            Assert.IsNotNull(valuesProperty);

            const string TestKey = "__TestPaletteProjectSettings";
            RemoveStringDictionaryEntry(keysProperty, valuesProperty, TestKey);
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            serializedSettings.Update();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(UnityHelpersSettings),
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            UnityHelpersSettings.WButtonCustomColor value = new()
            {
                ButtonColor = Color.magenta,
                TextColor = Color.yellow,
            };

            bool saveInvoked = false;
            void OnSettingsSaved()
            {
                saveInvoked = true;
            }

            UnityHelpersSettings.SettingsSaved += OnSettingsSaved;
            try
            {
                SerializableDictionaryPropertyDrawer.CommitResult result = drawer.CommitEntry(
                    keysProperty,
                    valuesProperty,
                    typeof(string),
                    typeof(UnityHelpersSettings.WButtonCustomColor),
                    TestKey,
                    value,
                    dictionaryProperty,
                    existingIndex: -1
                );

                Assert.IsTrue(result.added, "Expected palette entry to be added.");
                Assert.IsTrue(
                    saveInvoked,
                    "UnityHelpersSettings.SaveSettings should be invoked after palette edits."
                );
            }
            finally
            {
                UnityHelpersSettings.SettingsSaved -= OnSettingsSaved;
                RemoveStringDictionaryEntry(keysProperty, valuesProperty, TestKey);
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                serializedSettings.Update();
                settings.SaveSettings();
            }
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
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            Assert.Greater(valuesProperty.arraySize, 0, "Dictionary should contain test entries.");
            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(0);
            valueProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

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

            GroupGUIWidthUtility.ResetForTests();

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
        public IEnumerator PendingEntryHeaderHonorsGroupPaddingInSettingsContext()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();
            SerializedProperty paletteProperty = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            paletteProperty.isExpanded = true;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(UnityHelpersSettings),
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Rect controlRect = new(0f, 0f, 360f, 420f);
            GUIContent label = new("Palette");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                paletteProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, paletteProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingHeaderRect,
                "Baseline draw should capture pending header layout for settings dictionaries."
            );

            Rect baselineHeader = SerializableDictionaryPropertyDrawer.LastPendingHeaderRect;

            const float LeftPadding = 18f;
            const float RightPadding = 12f;
            float horizontalPadding = LeftPadding + RightPadding;

            GroupGUIWidthUtility.ResetForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                paletteProperty.serializedObject.UpdateIfRequiredOrScript();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                    drawer.OnGUI(controlRect, paletteProperty, label);
                }
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingHeaderRect,
                "Grouped draw should capture pending header layout for settings dictionaries."
            );

            Rect groupedHeader = SerializableDictionaryPropertyDrawer.LastPendingHeaderRect;

            Assert.That(
                groupedHeader.xMin,
                Is.EqualTo(baselineHeader.xMin + LeftPadding).Within(0.0001f),
                "Pending header in Project Settings should respect group padding."
            );
            Assert.That(
                groupedHeader.width,
                Is.EqualTo(Mathf.Max(0f, baselineHeader.width - horizontalPadding)).Within(0.0001f),
                "Pending header width in Project Settings should shrink by the applied padding."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryFieldsHonorGroupPadding()
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

            SerializableDictionaryPropertyDrawer.PendingEntry pending =
                drawer.GetOrCreatePendingEntry(
                    dictionaryProperty,
                    typeof(int),
                    typeof(string),
                    isSortedDictionary: false
                );
            pending.isExpanded = true;

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                pending.isExpanded = true;
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingFieldRects,
                "Baseline draw should capture pending key/value rects."
            );

            Rect baselineKey = SerializableDictionaryPropertyDrawer.LastPendingKeyFieldRect;
            Rect baselineValue = SerializableDictionaryPropertyDrawer.LastPendingValueFieldRect;

            const float LeftPadding = 18f;
            const float RightPadding = 14f;
            float horizontalPadding = LeftPadding + RightPadding;

            GroupGUIWidthUtility.ResetForTests();

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
                    pending.isExpanded = true;
                    drawer.OnGUI(controlRect, dictionaryProperty, label);
                }
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingFieldRects,
                "Grouped draw should capture pending key/value rects."
            );

            Rect groupedKey = SerializableDictionaryPropertyDrawer.LastPendingKeyFieldRect;
            Rect groupedValue = SerializableDictionaryPropertyDrawer.LastPendingValueFieldRect;

            Assert.That(
                groupedKey.xMin,
                Is.EqualTo(baselineKey.xMin + LeftPadding).Within(0.0001f),
                "Pending key field should shift by the applied left padding."
            );
            Assert.That(
                groupedValue.xMin,
                Is.EqualTo(baselineValue.xMin + LeftPadding).Within(0.0001f),
                "Pending value field should shift by the applied left padding."
            );
            Assert.That(
                groupedKey.width,
                Is.EqualTo(Mathf.Max(0f, baselineKey.width - horizontalPadding)).Within(0.0001f),
                "Pending key width should shrink by the total group padding."
            );
            Assert.That(
                groupedValue.width,
                Is.EqualTo(Mathf.Max(0f, baselineValue.width - horizontalPadding)).Within(0.0001f),
                "Pending value width should shrink by the total group padding."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryFieldsHonorGroupPaddingInSettingsContext()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();
            SerializedProperty paletteProperty = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            paletteProperty.isExpanded = true;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(UnityHelpersSettings),
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Rect controlRect = new(0f, 0f, 360f, 420f);
            GUIContent label = new("Palette");

            SerializableDictionaryPropertyDrawer.PendingEntry pending =
                drawer.GetOrCreatePendingEntry(
                    paletteProperty,
                    typeof(string),
                    typeof(UnityHelpersSettings.WButtonCustomColor),
                    isSortedDictionary: false
                );
            pending.isExpanded = true;

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                paletteProperty.serializedObject.UpdateIfRequiredOrScript();
                pending.isExpanded = true;
                drawer.OnGUI(controlRect, paletteProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingFieldRects,
                "Baseline draw should capture pending key/value rects for settings dictionaries."
            );

            Rect baselineKey = SerializableDictionaryPropertyDrawer.LastPendingKeyFieldRect;
            Rect baselineValue = SerializableDictionaryPropertyDrawer.LastPendingValueFieldRect;

            const float LeftPadding = 20f;
            const float RightPadding = 10f;
            float horizontalPadding = LeftPadding + RightPadding;

            GroupGUIWidthUtility.ResetForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                paletteProperty.serializedObject.UpdateIfRequiredOrScript();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                    pending.isExpanded = true;
                    drawer.OnGUI(controlRect, paletteProperty, label);
                }
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingFieldRects,
                "Grouped draw should capture pending key/value rects for settings dictionaries."
            );

            Rect groupedKey = SerializableDictionaryPropertyDrawer.LastPendingKeyFieldRect;
            Rect groupedValue = SerializableDictionaryPropertyDrawer.LastPendingValueFieldRect;

            Assert.That(
                groupedKey.xMin,
                Is.EqualTo(baselineKey.xMin + LeftPadding).Within(0.0001f),
                "Pending key field in Project Settings should shift by the applied left padding."
            );
            Assert.That(
                groupedValue.xMin,
                Is.EqualTo(baselineValue.xMin + LeftPadding).Within(0.0001f),
                "Pending value field in Project Settings should shift by the applied left padding."
            );
            Assert.That(
                groupedKey.width,
                Is.EqualTo(Mathf.Max(0f, baselineKey.width - horizontalPadding)).Within(0.0001f),
                "Pending key width in Project Settings should shrink by the total group padding."
            );
            Assert.That(
                groupedValue.width,
                Is.EqualTo(Mathf.Max(0f, baselineValue.width - horizontalPadding)).Within(0.0001f),
                "Pending value width in Project Settings should shrink by the total group padding."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryFoldoutValueRespectsGroupPadding()
        {
            ColorDataDictionaryHost host = CreateScriptableObject<ColorDataDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ColorDataDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ColorDataDictionaryHost),
                nameof(ColorDataDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 420f, 480f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.PendingEntry pending =
                drawer.GetOrCreatePendingEntry(
                    dictionaryProperty,
                    typeof(string),
                    typeof(ColorData),
                    isSortedDictionary: false
                );
            pending.isExpanded = true;

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                pending.isExpanded = true;
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingFieldRects,
                "Baseline draw should capture pending field rects."
            );
            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.LastPendingValueUsedFoldoutLabel,
                "Baseline draw should report a foldout label for complex pending values."
            );
            Assert.That(
                SerializableDictionaryPropertyDrawer.LastPendingValueFoldoutOffset,
                Is.EqualTo(SerializableDictionaryPropertyDrawer.PendingExpandableValueFoldoutGutter)
                    .Within(0.0001f),
                "Baseline foldout gutter should match the configured pending-value gutter."
            );

            Rect baselineValue = SerializableDictionaryPropertyDrawer.LastPendingValueFieldRect;

            const float LeftPadding = 16f;
            const float RightPadding = 12f;
            float horizontalPadding = LeftPadding + RightPadding;

            GroupGUIWidthUtility.ResetForTests();

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
                    pending.isExpanded = true;
                    drawer.OnGUI(controlRect, dictionaryProperty, label);
                }
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastPendingFieldRects,
                "Grouped draw should capture pending field rects."
            );
            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.LastPendingValueUsedFoldoutLabel,
                "Grouped draw should still report a foldout label for complex pending values."
            );
            Assert.That(
                SerializableDictionaryPropertyDrawer.LastPendingValueFoldoutOffset,
                Is.EqualTo(SerializableDictionaryPropertyDrawer.PendingExpandableValueFoldoutGutter)
                    .Within(0.0001f),
                "Grouped foldout gutter should match the configured pending-value gutter."
            );

            Rect groupedValue = SerializableDictionaryPropertyDrawer.LastPendingValueFieldRect;

            Assert.That(
                groupedValue.xMin,
                Is.EqualTo(baselineValue.xMin + LeftPadding).Within(0.0001f),
                "Pending value foldout should shift by the applied left padding."
            );
            Assert.That(
                groupedValue.width,
                Is.EqualTo(Mathf.Max(0f, baselineValue.width - horizontalPadding)).Within(0.0001f),
                "Pending value foldout width should shrink by the total group padding."
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

            GroupGUIWidthUtility.ResetForTests();

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
            float baselineValueOffset = baselineValueRect.xMin - baselineRowRect.xMin;
            float groupedValueOffset = groupedValueRect.xMin - groupedRowRect.xMin;
            float offsetDelta = Mathf.Abs(groupedValueOffset - baselineValueOffset);
            TestContext.WriteLine(
                $"[GroupPadding] baseline offset: {baselineValueOffset:F3}, grouped offset: {groupedValueOffset:F3}, delta: {offsetDelta:F3}"
            );
            Assert.LessOrEqual(
                offsetDelta,
                SerializableDictionaryPropertyDrawer.DictionaryRowFieldPadding + 0.5f,
                $"Value field offset within the row should remain consistent when padding is applied (delta: {offsetDelta:F3})."
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
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            Assert.Greater(valuesProperty.arraySize, 0, "Dictionary should contain test entries.");
            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(0);
            valueProperty.isExpanded = true;
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
                + SerializableDictionaryPropertyDrawer.DictionaryRowFoldoutGapBoost
                + SerializableDictionaryPropertyDrawer.DictionaryRowFieldPadding;

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
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );
            Assert.Greater(valuesProperty.arraySize, 0, "Dictionary should contain test entries.");
            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(0);
            valueProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(ComplexValueDictionaryHost),
                nameof(ComplexValueDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 360f, 520f);
            GUIContent label = new("Dictionary");
            string cacheKey = drawer.GetListKey(dictionaryProperty);

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

            valueProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            drawer.MarkListCacheDirty(cacheKey);
            drawer.SetRowFoldoutStateForTests(cacheKey, 0, false);

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            float collapsedHeight = SerializableDictionaryPropertyDrawer.LastRowValueRect.height;
            TestContext.WriteLine(
                $"[RowFoldout] expanded height: {expandedHeight:F3}, collapsed height: {collapsedHeight:F3}"
            );
            Assert.Less(
                collapsedHeight,
                expandedHeight - 0.5f,
                "Collapsing the value foldout should reduce the rendered height."
            );

            valueProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
            drawer.MarkListCacheDirty(cacheKey);
            drawer.SetRowFoldoutStateForTests(cacheKey, 0, true);

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            float reopenedHeight = SerializableDictionaryPropertyDrawer.LastRowValueRect.height;
            TestContext.WriteLine($"[RowFoldout] reopened height: {reopenedHeight:F3}");
            Assert.That(
                reopenedHeight,
                Is.EqualTo(expandedHeight).Within(0.5f),
                "Re-expanding the value foldout should restore the original height."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryRowChildControlsStayInsideValueColumn()
        {
            ColorDataDictionaryHost host = CreateScriptableObject<ColorDataDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ColorDataDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.CommitResult commit = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(ColorData),
                "Accent",
                new ColorData
                {
                    color1 = Color.red,
                    color2 = Color.green,
                    color3 = Color.blue,
                    color4 = Color.white,
                    otherColors = new[] { Color.yellow, Color.cyan, Color.magenta },
                },
                dictionaryProperty
            );

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(commit.index);
            valueProperty.isExpanded = true;
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            AssignDictionaryFieldInfo(
                drawer,
                typeof(ColorDataDictionaryHost),
                nameof(ColorDataDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 420f, 520f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowRects,
                "Row layout tracking should record the most recent row."
            );
            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowChildContentRect,
                "Expected at least one child rect to be recorded for complex values."
            );

            Rect valueRect = SerializableDictionaryPropertyDrawer.LastRowValueRect;
            Rect childRect = SerializableDictionaryPropertyDrawer.LastRowChildContentRect;

            Assert.That(
                childRect.xMin,
                Is.GreaterThanOrEqualTo(valueRect.xMin - 0.0001f),
                "Child controls should not render outside the value column on the left."
            );
            Assert.That(
                childRect.xMax,
                Is.LessThanOrEqualTo(valueRect.xMax + 0.0001f),
                "Child controls should stay within the value column bounds."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryRowChildControlsStayInsideValueColumnWithGroupPadding()
        {
            ColorDataDictionaryHost host = CreateScriptableObject<ColorDataDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(ColorDataDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.CommitResult commit = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(ColorData),
                "Accent",
                new ColorData
                {
                    color1 = Color.magenta,
                    color2 = Color.cyan,
                    color3 = Color.yellow,
                    color4 = Color.white,
                    otherColors = new[] { Color.red, Color.green },
                },
                dictionaryProperty
            );

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(commit.index);
            valueProperty.isExpanded = true;
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            AssignDictionaryFieldInfo(
                drawer,
                typeof(ColorDataDictionaryHost),
                nameof(ColorDataDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 420f, 520f);
            GUIContent label = new("Dictionary");

            const float LeftPadding = 22f;
            const float RightPadding = 14f;
            float horizontalPadding = LeftPadding + RightPadding;

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

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
                    drawer.OnGUI(controlRect, dictionaryProperty, label);
                }
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowRects,
                "Grouped draw should capture the most recent row layout."
            );
            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasLastRowChildContentRect,
                "Grouped draw should capture at least one child rect for complex values."
            );

            Rect valueRect = SerializableDictionaryPropertyDrawer.LastRowValueRect;
            Rect childRect = SerializableDictionaryPropertyDrawer.LastRowChildContentRect;

            Assert.That(
                childRect.xMin,
                Is.GreaterThanOrEqualTo(valueRect.xMin - 0.0001f),
                "Child controls should remain within the left edge of the grouped value column."
            );
            Assert.That(
                childRect.xMax,
                Is.LessThanOrEqualTo(valueRect.xMax + 0.0001f),
                "Child controls should remain within the right edge of the grouped value column."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryRowChildLabelWidthsAdaptToContent()
        {
            LabelStressDictionaryHost host = CreateScriptableObject<LabelStressDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(LabelStressDictionaryHost.dictionary)
            );
            SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Keys
            );
            SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                SerializableDictionarySerializedPropertyNames.Values
            );

            SerializableDictionaryPropertyDrawer drawer = new();
            SerializableDictionaryPropertyDrawer.CommitResult commit = drawer.CommitEntry(
                keysProperty,
                valuesProperty,
                typeof(string),
                typeof(LabelStressValue),
                "Entry",
                new LabelStressValue
                {
                    shortName = 1f,
                    ridiculouslyVerboseFieldNameRequiringSpace = 2f,
                },
                dictionaryProperty
            );

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializedProperty valueProperty = valuesProperty.GetArrayElementAtIndex(commit.index);
            valueProperty.isExpanded = true;
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            AssignDictionaryFieldInfo(
                drawer,
                typeof(LabelStressDictionaryHost),
                nameof(LabelStressDictionaryHost.dictionary)
            );

            Rect controlRect = new(0f, 0f, 420f, 480f);
            GUIContent label = new("Dictionary");

            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

            yield return TestIMGUIExecutor.Run(() =>
            {
                dictionaryProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, dictionaryProperty, label);
            });

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.HasRowChildLabelWidthData,
                "Expected row child label width tracking to capture measurements."
            );

            float minWidth = SerializableDictionaryPropertyDrawer.LastRowChildMinLabelWidth;
            float maxWidth = SerializableDictionaryPropertyDrawer.LastRowChildMaxLabelWidth;

            float widthGain = maxWidth - minWidth;
            TestContext.WriteLine(
                $"[ChildLabelWidth] min: {minWidth:F2}, max: {maxWidth:F2}, gain: {widthGain:F2}"
            );
            Assert.That(
                widthGain,
                Is.GreaterThan(2f),
                $"Long field names should reserve noticeably more width than short field names (observed gain: {widthGain:F2})."
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

            GroupGUIWidthUtility.ResetForTests();

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

        private static string DumpPageEntries(
            SerializableDictionaryPropertyDrawer.ListPageCache cache
        )
        {
            if (cache?.entries == null || cache.entries.Count == 0)
            {
                return "[]";
            }

            List<int> indices = new(cache.entries.Count);
            foreach (SerializableDictionaryPropertyDrawer.PageEntry cacheEntry in cache.entries)
            {
                indices.Add(cacheEntry?.arrayIndex ?? -1);
            }

            return $"[{string.Join(", ", indices)}]";
        }

        private static void RemoveStringDictionaryEntry(
            SerializedProperty keysProperty,
            SerializedProperty valuesProperty,
            string key
        )
        {
            if (
                keysProperty == null
                || valuesProperty == null
                || string.IsNullOrEmpty(key)
                || !keysProperty.isArray
                || !valuesProperty.isArray
            )
            {
                return;
            }

            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                SerializedProperty keyProperty = keysProperty.GetArrayElementAtIndex(i);
                if (!string.Equals(keyProperty.stringValue, key, StringComparison.Ordinal))
                {
                    continue;
                }

                keysProperty.DeleteArrayElementAtIndex(i);
                if (i < valuesProperty.arraySize)
                {
                    valuesProperty.DeleteArrayElementAtIndex(i);
                }

                break;
            }
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

        private static MethodInfo ValuesEqualMethod =>
            _valuesEqualMethod ??= typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                "ValuesEqual",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(object), typeof(object) },
                modifiers: null
            );

        private static MethodInfo GetPropertyValueMethod =>
            _getPropertyValueMethod ??= typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                "GetPropertyValue",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(SerializedProperty), typeof(Type) },
                modifiers: null
            );

        private static MethodInfo _valuesEqualMethod;
        private static MethodInfo _getPropertyValueMethod;

        private static bool InvokeValuesEqual(object left, object right)
        {
            MethodInfo method = ValuesEqualMethod;
            Assert.IsNotNull(method, "ValuesEqual reflection lookup failed.");
            return (bool)method.Invoke(null, new[] { left, right });
        }

        private static ColorData ReadColorData(SerializedProperty property)
        {
            if (property == null)
            {
                return default;
            }

            MethodInfo method = GetPropertyValueMethod;
            Assert.IsNotNull(method, "GetPropertyValue reflection lookup failed.");
            object value = method.Invoke(null, new object[] { property, typeof(ColorData) });
            return value is ColorData data ? data : default;
        }

        private static string DescribeColorData(ColorData data)
        {
            string formattedColor1 = FormatColor(data.color1);
            string otherSummary =
                data.otherColors == null
                    ? "null"
                    : data.otherColors.Length.ToString(CultureInfo.InvariantCulture);
            return $"color1={formattedColor1}, otherColors={otherSummary}";
        }

        private static string FormatColor(Color color)
        {
            return $"({color.r:0.00},{color.g:0.00},{color.b:0.00},{color.a:0.00})";
        }

        [Test]
        public void GetPropertyHeightIncreasesWhenPendingEntryIsExpanded()
        {
            using (new DictionaryTweenDisabledScope())
            {
                bool tweenEnabled = SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                );
                Assert.IsFalse(
                    tweenEnabled,
                    "Tween should be disabled by DictionaryTweenDisabledScope for accurate height tests."
                );

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

                SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                    drawer,
                    dictionaryProperty,
                    typeof(int),
                    typeof(string),
                    isSortedDictionary: false
                );

                pending.isExpanded = false;
                float collapsedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                pending.isExpanded = true;
                float expandedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                string diagnostics =
                    $"collapsedHeight={collapsedHeight}, expandedHeight={expandedHeight}, "
                    + $"pendingIsExpanded={pending.isExpanded}, foldoutAnimExists={pending.foldoutAnim != null}, "
                    + $"tweenEnabled={tweenEnabled}";
                Assert.Greater(
                    expandedHeight,
                    collapsedHeight,
                    $"Property height should increase when the pending New Entry section is expanded. Diagnostics: {diagnostics}"
                );
            }
        }

        [Test]
        public void GetPropertyHeightDecreasesWhenPendingEntryIsCollapsed()
        {
            using (new DictionaryTweenDisabledScope())
            {
                bool tweenEnabled = SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                );
                Assert.IsFalse(
                    tweenEnabled,
                    "Tween should be disabled by DictionaryTweenDisabledScope for accurate height tests."
                );

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

                SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                    drawer,
                    dictionaryProperty,
                    typeof(int),
                    typeof(string),
                    isSortedDictionary: false
                );

                pending.isExpanded = true;
                float expandedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                pending.isExpanded = false;
                float collapsedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                string diagnostics =
                    $"expandedHeight={expandedHeight}, collapsedHeight={collapsedHeight}, "
                    + $"pendingIsExpanded={pending.isExpanded}, foldoutAnimExists={pending.foldoutAnim != null}, "
                    + $"tweenEnabled={tweenEnabled}";
                Assert.Less(
                    collapsedHeight,
                    expandedHeight,
                    $"Property height should decrease when the pending New Entry section is collapsed. Diagnostics: {diagnostics}"
                );
            }
        }

        [Test]
        public void GetPropertyHeightCacheInvalidatesWhenPendingExpandStateChanges()
        {
            using (new DictionaryTweenDisabledScope())
            {
                bool tweenEnabled = SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                );
                Assert.IsFalse(
                    tweenEnabled,
                    "Tween should be disabled by DictionaryTweenDisabledScope for accurate height tests."
                );

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

                SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                    drawer,
                    dictionaryProperty,
                    typeof(int),
                    typeof(string),
                    isSortedDictionary: false
                );

                pending.isExpanded = false;
                float heightBeforeToggle = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );
                float heightCachedSame = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );
                Assert.AreEqual(
                    heightBeforeToggle,
                    heightCachedSame,
                    0.001f,
                    "Height should be cached and return the same value when nothing changes."
                );

                pending.isExpanded = true;
                float heightAfterExpand = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );
                string diagnostics =
                    $"heightBeforeToggle={heightBeforeToggle}, heightCachedSame={heightCachedSame}, "
                    + $"heightAfterExpand={heightAfterExpand}, pendingIsExpanded={pending.isExpanded}, "
                    + $"foldoutAnimExists={pending.foldoutAnim != null}, tweenEnabled={tweenEnabled}";
                Assert.AreNotEqual(
                    heightBeforeToggle,
                    heightAfterExpand,
                    $"Height cache should invalidate when pending isExpanded state changes. Diagnostics: {diagnostics}"
                );
            }
        }

        [Test]
        public void GetPropertyHeightReturnsConsistentHeightsForEmptyDictionaryWithPendingExpanded()
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

            SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                drawer,
                dictionaryProperty,
                typeof(int),
                typeof(string),
                isSortedDictionary: false
            );
            pending.isExpanded = true;

            float height1 = drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);
            float height2 = drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);
            float height3 = drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);

            Assert.AreEqual(
                height1,
                height2,
                0.001f,
                "Height should remain consistent across multiple calls with same state."
            );
            Assert.AreEqual(
                height2,
                height3,
                0.001f,
                "Height should remain consistent across multiple calls with same state."
            );
        }

        [Test]
        public void GetPropertyHeightDiffersBetweenExpandedAndCollapsedPendingForEmptyDictionary()
        {
            using (new DictionaryTweenDisabledScope())
            {
                bool tweenEnabled = SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                );
                Assert.IsFalse(
                    tweenEnabled,
                    "Tween should be disabled by DictionaryTweenDisabledScope for accurate height tests."
                );

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

                SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                    drawer,
                    dictionaryProperty,
                    typeof(int),
                    typeof(string),
                    isSortedDictionary: false
                );

                pending.isExpanded = false;
                float collapsedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                pending.isExpanded = true;
                float expandedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                float heightDifference = expandedHeight - collapsedHeight;
                float minimumExpectedDifference = EditorGUIUtility.singleLineHeight;
                string diagnostics =
                    $"collapsedHeight={collapsedHeight}, expandedHeight={expandedHeight}, "
                    + $"heightDifference={heightDifference}, pendingIsExpanded={pending.isExpanded}, "
                    + $"foldoutAnimExists={pending.foldoutAnim != null}, tweenEnabled={tweenEnabled}";
                Assert.Greater(
                    heightDifference,
                    minimumExpectedDifference,
                    $"Expanded pending entry height should be at least {minimumExpectedDifference}px larger than collapsed. Actual difference: {heightDifference}px. Diagnostics: {diagnostics}"
                );
            }
        }

        [Test]
        public void GetPropertyHeightPendingExpandAffectsHeightEvenWithDictionaryEntries()
        {
            using (new DictionaryTweenDisabledScope())
            {
                bool tweenEnabled = SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                );
                Assert.IsFalse(
                    tweenEnabled,
                    "Tween should be disabled by DictionaryTweenDisabledScope for accurate height tests."
                );

                TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
                host.dictionary.Add(1, "one");
                host.dictionary.Add(2, "two");
                host.dictionary.Add(3, "three");

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

                SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                    drawer,
                    dictionaryProperty,
                    typeof(int),
                    typeof(string),
                    isSortedDictionary: false
                );

                pending.isExpanded = false;
                float collapsedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                pending.isExpanded = true;
                float expandedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                string diagnostics =
                    $"collapsedHeight={collapsedHeight}, expandedHeight={expandedHeight}, "
                    + $"pendingIsExpanded={pending.isExpanded}, foldoutAnimExists={pending.foldoutAnim != null}, "
                    + $"tweenEnabled={tweenEnabled}";
                Assert.Greater(
                    expandedHeight,
                    collapsedHeight,
                    $"Expanding the pending entry should increase height even when the dictionary has existing entries. Diagnostics: {diagnostics}"
                );
            }
        }

        [Test]
        public void GetPropertyHeightTogglingPendingMultipleTimesUpdatesHeightCorrectly()
        {
            using (new DictionaryTweenDisabledScope())
            {
                bool tweenEnabled = SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                );
                Assert.IsFalse(
                    tweenEnabled,
                    "Tween should be disabled by DictionaryTweenDisabledScope for accurate height tests."
                );

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

                SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                    drawer,
                    dictionaryProperty,
                    typeof(int),
                    typeof(string),
                    isSortedDictionary: false
                );

                pending.isExpanded = false;
                float collapsed1 = drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);

                pending.isExpanded = true;
                float expanded1 = drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);

                pending.isExpanded = false;
                float collapsed2 = drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);

                pending.isExpanded = true;
                float expanded2 = drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);

                string diagnostics =
                    $"collapsed1={collapsed1}, expanded1={expanded1}, "
                    + $"collapsed2={collapsed2}, expanded2={expanded2}, "
                    + $"pendingIsExpanded={pending.isExpanded}, foldoutAnimExists={pending.foldoutAnim != null}, "
                    + $"tweenEnabled={tweenEnabled}";
                Assert.AreEqual(
                    collapsed1,
                    collapsed2,
                    0.001f,
                    $"Collapsed heights should be consistent across multiple toggles. Diagnostics: {diagnostics}"
                );
                Assert.AreEqual(
                    expanded1,
                    expanded2,
                    0.001f,
                    $"Expanded heights should be consistent across multiple toggles. Diagnostics: {diagnostics}"
                );
                Assert.Greater(
                    expanded1,
                    collapsed1,
                    $"Expanded height should always be greater than collapsed height. Diagnostics: {diagnostics}"
                );
            }
        }

        [Test]
        public void GetPropertyHeightSortedDictionaryPendingExpandBehavesCorrectly()
        {
            using (new SortedDictionaryTweenDisabledScope())
            {
                bool tweenEnabled = SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: true
                );
                Assert.IsFalse(
                    tweenEnabled,
                    "Tween should be disabled by SortedDictionaryTweenDisabledScope for accurate height tests."
                );

                TestSortedDictionaryHost host = CreateScriptableObject<TestSortedDictionaryHost>();
                SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
                serializedObject.Update();
                SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                    nameof(TestSortedDictionaryHost.dictionary)
                );
                dictionaryProperty.isExpanded = true;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                SerializableDictionaryPropertyDrawer drawer = new();
                AssignDictionaryFieldInfo(
                    drawer,
                    typeof(TestSortedDictionaryHost),
                    nameof(TestSortedDictionaryHost.dictionary)
                );

                SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                    drawer,
                    dictionaryProperty,
                    typeof(int),
                    typeof(string),
                    isSortedDictionary: true
                );

                pending.isExpanded = false;
                float collapsedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                pending.isExpanded = true;
                float expandedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                string diagnostics =
                    $"collapsedHeight={collapsedHeight}, expandedHeight={expandedHeight}, "
                    + $"pendingIsExpanded={pending.isExpanded}, foldoutAnimExists={pending.foldoutAnim != null}, "
                    + $"tweenEnabled={tweenEnabled}";
                Assert.Greater(
                    expandedHeight,
                    collapsedHeight,
                    $"Sorted dictionary should also update height when pending entry is expanded. Diagnostics: {diagnostics}"
                );
            }
        }

        [Test]
        public void GetPropertyHeightComplexValueDictionaryPendingExpandUpdatesHeight()
        {
            using (new DictionaryTweenDisabledScope())
            {
                bool tweenEnabled = SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                );
                Assert.IsFalse(
                    tweenEnabled,
                    "Tween should be disabled by DictionaryTweenDisabledScope for accurate height tests."
                );

                ComplexValueDictionaryHost host =
                    CreateScriptableObject<ComplexValueDictionaryHost>();
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

                pending.isExpanded = false;
                float collapsedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                pending.isExpanded = true;
                float expandedHeight = drawer.GetPropertyHeight(
                    dictionaryProperty,
                    GUIContent.none
                );

                string diagnostics =
                    $"collapsedHeight={collapsedHeight}, expandedHeight={expandedHeight}, "
                    + $"pendingIsExpanded={pending.isExpanded}, foldoutAnimExists={pending.foldoutAnim != null}, "
                    + $"tweenEnabled={tweenEnabled}";
                Assert.Greater(
                    expandedHeight,
                    collapsedHeight,
                    $"Complex value dictionary should update height when pending entry is expanded. Diagnostics: {diagnostics}"
                );
            }
        }

        [Test]
        public void GetPropertyHeightMainFoldoutCollapsedIgnoresPendingState()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            AssignDictionaryFieldInfo(
                drawer,
                typeof(TestDictionaryHost),
                nameof(TestDictionaryHost.dictionary)
            );

            float collapsedHeight1 = drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);

            SerializableDictionaryPropertyDrawer.PendingEntry pending = GetPendingEntry(
                drawer,
                dictionaryProperty,
                typeof(int),
                typeof(string),
                isSortedDictionary: false
            );
            pending.isExpanded = true;

            float collapsedHeight2 = drawer.GetPropertyHeight(dictionaryProperty, GUIContent.none);

            Assert.AreEqual(
                collapsedHeight1,
                collapsedHeight2,
                0.001f,
                "When the main dictionary foldout is collapsed, pending entry state should not affect height."
            );
        }

        private sealed class DictionaryTweenDisabledScope : IDisposable
        {
            private readonly bool originalValue;
            private readonly FieldInfo fieldInfo;
            private readonly UnityHelpersSettings settings;
            private bool disposed;

            public DictionaryTweenDisabledScope()
            {
                settings = UnityHelpersSettings.instance;

                fieldInfo = typeof(UnityHelpersSettings).GetField(
                    "serializableDictionaryFoldoutTweenEnabled",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                if (fieldInfo == null)
                {
                    throw new InvalidOperationException(
                        "Could not locate serializableDictionaryFoldoutTweenEnabled field via reflection."
                    );
                }

                originalValue = (bool)fieldInfo.GetValue(settings);
                fieldInfo.SetValue(settings, false);
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                fieldInfo.SetValue(settings, originalValue);
            }
        }

        private sealed class SortedDictionaryTweenDisabledScope : IDisposable
        {
            private readonly bool originalValue;
            private readonly FieldInfo fieldInfo;
            private readonly UnityHelpersSettings settings;
            private bool disposed;

            public SortedDictionaryTweenDisabledScope()
            {
                settings = UnityHelpersSettings.instance;

                fieldInfo = typeof(UnityHelpersSettings).GetField(
                    "serializableSortedDictionaryFoldoutTweenEnabled",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                if (fieldInfo == null)
                {
                    throw new InvalidOperationException(
                        "Could not locate serializableSortedDictionaryFoldoutTweenEnabled field via reflection."
                    );
                }

                originalValue = (bool)fieldInfo.GetValue(settings);
                fieldInfo.SetValue(settings, false);
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                fieldInfo.SetValue(settings, originalValue);
            }
        }
    }
}
