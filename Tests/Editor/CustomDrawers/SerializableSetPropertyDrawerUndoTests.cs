// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes;

    public sealed class SerializableSetPropertyDrawerUndoTests : CommonTestBase
    {
        private static string FormatSetContents<T>(IEnumerable<T> set)
        {
            return string.Join(", ", set.Select(item => item?.ToString() ?? "null"));
        }

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            Undo.ClearAll();
        }

        [TearDown]
        public override void TearDown()
        {
            Undo.ClearAll();
            base.TearDown();
        }

        [Test]
        public void TryCommitPendingEntryRegistersUndoAndCanBeReverted()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );

            SerializableSetPropertyDrawer drawer = new();
            string propertyPath = setProperty.propertyPath;
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializableSetPropertyDrawer.PendingEntry pending = drawer.GetOrCreatePendingEntry(
                setProperty,
                propertyPath,
                typeof(string),
                isSortedSet: false
            );
            pending.value = "TestValue";

            ISerializableSetInspector inspector = host.set;
            Assert.IsTrue(inspector != null, "Expected ISerializableSetInspector implementation.");

            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int initialCount = host.set.Count;

            bool result = drawer.TryCommitPendingEntry(
                pending,
                setProperty,
                propertyPath,
                ref itemsProperty,
                pagination,
                inspector
            );

            Assert.IsTrue(result, "TryCommitPendingEntry should succeed.");
            Assert.AreEqual(
                initialCount + 1,
                host.set.Count,
                "Set should have one more entry after commit."
            );
            Assert.IsTrue(
                host.set.Contains("TestValue"),
                "Set should contain the committed value."
            );

            Undo.PerformUndo();

            serializedObject.Update();
            Assert.AreEqual(
                initialCount,
                host.set.Count,
                "Set count should return to initial after undo."
            );
            Assert.IsFalse(
                host.set.Contains("TestValue"),
                "Set should not contain the value after undo."
            );
        }

        [Test]
        public void TryAddNewElementRegistersUndoAndCanBeReverted()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(1);
            host.set.Add(2);
            host.set.Add(3);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));

            SerializableSetPropertyDrawer drawer = new();
            string propertyPath = setProperty.propertyPath;
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);

            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int initialCount = host.set.Count;
            string initialContents = FormatSetContents(host.set);

            bool result = drawer.TryAddNewElement(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination
            );

            Assert.IsTrue(result, "TryAddNewElement should succeed.");
            int afterAddCount = host.set.Count;
            string afterAddContents = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount + 1,
                afterAddCount,
                $"Set should have one more entry. Initial: [{initialContents}], After add: [{afterAddContents}]"
            );

            Undo.PerformUndo();

            serializedObject.Update();
            int afterUndoCount = host.set.Count;
            string afterUndoContents = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount,
                afterUndoCount,
                $"Set count should return to initial after undo. Initial: [{initialContents}], After add: [{afterAddContents}], After undo: [{afterUndoContents}]"
            );
        }

        [Test]
        public void TryClearSetRegistersUndoAndCanBeReverted()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(10);
            host.set.Add(20);
            host.set.Add(30);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int initialCount = host.set.Count;
            Assert.Greater(initialCount, 0, "Set should have entries before clear.");

            drawer.InvokeTryClearSet(ref setProperty, propertyPath, ref itemsProperty);

            serializedObject.Update();
            Assert.AreEqual(0, host.set.Count, "Set should be empty after clear.");

            Undo.PerformUndo();

            serializedObject.Update();
            Assert.AreEqual(
                initialCount,
                host.set.Count,
                "Set count should return to initial after undo."
            );
            Assert.IsTrue(host.set.Contains(10), "Set should contain original values after undo.");
            Assert.IsTrue(host.set.Contains(20), "Set should contain original values after undo.");
            Assert.IsTrue(host.set.Contains(30), "Set should contain original values after undo.");
        }

        [Test]
        public void RemoveSelectedEntryRegistersUndoAndCanBeReverted()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(100);
            host.set.Add(200);
            host.set.Add(300);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            pagination.selectedIndex = 0;

            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int initialCount = host.set.Count;

            drawer.InvokeTryRemoveSelectedEntry(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination
            );

            serializedObject.Update();
            Assert.AreEqual(
                initialCount - 1,
                host.set.Count,
                "Set should have one less entry after removal."
            );

            Undo.PerformUndo();

            serializedObject.Update();
            Assert.AreEqual(
                initialCount,
                host.set.Count,
                "Set count should return to initial after undo."
            );
        }

        [Test]
        public void MoveSelectedEntryRegistersUndoAndCanBeReverted()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(1);
            host.set.Add(2);
            host.set.Add(3);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            pagination.selectedIndex = 0;

            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializedProperty firstElement = itemsProperty.GetArrayElementAtIndex(0);
            int originalFirstValue = firstElement.intValue;

            drawer.InvokeTryMoveSelectedEntry(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination,
                1
            );

            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            SerializedProperty elementAfterMove = itemsProperty.GetArrayElementAtIndex(1);
            Assert.AreEqual(
                originalFirstValue,
                elementAfterMove.intValue,
                "Element should have moved to new position."
            );

            Undo.PerformUndo();

            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            SerializedProperty elementAfterUndo = itemsProperty.GetArrayElementAtIndex(0);
            Assert.AreEqual(
                originalFirstValue,
                elementAfterUndo.intValue,
                "Element should return to original position after undo."
            );
        }

        [Test]
        public void SortElementsRegistersUndoAndCanBeReverted()
        {
            SortedSetHost host = CreateScriptableObject<SortedSetHost>();
            host.set.Add(30);
            host.set.Add(10);
            host.set.Add(20);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedSetHost.set)
            );
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializedProperty first = itemsProperty.GetArrayElementAtIndex(0);
            SerializedProperty second = itemsProperty.GetArrayElementAtIndex(1);
            SerializedProperty third = itemsProperty.GetArrayElementAtIndex(2);
            int originalFirst = first.intValue;
            int originalSecond = second.intValue;
            int originalThird = third.intValue;

            bool sorted = drawer.InvokeTrySortElements(
                ref setProperty,
                propertyPath,
                itemsProperty
            );

            Assert.IsTrue(sorted, "TrySortElements should succeed.");
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            first = itemsProperty.GetArrayElementAtIndex(0);
            second = itemsProperty.GetArrayElementAtIndex(1);
            third = itemsProperty.GetArrayElementAtIndex(2);
            Assert.LessOrEqual(
                first.intValue,
                second.intValue,
                "Elements should be sorted after sort."
            );
            Assert.LessOrEqual(
                second.intValue,
                third.intValue,
                "Elements should be sorted after sort."
            );

            Undo.PerformUndo();

            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            first = itemsProperty.GetArrayElementAtIndex(0);
            second = itemsProperty.GetArrayElementAtIndex(1);
            third = itemsProperty.GetArrayElementAtIndex(2);

            Assert.AreEqual(
                originalFirst,
                first.intValue,
                "First element should return to original position after undo."
            );
            Assert.AreEqual(
                originalSecond,
                second.intValue,
                "Second element should return to original position after undo."
            );
            Assert.AreEqual(
                originalThird,
                third.intValue,
                "Third element should return to original position after undo."
            );
        }

        [Test]
        public void UndoAfterAddThenRemoveRestoresOriginalState()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(1);
            host.set.Add(2);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int initialCount = host.set.Count;
            string initialContents = FormatSetContents(host.set);

            bool addResult = drawer.TryAddNewElement(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination
            );
            Assert.IsTrue(addResult, "Add should succeed.");
            serializedObject.Update();
            string afterAddContents = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount + 1,
                host.set.Count,
                $"Set should have one more entry. Initial: [{initialContents}], After add: [{afterAddContents}]"
            );

            // Increment undo group to ensure each operation is in a separate undo group.
            // Unity's undo system collapses operations with the same name in the same frame.
            Undo.IncrementCurrentGroup();

            pagination.selectedIndex = 0;
            drawer.InvokeTryRemoveSelectedEntry(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination
            );
            serializedObject.Update();
            string afterRemoveContents = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount,
                host.set.Count,
                $"Set count should match after add then remove. After add: [{afterAddContents}], After remove: [{afterRemoveContents}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterFirstUndo = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount + 1,
                host.set.Count,
                $"First undo should restore the added entry. After remove: [{afterRemoveContents}], After undo: [{afterFirstUndo}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterSecondUndo = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount,
                host.set.Count,
                $"Second undo should restore original state. After first undo: [{afterFirstUndo}], After second undo: [{afterSecondUndo}]"
            );
        }

        [Test]
        public void RedoAfterUndoRestoresModifiedState()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(5);
            host.set.Add(10);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int initialCount = host.set.Count;
            string initialContents = FormatSetContents(host.set);

            bool addResult = drawer.TryAddNewElement(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination
            );
            Assert.IsTrue(addResult, "Add should succeed.");
            serializedObject.Update();
            int countAfterAdd = host.set.Count;
            string afterAddContents = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount + 1,
                countAfterAdd,
                $"Set should have one more entry. Initial: [{initialContents}], After add: [{afterAddContents}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            int afterUndoCount = host.set.Count;
            string afterUndoContents = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount,
                afterUndoCount,
                $"Undo should restore original count. Initial: [{initialContents}], After add: [{afterAddContents}], After undo: [{afterUndoContents}]"
            );

            Undo.PerformRedo();
            serializedObject.Update();
            int afterRedoCount = host.set.Count;
            string afterRedoContents = FormatSetContents(host.set);
            Assert.AreEqual(
                countAfterAdd,
                afterRedoCount,
                $"Redo should restore the added entry. After add: [{afterAddContents}], After undo: [{afterUndoContents}], After redo: [{afterRedoContents}]"
            );
        }

        [Test]
        public void MultipleUndoRedoCyclesWorkCorrectly()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(0, host.set.Count, "Set should start empty.");

            drawer.TryAddNewElement(ref setProperty, propertyPath, ref itemsProperty, pagination);
            serializedObject.Update();
            Assert.AreEqual(1, host.set.Count, "Count after first add.");

            // Increment undo group to ensure each operation is in a separate undo group.
            // Unity's undo system collapses operations with the same name in the same frame.
            Undo.IncrementCurrentGroup();

            drawer.TryAddNewElement(ref setProperty, propertyPath, ref itemsProperty, pagination);
            serializedObject.Update();
            Assert.AreEqual(2, host.set.Count, "Count after second add.");

            Undo.IncrementCurrentGroup();

            drawer.TryAddNewElement(ref setProperty, propertyPath, ref itemsProperty, pagination);
            serializedObject.Update();
            string afterThirdAdd = FormatSetContents(host.set);
            Assert.AreEqual(
                3,
                host.set.Count,
                $"Count after third add. Contents: [{afterThirdAdd}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterFirstUndo = FormatSetContents(host.set);
            Assert.AreEqual(
                2,
                host.set.Count,
                $"Count after first undo. Before: [{afterThirdAdd}], After: [{afterFirstUndo}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterSecondUndo = FormatSetContents(host.set);
            Assert.AreEqual(
                1,
                host.set.Count,
                $"Count after second undo. Before: [{afterFirstUndo}], After: [{afterSecondUndo}]"
            );

            Undo.PerformRedo();
            serializedObject.Update();
            string afterRedo = FormatSetContents(host.set);
            Assert.AreEqual(
                2,
                host.set.Count,
                $"Count after redo. Before: [{afterSecondUndo}], After: [{afterRedo}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterUndoAgain = FormatSetContents(host.set);
            Assert.AreEqual(
                1,
                host.set.Count,
                $"Count after undo again. Before: [{afterRedo}], After: [{afterUndoAgain}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterFinalUndo = FormatSetContents(host.set);
            Assert.AreEqual(
                0,
                host.set.Count,
                $"Count after final undo to empty. Before: [{afterUndoAgain}], After: [{afterFinalUndo}]"
            );

            Undo.PerformRedo();
            Undo.PerformRedo();
            Undo.PerformRedo();
            serializedObject.Update();
            string afterMultipleRedos = FormatSetContents(host.set);
            Assert.AreEqual(
                3,
                host.set.Count,
                $"Count after multiple redos. Contents: [{afterMultipleRedos}]"
            );
        }

        [Test]
        public void UndoOnEmptySetDoesNotCauseErrors()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            Assert.AreEqual(0, host.set.Count, "Set should start empty.");

            Undo.PerformUndo();
            serializedObject.Update();

            Assert.AreEqual(0, host.set.Count, "Set should remain empty after undo on empty set.");
        }

        [Test]
        public void ClearEmptySetRegistersNoUndoOperation()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(0, host.set.Count, "Set should start empty.");

            bool result = drawer.InvokeTryClearSet(
                ref setProperty,
                propertyPath,
                ref itemsProperty
            );

            Assert.IsTrue(result, "TryClearSet should return true even for empty set.");
            serializedObject.Update();
            Assert.AreEqual(0, host.set.Count, "Set should remain empty.");
        }

        [Test]
        public void UndoAfterMultipleOperationsPreservesCorrectOrder()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(1);
            host.set.Add(2);
            host.set.Add(3);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            drawer.TryAddNewElement(ref setProperty, propertyPath, ref itemsProperty, pagination);
            serializedObject.Update();
            Assert.AreEqual(4, host.set.Count, "Count after add.");

            // Increment undo group to ensure each operation is in a separate undo group.
            // Unity's undo system collapses operations with the same name in the same frame.
            Undo.IncrementCurrentGroup();

            pagination.selectedIndex = 0;
            drawer.InvokeTryRemoveSelectedEntry(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination
            );
            serializedObject.Update();
            Assert.AreEqual(3, host.set.Count, "Count after remove.");

            Undo.IncrementCurrentGroup();

            drawer.InvokeTryClearSet(ref setProperty, propertyPath, ref itemsProperty);
            serializedObject.Update();
            string afterClear = FormatSetContents(host.set);
            Assert.AreEqual(0, host.set.Count, $"Count after clear. Contents: [{afterClear}]");

            Undo.PerformUndo();
            serializedObject.Update();
            string afterFirstUndo = FormatSetContents(host.set);
            Assert.AreEqual(
                3,
                host.set.Count,
                $"First undo restores before clear. Before undo: [{afterClear}], After undo: [{afterFirstUndo}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterSecondUndo = FormatSetContents(host.set);
            Assert.AreEqual(
                4,
                host.set.Count,
                $"Second undo restores removed entry. Before undo: [{afterFirstUndo}], After undo: [{afterSecondUndo}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterThirdUndo = FormatSetContents(host.set);
            Assert.AreEqual(
                3,
                host.set.Count,
                $"Third undo restores before add. Before undo: [{afterSecondUndo}], After undo: [{afterThirdUndo}]"
            );
        }

        [Test]
        public void MoveEntryDownThenUpThenUndoRestoresOriginalOrder()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(100);
            host.set.Add(200);
            host.set.Add(300);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int originalFirst = itemsProperty.GetArrayElementAtIndex(0).intValue;
            int originalSecond = itemsProperty.GetArrayElementAtIndex(1).intValue;
            int originalThird = itemsProperty.GetArrayElementAtIndex(2).intValue;
            string originalOrder = $"[{originalFirst}, {originalSecond}, {originalThird}]";

            pagination.selectedIndex = 0;
            drawer.InvokeTryMoveSelectedEntry(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination,
                1
            );
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int afterMoveDown0 = itemsProperty.GetArrayElementAtIndex(0).intValue;
            int afterMoveDown1 = itemsProperty.GetArrayElementAtIndex(1).intValue;
            int afterMoveDown2 = itemsProperty.GetArrayElementAtIndex(2).intValue;
            string afterMoveDownOrder = $"[{afterMoveDown0}, {afterMoveDown1}, {afterMoveDown2}]";

            Assert.AreEqual(
                originalFirst,
                afterMoveDown1,
                $"First element moved down. Original: {originalOrder}, After move down: {afterMoveDownOrder}"
            );

            // Increment undo group to ensure each operation is in a separate undo group.
            // Unity's undo system collapses operations with the same name in the same frame.
            Undo.IncrementCurrentGroup();

            pagination.selectedIndex = 1;
            drawer.InvokeTryMoveSelectedEntry(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination,
                -1
            );
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int afterMoveUp0 = itemsProperty.GetArrayElementAtIndex(0).intValue;
            int afterMoveUp1 = itemsProperty.GetArrayElementAtIndex(1).intValue;
            int afterMoveUp2 = itemsProperty.GetArrayElementAtIndex(2).intValue;
            string afterMoveUpOrder = $"[{afterMoveUp0}, {afterMoveUp1}, {afterMoveUp2}]";

            Assert.AreEqual(
                originalFirst,
                afterMoveUp0,
                $"First element moved back up. After move down: {afterMoveDownOrder}, After move up: {afterMoveUpOrder}"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            int afterUndo1Pos0 = itemsProperty.GetArrayElementAtIndex(0).intValue;
            int afterUndo1Pos1 = itemsProperty.GetArrayElementAtIndex(1).intValue;
            int afterUndo1Pos2 = itemsProperty.GetArrayElementAtIndex(2).intValue;
            string afterUndo1Order = $"[{afterUndo1Pos0}, {afterUndo1Pos1}, {afterUndo1Pos2}]";

            Assert.AreEqual(
                originalSecond,
                afterUndo1Pos0,
                $"Undo restores after first move. Original: {originalOrder}, After move down: {afterMoveDownOrder}, After move up: {afterMoveUpOrder}, After first undo: {afterUndo1Order}"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            int afterUndo2Pos0 = itemsProperty.GetArrayElementAtIndex(0).intValue;
            int afterUndo2Pos1 = itemsProperty.GetArrayElementAtIndex(1).intValue;
            int afterUndo2Pos2 = itemsProperty.GetArrayElementAtIndex(2).intValue;
            string afterUndo2Order = $"[{afterUndo2Pos0}, {afterUndo2Pos1}, {afterUndo2Pos2}]";

            Assert.AreEqual(
                originalFirst,
                afterUndo2Pos0,
                $"Second undo restores original order. Original: {originalOrder}, After first undo: {afterUndo1Order}, After second undo: {afterUndo2Order}"
            );
        }

        [Test]
        [TestCase(0, Description = "Empty set")]
        [TestCase(1, Description = "Single element set")]
        [TestCase(5, Description = "Multiple element set")]
        public void TryAddNewElementUndoWorksWithVaryingInitialSetSizes(int initialCount)
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            for (int i = 0; i < initialCount; i++)
            {
                host.set.Add(i * 100);
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));

            SerializableSetPropertyDrawer drawer = new();
            string propertyPath = setProperty.propertyPath;
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);

            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int beforeAddCount = host.set.Count;
            string beforeAddContents = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount,
                beforeAddCount,
                $"Initial set size should be {initialCount}. Contents: [{beforeAddContents}]"
            );

            bool result = drawer.TryAddNewElement(
                ref setProperty,
                propertyPath,
                ref itemsProperty,
                pagination
            );

            Assert.IsTrue(result, "TryAddNewElement should succeed.");
            int afterAddCount = host.set.Count;
            string afterAddContents = FormatSetContents(host.set);
            Assert.AreEqual(
                beforeAddCount + 1,
                afterAddCount,
                $"Set should have one more entry. Before: [{beforeAddContents}], After add: [{afterAddContents}]"
            );

            Undo.PerformUndo();

            serializedObject.Update();
            int afterUndoCount = host.set.Count;
            string afterUndoContents = FormatSetContents(host.set);
            Assert.AreEqual(
                beforeAddCount,
                afterUndoCount,
                $"Undo should restore original count. Before add: [{beforeAddContents}], After add: [{afterAddContents}], After undo: [{afterUndoContents}]"
            );
        }

        [Test]
        [TestCase(2, Description = "Small set")]
        [TestCase(5, Description = "Medium set")]
        [TestCase(10, Description = "Larger set")]
        public void TryClearSetUndoWorksWithVaryingSetSizes(int initialCount)
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            for (int i = 0; i < initialCount; i++)
            {
                host.set.Add(i * 10);
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int beforeClearCount = host.set.Count;
            string beforeClearContents = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount,
                beforeClearCount,
                $"Initial set size should be {initialCount}. Contents: [{beforeClearContents}]"
            );

            bool result = drawer.InvokeTryClearSet(
                ref setProperty,
                propertyPath,
                ref itemsProperty
            );

            Assert.IsTrue(result, "TryClearSet should succeed.");
            serializedObject.Update();
            Assert.AreEqual(0, host.set.Count, "Set should be empty after clear.");

            Undo.PerformUndo();

            serializedObject.Update();
            int afterUndoCount = host.set.Count;
            string afterUndoContents = FormatSetContents(host.set);
            Assert.AreEqual(
                beforeClearCount,
                afterUndoCount,
                $"Undo should restore original count. Before: [{beforeClearContents}], After undo: [{afterUndoContents}]"
            );
        }

        [Test]
        public void UndoAfterConsecutiveAddsRestoresEachStateCorrectly()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(0, host.set.Count, "Set should start empty.");

            int numberOfAdds = 5;
            for (int i = 0; i < numberOfAdds; i++)
            {
                // Increment undo group between operations to ensure each add is in a separate undo group.
                // Unity's undo system collapses operations with the same name in the same frame.
                if (i > 0)
                {
                    Undo.IncrementCurrentGroup();
                }

                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                );
                serializedObject.Update();
                Assert.AreEqual(
                    i + 1,
                    host.set.Count,
                    $"Count after add {i + 1}: [{FormatSetContents(host.set)}]"
                );
            }

            for (int i = numberOfAdds - 1; i >= 0; i--)
            {
                Undo.PerformUndo();
                serializedObject.Update();
                Assert.AreEqual(
                    i,
                    host.set.Count,
                    $"Count after undo to {i}: [{FormatSetContents(host.set)}]"
                );
            }
        }

        [Test]
        [TestCase(1, Description = "Single undo")]
        [TestCase(2, Description = "Two undos")]
        [TestCase(3, Description = "Three undos")]
        [TestCase(5, Description = "Five undos")]
        public void ConsecutiveRemovesCanBeUndoneIndividually(int removalCount)
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            for (int i = 0; i < removalCount + 2; i++)
            {
                host.set.Add(i * 100);
            }

            int initialCount = host.set.Count;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            for (int i = 0; i < removalCount; i++)
            {
                if (i > 0)
                {
                    Undo.IncrementCurrentGroup();
                }

                pagination.selectedIndex = 0;
                drawer.InvokeTryRemoveSelectedEntry(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                );
                serializedObject.Update();
                Assert.AreEqual(
                    initialCount - (i + 1),
                    host.set.Count,
                    $"Count after removal {i + 1}: [{FormatSetContents(host.set)}]"
                );
            }

            for (int i = removalCount - 1; i >= 0; i--)
            {
                Undo.PerformUndo();
                serializedObject.Update();
                Assert.AreEqual(
                    initialCount - i,
                    host.set.Count,
                    $"Count after undo {removalCount - i}: [{FormatSetContents(host.set)}]"
                );
            }
        }

        [Test]
        [TestCase(1, 1, Description = "One add then one remove")]
        [TestCase(2, 1, Description = "Two adds then one remove")]
        [TestCase(1, 2, Description = "One add then two removes")]
        [TestCase(3, 2, Description = "Three adds then two removes")]
        public void MixedAddRemoveOperationsCanBeUndone(int addCount, int removeCount)
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(100);
            host.set.Add(200);

            int initialCount = host.set.Count;
            int safeRemoveCount = Math.Min(removeCount, initialCount + addCount);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int expectedCountAfterAdds = initialCount + addCount;
            for (int i = 0; i < addCount; i++)
            {
                if (i > 0)
                {
                    Undo.IncrementCurrentGroup();
                }

                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                );
                serializedObject.Update();
            }
            string afterAdds = FormatSetContents(host.set);
            Assert.AreEqual(
                expectedCountAfterAdds,
                host.set.Count,
                $"Count after {addCount} adds: [{afterAdds}]"
            );

            Undo.IncrementCurrentGroup();

            int expectedCountAfterRemoves = expectedCountAfterAdds - safeRemoveCount;
            for (int i = 0; i < safeRemoveCount; i++)
            {
                if (i > 0)
                {
                    Undo.IncrementCurrentGroup();
                }

                pagination.selectedIndex = 0;
                drawer.InvokeTryRemoveSelectedEntry(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                );
                serializedObject.Update();
            }
            string afterRemoves = FormatSetContents(host.set);
            Assert.AreEqual(
                expectedCountAfterRemoves,
                host.set.Count,
                $"Count after {safeRemoveCount} removes: [{afterRemoves}]"
            );

            for (int i = 0; i < safeRemoveCount; i++)
            {
                Undo.PerformUndo();
                serializedObject.Update();
            }
            string afterUndoRemoves = FormatSetContents(host.set);
            Assert.AreEqual(
                expectedCountAfterAdds,
                host.set.Count,
                $"Count after undoing removes: [{afterUndoRemoves}]"
            );

            for (int i = 0; i < addCount; i++)
            {
                Undo.PerformUndo();
                serializedObject.Update();
            }
            string afterUndoAdds = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount,
                host.set.Count,
                $"Count after undoing adds: [{afterUndoAdds}]"
            );
        }

        [Test]
        [TestCase(2, Description = "Two moves")]
        [TestCase(3, Description = "Three moves")]
        [TestCase(4, Description = "Four moves")]
        public void ConsecutiveMovesCanBeUndoneIndividually(int moveCount)
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            for (int i = 0; i < moveCount + 2; i++)
            {
                host.set.Add(i * 100);
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int originalFirst = itemsProperty.GetArrayElementAtIndex(0).intValue;
            List<string> stateHistory = new() { FormatSetContents(host.set) };

            for (int i = 0; i < moveCount; i++)
            {
                if (i > 0)
                {
                    Undo.IncrementCurrentGroup();
                }

                pagination.selectedIndex = 0;
                drawer.InvokeTryMoveSelectedEntry(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination,
                    1
                );
                serializedObject.Update();
                itemsProperty = setProperty.FindPropertyRelative(
                    SerializableHashSetSerializedPropertyNames.Items
                );
                stateHistory.Add(FormatSetContents(host.set));
            }

            for (int i = moveCount - 1; i >= 0; i--)
            {
                Undo.PerformUndo();
                serializedObject.Update();
                itemsProperty = setProperty.FindPropertyRelative(
                    SerializableHashSetSerializedPropertyNames.Items
                );
                string currentState = FormatSetContents(host.set);
                string expectedState = stateHistory[i];
                Assert.AreEqual(
                    expectedState,
                    currentState,
                    $"State after undo {moveCount - i} should match state {i}. "
                        + $"History: [{string.Join(" -> ", stateHistory)}], Current: [{currentState}]"
                );
            }
        }

        [Test]
        public void UndoAfterClearThenAddRestoresCorrectly()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(10);
            host.set.Add(20);
            host.set.Add(30);

            int initialCount = host.set.Count;
            string initialContents = FormatSetContents(host.set);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            drawer.InvokeTryClearSet(ref setProperty, propertyPath, ref itemsProperty);
            serializedObject.Update();
            string afterClear = FormatSetContents(host.set);
            Assert.AreEqual(
                0,
                host.set.Count,
                $"Set should be empty after clear. Contents: [{afterClear}]"
            );

            Undo.IncrementCurrentGroup();

            drawer.TryAddNewElement(ref setProperty, propertyPath, ref itemsProperty, pagination);
            serializedObject.Update();
            string afterAdd = FormatSetContents(host.set);
            Assert.AreEqual(
                1,
                host.set.Count,
                $"Set should have 1 entry after add. Contents: [{afterAdd}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterUndoAdd = FormatSetContents(host.set);
            Assert.AreEqual(
                0,
                host.set.Count,
                $"Undoing add should leave empty set. Before: [{afterAdd}], After: [{afterUndoAdd}]"
            );

            Undo.PerformUndo();
            serializedObject.Update();
            string afterUndoClear = FormatSetContents(host.set);
            Assert.AreEqual(
                initialCount,
                host.set.Count,
                $"Undoing clear should restore initial state. Initial: [{initialContents}], After: [{afterUndoClear}]"
            );
        }

        [Test]
        public void RedoAfterMultipleUndosRestoresCorrectSequence()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            string propertyPath = setProperty.propertyPath;

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            int operationCount = 4;
            List<int> expectedCounts = new() { 0 };

            for (int i = 0; i < operationCount; i++)
            {
                if (i > 0)
                {
                    Undo.IncrementCurrentGroup();
                }

                drawer.TryAddNewElement(
                    ref setProperty,
                    propertyPath,
                    ref itemsProperty,
                    pagination
                );
                serializedObject.Update();
                expectedCounts.Add(host.set.Count);
            }

            Assert.AreEqual(
                operationCount,
                host.set.Count,
                $"Should have {operationCount} entries after adds. Contents: [{FormatSetContents(host.set)}]"
            );

            for (int i = 0; i < operationCount; i++)
            {
                Undo.PerformUndo();
                serializedObject.Update();
                Assert.AreEqual(
                    expectedCounts[operationCount - 1 - i],
                    host.set.Count,
                    $"Undo {i + 1} should restore count to {expectedCounts[operationCount - 1 - i]}. "
                        + $"Contents: [{FormatSetContents(host.set)}]"
                );
            }

            for (int i = 0; i < operationCount; i++)
            {
                Undo.PerformRedo();
                serializedObject.Update();
                Assert.AreEqual(
                    expectedCounts[i + 1],
                    host.set.Count,
                    $"Redo {i + 1} should restore count to {expectedCounts[i + 1]}. "
                        + $"Contents: [{FormatSetContents(host.set)}]"
                );
            }
        }
    }
}
