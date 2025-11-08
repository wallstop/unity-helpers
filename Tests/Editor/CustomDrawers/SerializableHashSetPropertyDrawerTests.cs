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

    public sealed class SerializableHashSetPropertyDrawerTests : CommonTestBase
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

            SerializableHashSetPropertyDrawer drawer = new SerializableHashSetPropertyDrawer();
            SerializableHashSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            pagination.pageSize = 4096;

            drawer.GetPropertyHeight(setProperty, GUIContent.none);

            Assert.AreEqual(
                SerializableHashSetPropertyDrawer.MaxPageSize,
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

            SerializableHashSetPropertyDrawer drawer = new SerializableHashSetPropertyDrawer();
            SerializableHashSetPropertyDrawer.DuplicateState state = drawer.EvaluateDuplicateState(
                setProperty,
                itemsProperty,
                force: true
            );

            Assert.IsTrue(state.HasDuplicates);
            CollectionAssert.AreEquivalent(new int[] { 0, 1 }, state.DuplicateIndices);
            StringAssert.Contains("Value 2", state.Summary);
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

            SerializableHashSetPropertyDrawer drawer = new SerializableHashSetPropertyDrawer();
            drawer.SortElements(setProperty, itemsProperty);

            serializedObject.Update();
            int[] result = Enumerable
                .Range(0, itemsProperty.arraySize)
                .Select(i => itemsProperty.GetArrayElementAtIndex(i).intValue)
                .ToArray();

            CollectionAssert.AreEqual(new int[] { 1, 3, 5 }, result);
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

            MethodInfo removeEntry = typeof(SerializableHashSetPropertyDrawer).GetMethod(
                "RemoveEntry",
                BindingFlags.Static | BindingFlags.NonPublic
            );
            Assert.IsNotNull(removeEntry);

            removeEntry.Invoke(null, new object[] { itemsProperty, 1 });
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            Assert.AreEqual(2, itemsProperty.arraySize);
            int[] remaining = Enumerable
                .Range(0, itemsProperty.arraySize)
                .Select(i => itemsProperty.GetArrayElementAtIndex(i).intValue)
                .ToArray();
            CollectionAssert.AreEquivalent(new int[] { 10, 30 }, remaining);
        }
    }
}
