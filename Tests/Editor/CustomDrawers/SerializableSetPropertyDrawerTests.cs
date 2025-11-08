namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class SerializableSetPropertyDrawerTests : CommonTestBase
    {
        private sealed class HashSetHost : ScriptableObject
        {
            public SerializableHashSet<int> set = new SerializableHashSet<int>();
        }

        private sealed class SortedHashSetHost : ScriptableObject
        {
            public SerializableSortedSet<int> set = new SerializableSortedSet<int>();
        }

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
                SerializableSetPropertyDrawer.MaxPageSize,
                pagination.pageSize,
                "Page size should clamp to drawer maximum."
            );
        }

        [Test]
        public void EvaluateDuplicateStateDetectsDuplicateEntries()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            FieldInfo itemsField = typeof(SerializableHashSet<int>).GetField(
                "_items",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(itemsField);

            itemsField.SetValue(host.set, new int[] { 2, 2, 4 });
            host.set.OnAfterDeserialize();

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

            Assert.IsTrue(state.HasDuplicates);
            CollectionAssert.AreEquivalent(new int[] { 0, 1 }, state.DuplicateIndices);
            StringAssert.Contains("Value 2", state.Summary);
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
            bool added = drawer.TryAddNewElement(
                ref setProperty,
                setProperty.propertyPath,
                ref itemsProperty
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
        public void SortElementsOrdersSerializedIntegers()
        {
            SortedHashSetHost host = CreateScriptableObject<SortedHashSetHost>();
            host.set.Add(5);
            host.set.Add(1);
            host.set.Add(3);
            host.set.OnBeforeSerialize();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedHashSetHost.set)
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
            setProperty = serializedObject.FindProperty(nameof(SortedHashSetHost.set));
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
