namespace WallstopStudios.UnityHelpers.Tests.Editor.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Editor;

    public sealed class SerializableDictionaryPropertyDrawerTests
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private const BindingFlags StaticFlags =
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private sealed class TestDictionaryHost : ScriptableObject
        {
            public IntStringDictionary dictionary = new();
        }

        [Serializable]
        private sealed class IntStringDictionary : SerializableDictionary<int, string> { }

        [Test]
        public void PageSizeClampPreventsExcessiveCacheGrowth()
        {
            TestDictionaryHost host = ScriptableObject.CreateInstance<TestDictionaryHost>();
            try
            {
                for (int i = 0; i < 512; i++)
                {
                    host.dictionary.Add(i, $"Value {i}");
                }

                SerializedObject serializedObject = new SerializedObject(host);
                serializedObject.Update();
                SerializedProperty dictionaryProperty = serializedObject.FindProperty("dictionary");
                SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative("_keys");
                SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                    "_values"
                );

                SerializableDictionaryPropertyDrawer drawer =
                    new SerializableDictionaryPropertyDrawer();

                MethodInfo paginationMethod =
                    typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                        "GetOrCreatePaginationState",
                        InstanceFlags
                    );
                object pagination = paginationMethod.Invoke(
                    drawer,
                    new object[] { dictionaryProperty }
                );
                Type paginationType = pagination.GetType();
                FieldInfo pageSizeField = paginationType.GetField(
                    "pageSize",
                    BindingFlags.Instance | BindingFlags.Public
                );
                pageSizeField.SetValue(pagination, 512);

                MethodInfo listMethod = typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                    "GetOrCreateList",
                    InstanceFlags
                );
                listMethod.Invoke(
                    drawer,
                    new object[] { dictionaryProperty, keysProperty, valuesProperty }
                );

                FieldInfo maxPageSizeField = typeof(SerializableDictionaryPropertyDrawer).GetField(
                    "MaxPageSize",
                    StaticFlags
                );
                int maxPageSize = (int)maxPageSizeField.GetValue(null);
                int clampedPageSize = (int)pageSizeField.GetValue(pagination);

                Assert.AreEqual(maxPageSize, clampedPageSize);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(host);
            }
        }

        [Test]
        public void SyncSelectionKeepsIndexWithinVisiblePage()
        {
            TestDictionaryHost host = ScriptableObject.CreateInstance<TestDictionaryHost>();
            try
            {
                for (int i = 0; i < 30; i++)
                {
                    host.dictionary.Add(i, $"Item {i}");
                }

                SerializedObject serializedObject = new SerializedObject(host);
                serializedObject.Update();
                SerializedProperty dictionaryProperty = serializedObject.FindProperty("dictionary");
                SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative("_keys");
                SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                    "_values"
                );

                SerializableDictionaryPropertyDrawer drawer =
                    new SerializableDictionaryPropertyDrawer();

                MethodInfo paginationMethod =
                    typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                        "GetOrCreatePaginationState",
                        InstanceFlags
                    );
                object pagination = paginationMethod.Invoke(
                    drawer,
                    new object[] { dictionaryProperty }
                );
                Type paginationType = pagination.GetType();
                FieldInfo pageIndexField = paginationType.GetField(
                    "pageIndex",
                    BindingFlags.Instance | BindingFlags.Public
                );
                FieldInfo pageSizeField = paginationType.GetField(
                    "pageSize",
                    BindingFlags.Instance | BindingFlags.Public
                );
                FieldInfo selectedIndexField = paginationType.GetField(
                    "selectedIndex",
                    BindingFlags.Instance | BindingFlags.Public
                );

                pageSizeField.SetValue(pagination, 10);
                pageIndexField.SetValue(pagination, 0);

                MethodInfo listMethod = typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                    "GetOrCreateList",
                    InstanceFlags
                );
                ReorderableList list = (ReorderableList)
                    listMethod.Invoke(
                        drawer,
                        new object[] { dictionaryProperty, keysProperty, valuesProperty }
                    );

                MethodInfo listKeyMethod = typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                    "GetListKey",
                    StaticFlags
                );
                string listKey = (string)
                    listKeyMethod.Invoke(null, new object[] { dictionaryProperty });

                MethodInfo ensureCacheMethod =
                    typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                        "EnsurePageCache",
                        InstanceFlags
                    );
                object cache = ensureCacheMethod.Invoke(
                    drawer,
                    new object[] { listKey, keysProperty, valuesProperty, pagination }
                );

                MethodInfo syncMethod = typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                    "SyncListSelectionWithPagination",
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                selectedIndexField.SetValue(pagination, 25);
                pageIndexField.SetValue(pagination, 2);
                syncMethod.Invoke(null, new object[] { list, pagination, cache });

                Assert.AreEqual(5, list.index);
                Assert.AreEqual(25, selectedIndexField.GetValue(pagination));

                pageIndexField.SetValue(pagination, 0);
                cache = ensureCacheMethod.Invoke(
                    drawer,
                    new object[] { listKey, keysProperty, valuesProperty, pagination }
                );
                syncMethod.Invoke(null, new object[] { list, pagination, cache });

                Assert.AreEqual(0, list.index);
                Assert.AreEqual(0, selectedIndexField.GetValue(pagination));
            }
            finally
            {
                ScriptableObject.DestroyImmediate(host);
            }
        }

        [Test]
        public void MarkListCacheDirtyClearsCachedEntries()
        {
            TestDictionaryHost host = ScriptableObject.CreateInstance<TestDictionaryHost>();
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    host.dictionary.Add(i, $"Entry {i}");
                }

                SerializedObject serializedObject = new SerializedObject(host);
                serializedObject.Update();
                SerializedProperty dictionaryProperty = serializedObject.FindProperty("dictionary");
                SerializedProperty keysProperty = dictionaryProperty.FindPropertyRelative("_keys");
                SerializedProperty valuesProperty = dictionaryProperty.FindPropertyRelative(
                    "_values"
                );

                SerializableDictionaryPropertyDrawer drawer =
                    new SerializableDictionaryPropertyDrawer();

                MethodInfo paginationMethod =
                    typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                        "GetOrCreatePaginationState",
                        InstanceFlags
                    );
                object pagination = paginationMethod.Invoke(
                    drawer,
                    new object[] { dictionaryProperty }
                );

                MethodInfo listMethod = typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                    "GetOrCreateList",
                    InstanceFlags
                );
                listMethod.Invoke(
                    drawer,
                    new object[] { dictionaryProperty, keysProperty, valuesProperty }
                );

                MethodInfo listKeyMethod = typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                    "GetListKey",
                    StaticFlags
                );
                string listKey = (string)
                    listKeyMethod.Invoke(null, new object[] { dictionaryProperty });

                MethodInfo ensureCacheMethod =
                    typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                        "EnsurePageCache",
                        InstanceFlags
                    );
                object cache = ensureCacheMethod.Invoke(
                    drawer,
                    new object[] { listKey, keysProperty, valuesProperty, pagination }
                );

                Type cacheType = cache.GetType();
                FieldInfo entriesField = cacheType.GetField(
                    "entries",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );
                FieldInfo dirtyField = cacheType.GetField(
                    "dirty",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                IList entries = (IList)entriesField.GetValue(cache);
                Assert.Greater(entries.Count, 0);
                Assert.IsFalse((bool)dirtyField.GetValue(cache));

                MethodInfo markDirtyMethod = typeof(SerializableDictionaryPropertyDrawer).GetMethod(
                    "MarkListCacheDirty",
                    InstanceFlags
                );
                markDirtyMethod.Invoke(drawer, new object[] { listKey });

                Assert.AreEqual(0, entries.Count);
                Assert.IsTrue((bool)dirtyField.GetValue(cache));
            }
            finally
            {
                ScriptableObject.DestroyImmediate(host);
            }
        }
    }
}
