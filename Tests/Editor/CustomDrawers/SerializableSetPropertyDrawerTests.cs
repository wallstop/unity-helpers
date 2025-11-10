namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class SerializableSetPropertyDrawerTests : CommonTestBase
    {
        private sealed class HashSetHost : ScriptableObject
        {
            public SerializableHashSet<int> set = new SerializableHashSet<int>();
        }

        private sealed class StringSetHost : ScriptableObject
        {
            public SerializableHashSet<string> set = new SerializableHashSet<string>();
        }

        private sealed class SortedSetHost : ScriptableObject
        {
            public SerializableSortedSet<int> set = new SerializableSortedSet<int>();
        }

        private sealed class SortedStringSetHost : ScriptableObject
        {
            public SerializableSortedSet<string> set = new SerializableSortedSet<string>();
        }

        private sealed class ObjectSetHost : ScriptableObject
        {
            public SerializableHashSet<TestData> set = new SerializableHashSet<TestData>();
        }

        private sealed class TestData : ScriptableObject { }

        [Test]
        public void GetPropertyHeightClampsPageSize()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            for (int i = 0; i < 128; i++)
            {
                host.set.Add(i);
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            pagination.pageSize = 4096;

            drawer.GetPropertyHeight(setProperty, GUIContent.none);

            Assert.AreEqual(
                UnityHelpersSettings.GetSerializableSetPageSize(),
                pagination.pageSize,
                "Page size should track the configured Unity Helpers setting."
            );
        }

        [Test]
        public void EvaluateDuplicateStateDetectsDuplicateEntries()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            ISerializableSetInspector inspector = (ISerializableSetInspector)host.set;
            Array duplicates = Array.CreateInstance(inspector.ElementType, 3);
            duplicates.SetValue(2, 0);
            duplicates.SetValue(2, 1);
            duplicates.SetValue(4, 2);
            inspector.SetSerializedItemsSnapshot(duplicates, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializableSetPropertyDrawer.DuplicateState state = drawer.EvaluateDuplicateState(
                setProperty,
                itemsProperty,
                force: true
            );

            Assert.IsTrue(state.hasDuplicates);
            CollectionAssert.AreEquivalent(new int[] { 0, 1 }, state.duplicateIndices);
            StringAssert.Contains("Value 2", state.summary);
        }

        [Test]
        public void NullEntriesProduceInspectorWarnings()
        {
            ObjectSetHost host = CreateScriptableObject<ObjectSetHost>();
            ISerializableSetInspector inspector = (ISerializableSetInspector)host.set;
            Array values = Array.CreateInstance(inspector.ElementType, 2);
            values.SetValue(null, 0);
            values.SetValue(CreateScriptableObject<TestData>(), 1);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(ObjectSetHost.set)
            );
            setProperty.isExpanded = true;
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializableSetPropertyDrawer.NullEntryState state = drawer.EvaluateNullEntryState(
                setProperty,
                itemsProperty
            );

            Assert.IsTrue(state.hasNullEntries);
            CollectionAssert.AreEquivalent(new int[] { 0 }, state.nullIndices);
            Assert.IsTrue(state.tooltips.ContainsKey(0));
            StringAssert.Contains("Null entry", state.summary);
        }

        [Test]
        public void SortedSetEditingPreservesSerializedEntry()
        {
            SortedStringSetHost host = CreateScriptableObject<SortedStringSetHost>();
            ISerializableSetInspector inspector = (ISerializableSetInspector)host.set;
            Array values = Array.CreateInstance(inspector.ElementType, 1);
            values.SetValue(string.Empty, 0);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedStringSetHost.set)
            );
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            itemsProperty.GetArrayElementAtIndex(0).stringValue = "delta";
            Assert.IsTrue(serializedObject.ApplyModifiedProperties());

            SerializableSetPropertyDrawer.SyncRuntimeSet(setProperty);

            serializedObject.Update();
            setProperty = serializedObject.FindProperty(nameof(SortedStringSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(1, itemsProperty.arraySize, "Entry should remain serialized.");
            Assert.AreEqual("delta", itemsProperty.GetArrayElementAtIndex(0).stringValue);
        }

        [Test]
        public void SortedSetStringEditingRetainsEntryWhenAddedThroughDrawer()
        {
            SortedStringSetHost host = CreateScriptableObject<SortedStringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedStringSetHost.set)
            );
            string propertyPath = setProperty.propertyPath;
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                ),
                "Drawer failed to add string entry."
            );

            serializedObject.Update();
            setProperty = serializedObject.FindProperty(propertyPath);
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            Assert.AreEqual(1, itemsProperty.arraySize);

            itemsProperty.GetArrayElementAtIndex(0).stringValue = "delta";
            Assert.IsTrue(serializedObject.ApplyModifiedProperties());

            SerializableSetPropertyDrawer.SyncRuntimeSet(setProperty);
            serializedObject.Update();

            setProperty = serializedObject.FindProperty(propertyPath);
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(1, itemsProperty.arraySize);
            Assert.AreEqual("delta", itemsProperty.GetArrayElementAtIndex(0).stringValue);
        }

        [Test]
        public void SortedSetEditingMaintainsElementOrder()
        {
            SortedStringSetHost host = CreateScriptableObject<SortedStringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedStringSetHost.set)
            );
            string propertyPath = setProperty.propertyPath;
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                )
            );
            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                )
            );

            serializedObject.Update();
            setProperty = serializedObject.FindProperty(propertyPath);
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            string[] initialOrder = ReadStringValues(itemsProperty);
            Assert.AreEqual(2, initialOrder.Length);
            Assert.That(initialOrder[0], Is.EqualTo(string.Empty));

            itemsProperty.GetArrayElementAtIndex(0).stringValue = "delta";
            itemsProperty.GetArrayElementAtIndex(1).stringValue = "alpha";
            Assert.IsTrue(serializedObject.ApplyModifiedProperties());

            SerializableSetPropertyDrawer.SyncRuntimeSet(setProperty);
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            CollectionAssert.AreEqual(new[] { "delta", "alpha" }, ReadStringValues(itemsProperty));
        }

        [Test]
        public void SortedSetSortButtonReordersEntries()
        {
            SortedStringSetHost host = CreateScriptableObject<SortedStringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedStringSetHost.set)
            );
            string propertyPath = setProperty.propertyPath;
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                )
            );
            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                )
            );

            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            itemsProperty.GetArrayElementAtIndex(0).stringValue = "zeta";
            itemsProperty.GetArrayElementAtIndex(1).stringValue = "beta";
            Assert.IsTrue(serializedObject.ApplyModifiedProperties());

            SerializableSetPropertyDrawer.SyncRuntimeSet(setProperty);
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            CollectionAssert.AreEqual(new[] { "zeta", "beta" }, ReadStringValues(itemsProperty));

            drawer.SortElements(setProperty, itemsProperty);
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            CollectionAssert.AreEqual(new[] { "beta", "zeta" }, ReadStringValues(itemsProperty));
        }

        [Test]
        public void HashSetStringEditingRetainsEntryWhenAddedThroughDrawer()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            string propertyPath = setProperty.propertyPath;
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                ),
                "Drawer failed to add string entry."
            );

            serializedObject.Update();
            setProperty = serializedObject.FindProperty(propertyPath);
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            Assert.AreEqual(1, itemsProperty.arraySize);

            itemsProperty.GetArrayElementAtIndex(0).stringValue = "gamma";
            Assert.IsTrue(serializedObject.ApplyModifiedProperties());

            SerializableSetPropertyDrawer.SyncRuntimeSet(setProperty);
            serializedObject.Update();

            setProperty = serializedObject.FindProperty(propertyPath);
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(1, itemsProperty.arraySize);
            Assert.AreEqual("gamma", itemsProperty.GetArrayElementAtIndex(0).stringValue);
        }

        [Test]
        public void HashSetEditingMaintainsElementOrder()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            string propertyPath = setProperty.propertyPath;
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                )
            );
            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                )
            );

            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            string[] initialOrder = ReadStringValues(itemsProperty);
            Assert.AreEqual(2, initialOrder.Length);
            Assert.That(initialOrder[0], Is.EqualTo(string.Empty));

            itemsProperty.GetArrayElementAtIndex(0).stringValue = "delta";
            itemsProperty.GetArrayElementAtIndex(1).stringValue = "alpha";
            Assert.IsTrue(serializedObject.ApplyModifiedProperties());

            SerializableSetPropertyDrawer.SyncRuntimeSet(setProperty);
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            CollectionAssert.AreEqual(new[] { "delta", "alpha" }, ReadStringValues(itemsProperty));
        }

        [Test]
        public void ExpandRowRectVerticallyExtendsSelectionBounds()
        {
            Rect baseRect = new Rect(5f, 10f, 25f, 16f);
            Rect expanded = SerializableSetPropertyDrawer.ExpandRowRectVertically(baseRect);

            Assert.That(expanded.yMin, Is.LessThan(baseRect.yMin));
            Assert.That(expanded.yMax, Is.GreaterThan(baseRect.yMax));
            Assert.That(expanded.width, Is.EqualTo(baseRect.width));
        }

        [Test]
        public void TryAddNewElementAllowsMultipleNullPlaceholders()
        {
            ObjectSetHost host = CreateScriptableObject<ObjectSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(ObjectSetHost.set)
            );
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);

            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    setProperty.propertyPath,
                    ref itemsProperty,
                    pagination
                )
            );

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            setProperty = serializedObject.FindProperty(nameof(ObjectSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.IsTrue(
                drawer.TryAddNewElement(
                    ref setProperty,
                    setProperty.propertyPath,
                    ref itemsProperty,
                    pagination
                )
            );

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            setProperty = serializedObject.FindProperty(nameof(ObjectSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.IsNotNull(itemsProperty);
            Assert.AreEqual(2, itemsProperty.arraySize);
            AssertPlaceholderIsNull(itemsProperty.GetArrayElementAtIndex(0));
            AssertPlaceholderIsNull(itemsProperty.GetArrayElementAtIndex(1));
        }

        [Test]
        public void TryAddNewElementAppendsDefaultValue()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            bool added = drawer.TryAddNewElement(
                ref setProperty,
                setProperty.propertyPath,
                ref itemsProperty,
                pagination
            );

            Assert.IsTrue(added, "Drawer failed to append a new element.");

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.IsNotNull(itemsProperty);
            Assert.AreEqual(1, itemsProperty.arraySize);
            Assert.AreEqual(1, host.set.Count);
        }

        [Test]
        public void TryAddNewElementPreservesSerializedDuplicates()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            ISerializableSetInspector inspector = (ISerializableSetInspector)host.set;
            Array duplicates = Array.CreateInstance(inspector.ElementType, 3);
            duplicates.SetValue(1, 0);
            duplicates.SetValue(1, 1);
            duplicates.SetValue(2, 2);
            inspector.SetSerializedItemsSnapshot(duplicates, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            Assert.IsNotNull(itemsProperty);
            Assert.AreEqual(3, itemsProperty.arraySize);

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            bool added = drawer.TryAddNewElement(
                ref setProperty,
                setProperty.propertyPath,
                ref itemsProperty,
                pagination
            );

            Assert.IsTrue(
                added,
                "Drawer failed to append a new element when duplicates were present."
            );

            serializedObject.Update();
            setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(4, itemsProperty.arraySize);
            Assert.AreEqual(1, itemsProperty.GetArrayElementAtIndex(0).intValue);
            Assert.AreEqual(1, itemsProperty.GetArrayElementAtIndex(1).intValue);
            Assert.AreEqual(0, itemsProperty.GetArrayElementAtIndex(3).intValue);
            Assert.AreEqual(3, host.set.Count);
            SerializableSetPropertyDrawer.DuplicateState duplicateState =
                drawer.EvaluateDuplicateState(setProperty, itemsProperty, force: true);
            CollectionAssert.AreEquivalent(new int[] { 0, 1 }, duplicateState.duplicateIndices);
        }

        [Test]
        public void TryAddNewElementPreservesSerializedDuplicatesForSortedSet()
        {
            SortedSetHost host = CreateScriptableObject<SortedSetHost>();
            ISerializableSetInspector inspector = (ISerializableSetInspector)host.set;
            Array duplicates = Array.CreateInstance(inspector.ElementType, 3);
            duplicates.SetValue(3, 0);
            duplicates.SetValue(3, 1);
            duplicates.SetValue(5, 2);
            inspector.SetSerializedItemsSnapshot(duplicates, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedSetHost.set)
            );
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            Assert.IsNotNull(itemsProperty);
            Assert.AreEqual(3, itemsProperty.arraySize);

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            bool added = drawer.TryAddNewElement(
                ref setProperty,
                setProperty.propertyPath,
                ref itemsProperty,
                pagination
            );

            Assert.IsTrue(
                added,
                "Drawer failed to append a new element to the sorted set when duplicates were present."
            );

            serializedObject.Update();
            setProperty = serializedObject.FindProperty(nameof(SortedSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(4, itemsProperty.arraySize);
            Assert.AreEqual(3, itemsProperty.GetArrayElementAtIndex(0).intValue);
            Assert.AreEqual(3, itemsProperty.GetArrayElementAtIndex(1).intValue);
            Assert.AreEqual(0, itemsProperty.GetArrayElementAtIndex(3).intValue);
            Assert.AreEqual(3, host.set.Count);
            SerializableSetPropertyDrawer.DuplicateState duplicateState =
                drawer.EvaluateDuplicateState(setProperty, itemsProperty, force: true);
            CollectionAssert.AreEquivalent(new int[] { 0, 1 }, duplicateState.duplicateIndices);
        }

        [Test]
        public void SortElementsOrdersSerializedIntegers()
        {
            SortedSetHost host = CreateScriptableObject<SortedSetHost>();
            host.set.Add(5);
            host.set.Add(1);
            host.set.Add(3);
            host.set.OnBeforeSerialize();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedSetHost.set)
            );
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            itemsProperty.GetArrayElementAtIndex(0).intValue = 5;
            itemsProperty.GetArrayElementAtIndex(1).intValue = 1;
            itemsProperty.GetArrayElementAtIndex(2).intValue = 3;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            drawer.SortElements(setProperty, itemsProperty);

            serializedObject.Update();
            setProperty = serializedObject.FindProperty(nameof(SortedSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int[] serialized = Enumerable
                .Range(0, itemsProperty.arraySize)
                .Select(i => itemsProperty.GetArrayElementAtIndex(i).intValue)
                .ToArray();

            CollectionAssert.AreEqual(new int[] { 1, 3, 5 }, serialized);
            CollectionAssert.AreEqual(new int[] { 1, 3, 5 }, host.set.ToArray());
        }

        private static string[] ReadStringValues(SerializedProperty itemsProperty)
        {
            if (itemsProperty == null || !itemsProperty.isArray)
            {
                return Array.Empty<string>();
            }

            string[] values = new string[itemsProperty.arraySize];
            for (int index = 0; index < itemsProperty.arraySize; index++)
            {
                values[index] = itemsProperty.GetArrayElementAtIndex(index).stringValue;
            }

            return values;
        }

        private static void AssertPlaceholderIsNull(SerializedProperty element)
        {
            switch (element.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    Assert.IsTrue(
                        element.objectReferenceValue == null,
                        "Placeholder object reference should remain null."
                    );
                    break;
                default:
                    Assert.Fail($"Unsupported placeholder property type {element.propertyType}.");
                    break;
            }
        }

        [Test]
        public void RemoveEntryViaReflectionShrinksSerializedArray()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(10);
            host.set.Add(20);
            host.set.Add(30);
            host.set.OnBeforeSerialize();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            MethodInfo removeEntry = typeof(SerializableSetPropertyDrawer).GetMethod(
                "RemoveEntry",
                BindingFlags.Static | BindingFlags.NonPublic
            );
            Assert.IsNotNull(removeEntry);

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            MethodInfo removeValue = typeof(SerializableSetPropertyDrawer).GetMethod(
                "RemoveValueFromSet",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(removeValue);

            int removedValue = itemsProperty.GetArrayElementAtIndex(1).intValue;
            removeEntry.Invoke(null, new object[] { itemsProperty, 1 });
            removeValue.Invoke(
                drawer,
                new object[] { setProperty, setProperty.propertyPath, removedValue }
            );
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(2, itemsProperty.arraySize);
            int[] remaining = Enumerable
                .Range(0, itemsProperty.arraySize)
                .Select(i => itemsProperty.GetArrayElementAtIndex(i).intValue)
                .ToArray();
            CollectionAssert.AreEquivalent(new int[] { 10, 30 }, remaining);
            Assert.AreEqual(2, host.set.Count);
        }
    }
}
