namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
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
    using Object = UnityEngine.Object;

    public sealed class SerializableSetPropertyDrawerTests : CommonTestBase
    {
        private sealed class HashSetHost : ScriptableObject
        {
            public SerializableHashSet<int> set = new();
        }

        private sealed class StringSetHost : ScriptableObject
        {
            public SerializableHashSet<string> set = new();
        }

        private sealed class SortedSetHost : ScriptableObject
        {
            public SerializableSortedSet<int> set = new();
        }

        private sealed class SortedStringSetHost : ScriptableObject
        {
            public SerializableSortedSet<string> set = new();
        }

        [Serializable]
        internal sealed class CustomSortedSet : SerializableSortedSet<int> { }

        private sealed class DerivedSortedSetHost : ScriptableObject
        {
            public CustomSortedSet set = new();
        }

        private sealed class ObjectSetHost : ScriptableObject
        {
            public SerializableHashSet<TestData> set = new();
        }

        private sealed class DualStringSetHost : ScriptableObject
        {
            public SerializableHashSet<string> firstSet = new();
            public SerializableHashSet<string> secondSet = new();
        }

        private sealed class MixedFieldsSetHost : ScriptableObject
        {
            public int scalarValue;
            public SerializableHashSet<int> set = new();
        }

        private sealed class SetScalarAfterHost : ScriptableObject
        {
            public SerializableHashSet<int> set = new();
            public int trailingScalar;
        }

        private sealed class ComplexSetHost : ScriptableObject
        {
            public SerializableHashSet<ComplexSetElement> set = new();
        }

        [Serializable]
        private sealed class ComplexSetElement
        {
            public Color primary = Color.cyan;
            public NestedComplexElement nested = new();
        }

        [Serializable]
        private sealed class NestedComplexElement
        {
            public float intensity = 1.25f;
            public Vector2 offset = new(0.5f, -0.5f);
        }

        private sealed class PrivateCtorSetHost : ScriptableObject
        {
            public SerializableHashSet<PrivateCtorElement> set = new();
        }

        private sealed class TestData : ScriptableObject { }

        [Serializable]
        private sealed class CloneableSample
        {
            public int number = 5;
            public string label = "alpha";
        }

        [Serializable]
        private sealed class PrivateCtorElement
        {
            [SerializeField]
            private int magnitude;

            private PrivateCtorElement()
            {
                magnitude = 5;
            }

            public int Magnitude => magnitude;
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

            SerializableSetPropertyDrawer drawer = new();
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
        public void GetPropertyHeightAutoExpandsComplexRowsOnFirstDraw()
        {
            ComplexSetHost host = CreateScriptableObject<ComplexSetHost>();
            host.set.Add(new ComplexSetElement());

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(ComplexSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            Assert.Greater(itemsProperty.arraySize, 0, "Set should contain test entries.");
            SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(0);
            elementProperty.isExpanded = false;

            SerializableSetPropertyDrawer drawer = new();
            drawer.GetPropertyHeight(setProperty, GUIContent.none);

            serializedObject.Update();
            SerializedProperty refreshedItems = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            SerializedProperty refreshedElement = refreshedItems.GetArrayElementAtIndex(0);
            Assert.IsTrue(
                refreshedElement.isExpanded,
                "GetPropertyHeight should expand complex set rows before the first draw so layout reserves enough space."
            );
        }

        [UnityTest]
        public IEnumerator SetRowComplexValueChildControlsHaveSpaceOnFirstDraw()
        {
            ComplexSetHost host = CreateScriptableObject<ComplexSetHost>();
            host.set.Add(new ComplexSetElement());

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(ComplexSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            Assert.Greater(itemsProperty.arraySize, 0, "Set should contain test entries.");
            SerializedProperty elementProperty = itemsProperty.GetArrayElementAtIndex(0);
            elementProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 360f, 520f);
            GUIContent label = new("Set");

            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
            bool heightSetExpandedBefore = setProperty.isExpanded;
            bool heightRowExpandedBefore = elementProperty.isExpanded;

            drawer.GetPropertyHeight(setProperty, label);

            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            elementProperty = itemsProperty.GetArrayElementAtIndex(0);
            bool heightSetExpandedAfter = setProperty.isExpanded;
            bool heightRowExpandedAfter = elementProperty.isExpanded;
            TestContext.WriteLine(
                $"Complex row GetPropertyHeight expansion states -> set: {heightSetExpandedBefore}->{heightSetExpandedAfter}, row: {heightRowExpandedBefore}->{heightRowExpandedAfter}"
            );

            bool drawSetExpandedBefore = setProperty.isExpanded;
            bool drawRowExpandedBefore = elementProperty.isExpanded;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, setProperty, label);
            });

            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            elementProperty = itemsProperty.GetArrayElementAtIndex(0);
            bool drawSetExpandedAfter = setProperty.isExpanded;
            bool drawRowExpandedAfter = elementProperty.isExpanded;
            TestContext.WriteLine(
                $"Complex row OnGUI expansion states -> set: {drawSetExpandedBefore}->{drawSetExpandedAfter}, row: {drawRowExpandedBefore}->{drawRowExpandedAfter}"
            );

            Assert.IsTrue(
                SerializableSetPropertyDrawer.HasLastRowContentRect,
                "First draw should capture row content rect for complex set values."
            );
            Assert.Greater(
                SerializableSetPropertyDrawer.LastRowContentRect.height,
                EditorGUIUtility.singleLineHeight * 1.5f,
                "Complex set elements should render at full height on the first draw."
            );
            Assert.Greater(
                SerializableSetPropertyDrawer.LastRowContentRect.width,
                180f,
                "Complex set elements should render at full width on the first draw."
            );
        }

        [Test]
        public void ManualEntryAddsElementToSet()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializableSetPropertyDrawer.PendingEntry pending = drawer.GetOrCreatePendingEntry(
                setProperty,
                setProperty.propertyPath,
                typeof(string),
                isSortedSet: false
            );
            pending.value = "ManualEntry";

            ISerializableSetInspector inspector = host.set as ISerializableSetInspector;
            Assert.IsNotNull(
                inspector,
                "Expected inspector implementation on SerializableHashSet."
            );

            bool committed = drawer.TryCommitPendingEntry(
                pending,
                setProperty,
                setProperty.propertyPath,
                ref itemsProperty,
                pagination,
                inspector
            );

            Assert.IsTrue(committed, "Manual entry should commit successfully.");
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            Assert.AreEqual(1, itemsProperty.arraySize);
            Assert.AreEqual("ManualEntry", itemsProperty.GetArrayElementAtIndex(0).stringValue);
            Assert.IsNull(pending.errorMessage);
            Assert.IsFalse(pending.isExpanded);
        }

        [Test]
        public void ManualEntryDefaultsSupportPrivateConstructors()
        {
            PrivateCtorSetHost host = CreateScriptableObject<PrivateCtorSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PrivateCtorSetHost.set)
            );

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PendingEntry pending = drawer.GetOrCreatePendingEntry(
                setProperty,
                setProperty.propertyPath,
                typeof(PrivateCtorElement),
                isSortedSet: false
            );

            Assert.IsNotNull(pending.value);
            Assert.IsInstanceOf<PrivateCtorElement>(pending.value);
        }

        [Test]
        public void ManualEntryDefaultsRemainNullForUnityObjects()
        {
            ObjectSetHost host = CreateScriptableObject<ObjectSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(ObjectSetHost.set)
            );

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PendingEntry pending = drawer.GetOrCreatePendingEntry(
                setProperty,
                setProperty.propertyPath,
                typeof(TestData),
                isSortedSet: false
            );

            Assert.IsNull(
                pending.value,
                "UnityEngine.Object entries should start null so inspectors rely on object pickers."
            );
        }

        [UnityTest]
        public IEnumerator ManualEntryUsesObjectPickerForScriptableObjectValues()
        {
            SerializableSetPropertyDrawer.PendingEntry pending = new();
            Rect rect = new(0f, 0f, 200f, EditorGUIUtility.singleLineHeight);

            yield return TestIMGUIExecutor.Run(() =>
            {
                SerializableSetPropertyDrawer.DrawFieldForType(
                    rect,
                    new GUIContent("Value"),
                    null,
                    typeof(TestData),
                    pending
                );
            });

            Assert.IsNull(
                pending.valueWrapper,
                "ScriptableObject values should rely on object pickers rather than PendingValueWrappers."
            );
        }

        [UnityTest]
        public IEnumerator PendingEntryPrimitiveValueDrawsInlineField()
        {
            SerializableSetPropertyDrawer.PendingEntry pending = new();
            Rect rect = new(0f, 0f, 200f, EditorGUIUtility.singleLineHeight);

            yield return TestIMGUIExecutor.Run(() =>
            {
                pending.value = 7;
                pending.value = SerializableSetPropertyDrawer.DrawFieldForType(
                    rect,
                    new GUIContent("Value"),
                    pending.value,
                    typeof(int),
                    pending
                );
            });

            Assert.IsInstanceOf<int>(pending.value, "Primitive values should remain ints.");
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
        public void CloneComplexValueReturnsSameReferenceForStrings()
        {
            string original = Guid.NewGuid().ToString();
            object clone = SerializableSetPropertyDrawer.CloneComplexValue(
                original,
                typeof(string)
            );

            Assert.AreSame(
                original,
                clone,
                "Strings should bypass deep cloning to preserve edits."
            );
        }

        [Test]
        public void CloneComplexValueDeepClonesSerializableReferenceTypes()
        {
            CloneableSample sample = new() { number = 42, label = "sample" };
            CloneableSample clone =
                SerializableSetPropertyDrawer.CloneComplexValue(sample, typeof(CloneableSample))
                as CloneableSample;

            Assert.IsNotNull(clone, "Deep clone should produce an instance of the same type.");
            Assert.AreNotSame(sample, clone, "Clone should not reference the original object.");
            Assert.AreEqual(sample.number, clone.number);
            Assert.AreEqual(sample.label, clone.label);

            sample.number = 99;
            sample.label = "mutated";

            Assert.AreEqual(42, clone.number, "Deep clone should decouple numeric fields.");
            Assert.AreEqual("sample", clone.label, "Deep clone should decouple string fields.");
        }

        [Test]
        public void PendingEntryRetainsStringValueAcrossFrames()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PendingEntry pending = drawer.GetOrCreatePendingEntry(
                setProperty,
                setProperty.propertyPath,
                typeof(string),
                SerializableSetPropertyDrawer.IsSortedSet(setProperty)
            );

            pending.value = "Alpha";
            SerializableSetPropertyDrawer.PendingEntry fetched = drawer.GetOrCreatePendingEntry(
                setProperty,
                setProperty.propertyPath,
                typeof(string),
                SerializableSetPropertyDrawer.IsSortedSet(setProperty)
            );

            Assert.AreEqual("Alpha", fetched.value);
        }

        [Test]
        public void PendingEntriesAreIsolatedPerProperty()
        {
            DualStringSetHost host = CreateScriptableObject<DualStringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty firstSet = serializedObject.FindProperty(
                nameof(DualStringSetHost.firstSet)
            );
            SerializedProperty secondSet = serializedObject.FindProperty(
                nameof(DualStringSetHost.secondSet)
            );

            SerializableSetPropertyDrawer drawer = new();

            SerializableSetPropertyDrawer.PendingEntry firstPending =
                drawer.GetOrCreatePendingEntry(
                    firstSet,
                    firstSet.propertyPath,
                    typeof(string),
                    SerializableSetPropertyDrawer.IsSortedSet(firstSet)
                );
            SerializableSetPropertyDrawer.PendingEntry secondPending =
                drawer.GetOrCreatePendingEntry(
                    secondSet,
                    secondSet.propertyPath,
                    typeof(string),
                    SerializableSetPropertyDrawer.IsSortedSet(secondSet)
                );

            Assert.IsFalse(
                ReferenceEquals(firstPending, secondPending),
                "Each serialized property should have a dedicated pending entry."
            );

            firstPending.value = "alpha";
            secondPending.value = "beta";

            Assert.AreEqual("alpha", firstPending.value);
            Assert.AreEqual("beta", secondPending.value);
        }

        [Test]
        public void ManualEntryRejectsDuplicateValues()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            ISerializableSetInspector inspector = host.set;
            Assert.IsNotNull(
                inspector,
                "Expected inspector implementation on SerializableHashSet."
            );

            Array snapshot = new string[1];
            snapshot.SetValue("Existing", 0);
            inspector.SetSerializedItemsSnapshot(snapshot, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            SerializableSetPropertyDrawer.PendingEntry pending = drawer.GetOrCreatePendingEntry(
                setProperty,
                setProperty.propertyPath,
                typeof(string),
                isSortedSet: false
            );
            pending.value = "Existing";

            bool committed = drawer.TryCommitPendingEntry(
                pending,
                setProperty,
                setProperty.propertyPath,
                ref itemsProperty,
                pagination,
                inspector
            );

            Assert.IsFalse(committed, "Duplicate entries should be rejected.");
            StringAssert.Contains("exists", pending.errorMessage);
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );
            Assert.AreEqual(1, itemsProperty.arraySize);
        }

        [Test]
        public void EvaluateDuplicateStateDetectsDuplicateEntries()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            ISerializableSetInspector inspector = host.set;
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

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.DuplicateState state = drawer.EvaluateDuplicateState(
                setProperty,
                itemsProperty,
                force: true
            );

            Assert.IsTrue(state.hasDuplicates);
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, state.duplicateIndices);
            StringAssert.Contains("Duplicate entry 2", state.summary);
        }

        [Test]
        public void DuplicateStateDoesNotReportDistinctValuesAsDuplicates()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            ISerializableSetInspector inspector = host.set;
            Array values = Array.CreateInstance(inspector.ElementType, 2);
            values.SetValue(3, 0);
            values.SetValue(1, 1);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new();

            SerializableSetPropertyDrawer.DuplicateState firstPass = drawer.EvaluateDuplicateState(
                setProperty,
                itemsProperty,
                force: true
            );
            Assert.IsFalse(firstPass.hasDuplicates);

            SerializableSetPropertyDrawer.DuplicateState secondPass = drawer.EvaluateDuplicateState(
                setProperty,
                itemsProperty,
                force: true
            );
            Assert.IsFalse(secondPass.hasDuplicates);
        }

        [Test]
        public void DuplicateStateReportsDuplicatesAfterCacheReuse()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            ISerializableSetInspector inspector = host.set;
            Array values = Array.CreateInstance(inspector.ElementType, 2);
            values.SetValue(5, 0);
            values.SetValue(5, 1);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.DuplicateState state = drawer.EvaluateDuplicateState(
                setProperty,
                itemsProperty,
                force: true
            );

            Assert.IsTrue(state.hasDuplicates);
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, state.duplicateIndices);
            StringAssert.Contains("Duplicate entry 5", state.summary);
        }

        [Test]
        public void NullEntriesProduceInspectorWarnings()
        {
            ObjectSetHost host = CreateScriptableObject<ObjectSetHost>();
            ISerializableSetInspector inspector = host.set;
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

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.NullEntryState state = drawer.EvaluateNullEntryState(
                setProperty,
                itemsProperty
            );

            Assert.IsTrue(state.hasNullEntries);
            CollectionAssert.AreEquivalent(new[] { 0 }, state.nullIndices);
            Assert.IsTrue(state.tooltips.ContainsKey(0));
            StringAssert.Contains("Null entry", state.summary);
        }

        [Test]
        public void DuplicateStateHandlesDistinctStrings()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            ISerializableSetInspector inspector = host.set;
            Array values = Array.CreateInstance(inspector.ElementType, 2);
            values.SetValue("999", 0);
            values.SetValue("ddd", 1);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.DuplicateState state = drawer.EvaluateDuplicateState(
                setProperty,
                itemsProperty,
                force: true
            );

            Assert.IsFalse(state.hasDuplicates);
            CollectionAssert.IsEmpty(state.duplicateIndices);
            Assert.IsTrue(string.IsNullOrEmpty(state.summary));
        }

        [Test]
        public void SortedSetManualReorderShowsSortButton()
        {
            SortedSetHost host = CreateScriptableObject<SortedSetHost>();
            host.set.Add(1);
            host.set.Add(2);
            host.set.Add(3);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedSetHost.set)
            );
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            itemsProperty.MoveArrayElement(0, 2);
            serializedObject.ApplyModifiedProperties();

            bool beforeDeserialize = SerializableSetPropertyDrawer.ShouldShowSortButton(
                SerializableSetPropertyDrawer.IsSortedSet(setProperty),
                typeof(int),
                itemsProperty
            );
            Assert.IsTrue(
                beforeDeserialize,
                $"Sort button should be visible before the sorted set rehydrates. Items: {DumpIntArray(itemsProperty)}"
            );

            host.set.OnAfterDeserialize();
            serializedObject.Update();
            setProperty = serializedObject.FindProperty(nameof(SortedSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            bool showSort = SerializableSetPropertyDrawer.ShouldShowSortButton(
                SerializableSetPropertyDrawer.IsSortedSet(setProperty),
                typeof(int),
                itemsProperty
            );

            Assert.IsFalse(
                showSort,
                $"Sorted sets reorder entries immediately. Items: {DumpIntArray(itemsProperty)}"
            );
            Assert.IsFalse(
                host.set.PreserveSerializedEntries,
                "Sorted set should clear PreserveSerializedEntries once it reorders elements automatically."
            );
        }

        [Test]
        public void SetSortButtonVisibilityReflectsOrdering()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            ISerializableSetInspector inspector = host.set;
            Array values = Array.CreateInstance(inspector.ElementType, 2);
            values.SetValue(5, 0);
            values.SetValue(1, 1);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            bool showBefore = SerializableSetPropertyDrawer.ShouldShowSortButton(
                SerializableSetPropertyDrawer.IsSortedSet(setProperty),
                inspector.ElementType,
                itemsProperty
            );
            Assert.IsTrue(showBefore);

            values.SetValue(1, 0);
            values.SetValue(5, 1);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();
            serializedObject.Update();
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            bool showAfter = SerializableSetPropertyDrawer.ShouldShowSortButton(
                SerializableSetPropertyDrawer.IsSortedSet(setProperty),
                inspector.ElementType,
                itemsProperty
            );
            Assert.IsFalse(showAfter);
        }

        [Test]
        public void PageEntriesNeedSortingOnlyFlagsVisiblePage()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            ISerializableSetInspector inspector = host.set;
            Array values = Array.CreateInstance(typeof(int), 4);
            values.SetValue(1, 0);
            values.SetValue(2, 1);
            values.SetValue(4, 2);
            values.SetValue(3, 3);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PaginationState pagination =
                drawer.GetOrCreatePaginationState(setProperty);
            pagination.pageSize = 2;

            string listKey = SerializableSetPropertyDrawer.GetListKey(setProperty);
            SerializableSetPropertyDrawer.ListPageCache cache = drawer.EnsurePageCache(
                listKey,
                itemsProperty,
                pagination
            );

            bool firstPageNeedsSorting = SerializableSetPropertyDrawer.PageEntriesNeedSorting(
                cache,
                itemsProperty,
                allowSort: true
            );
            Assert.IsFalse(
                firstPageNeedsSorting,
                $"First page should already be sorted. Indexes: {DumpPageEntries(cache)} Values: {DumpIntArray(itemsProperty)}"
            );

            pagination.page = 1;
            cache = drawer.EnsurePageCache(listKey, itemsProperty, pagination);
            bool secondPageNeedsSorting = SerializableSetPropertyDrawer.PageEntriesNeedSorting(
                cache,
                itemsProperty,
                allowSort: true
            );
            Assert.IsTrue(
                secondPageNeedsSorting,
                $"Second page should require sorting. Indexes: {DumpPageEntries(cache)} Values: {DumpIntArray(itemsProperty)}"
            );
        }

        [Test]
        public void ReorderableListReordersHashSetEntries()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            ISerializableSetInspector inspector = host.set;
            Array values = Array.CreateInstance(inspector.ElementType, 3);
            values.SetValue(1, 0);
            values.SetValue(2, 1);
            values.SetValue(3, 2);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));

            SerializableSetPropertyDrawer drawer = new();
            ReorderableList list = drawer.GetOrCreateList(setProperty);

            Assert.IsNotNull(list.onReorderCallbackWithDetails, "Expected reorder callback.");
            SimulateReorderableListMove(list, 0, 2);
            list.onReorderCallbackWithDetails.Invoke(list, 0, 2);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(3, itemsProperty.arraySize);
            Assert.AreEqual(2, itemsProperty.GetArrayElementAtIndex(0).intValue);
            Assert.AreEqual(3, itemsProperty.GetArrayElementAtIndex(1).intValue);
            Assert.AreEqual(1, itemsProperty.GetArrayElementAtIndex(2).intValue);
        }

        [Test]
        public void ReorderableListReordersSortedSetEntries()
        {
            SortedSetHost host = CreateScriptableObject<SortedSetHost>();
            ISerializableSetInspector inspector = host.set;
            Array values = Array.CreateInstance(inspector.ElementType, 3);
            values.SetValue(10, 0);
            values.SetValue(20, 1);
            values.SetValue(30, 2);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(SortedSetHost.set)
            );

            SerializableSetPropertyDrawer drawer = new();
            ReorderableList list = drawer.GetOrCreateList(setProperty);

            Assert.IsNotNull(list.onReorderCallbackWithDetails, "Expected reorder callback.");
            SimulateReorderableListMove(list, 2, 0);
            list.onReorderCallbackWithDetails.Invoke(list, 2, 0);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            Assert.AreEqual(3, itemsProperty.arraySize);
            Assert.AreEqual(30, itemsProperty.GetArrayElementAtIndex(0).intValue);
            Assert.AreEqual(10, itemsProperty.GetArrayElementAtIndex(1).intValue);
            Assert.AreEqual(20, itemsProperty.GetArrayElementAtIndex(2).intValue);
        }

        private static void SimulateReorderableListMove(
            ReorderableList list,
            int oldIndex,
            int newIndex
        )
        {
            if (list?.list is not IList backing || backing.Count == 0)
            {
                return;
            }

            if (oldIndex < 0 || oldIndex >= backing.Count)
            {
                return;
            }

            object element = backing[oldIndex];
            backing.RemoveAt(oldIndex);

            int clampedIndex = Mathf.Clamp(newIndex, 0, backing.Count);
            if (clampedIndex >= backing.Count)
            {
                backing.Add(element);
            }
            else
            {
                backing.Insert(clampedIndex, element);
            }
        }

        [Test]
        public void EditingStringSetEntryAffectsOnlyTarget()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            ISerializableSetInspector inspector = host.set;
            Array values = Array.CreateInstance(inspector.ElementType, 3);
            values.SetValue("first", 0);
            values.SetValue("second", 1);
            values.SetValue("third", 2);
            inspector.SetSerializedItemsSnapshot(values, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            SerializedProperty itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            itemsProperty.GetArrayElementAtIndex(2).stringValue = "updated";
            Assert.IsTrue(serializedObject.ApplyModifiedProperties());

            SerializableSetPropertyDrawer.SyncRuntimeSet(setProperty);
            serializedObject.Update();
            setProperty = serializedObject.FindProperty(nameof(StringSetHost.set));
            itemsProperty = setProperty.FindPropertyRelative(
                SerializableHashSetSerializedPropertyNames.Items
            );

            string[] snapshot = new string[itemsProperty.arraySize];
            for (int index = 0; index < snapshot.Length; index++)
            {
                snapshot[index] = itemsProperty.GetArrayElementAtIndex(index).stringValue;
            }

            CollectionAssert.AreEqual(new[] { "first", "second", "updated" }, snapshot);
        }

        [Test]
        public void SortedSetEditingPreservesSerializedEntry()
        {
            SortedStringSetHost host = CreateScriptableObject<SortedStringSetHost>();
            ISerializableSetInspector inspector = host.set;
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

            SerializableSetPropertyDrawer drawer = new();
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

            SerializableSetPropertyDrawer drawer = new();
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

            SerializableSetPropertyDrawer drawer = new();
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
        public void IsSortedSetRecognizesDerivedTypes()
        {
            DerivedSortedSetHost host = CreateScriptableObject<DerivedSortedSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(DerivedSortedSetHost.set)
            );

            bool result = SerializableSetPropertyDrawer.IsSortedSet(setProperty);
            Assert.IsTrue(
                result,
                "Derived SerializableSortedSet types should be detected for sorting."
            );
        }

        [Test]
        public void HashSetStringEditingRetainsEntryWhenAddedThroughDrawer()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializableSetPropertyDrawer drawer = new();
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

            SerializableSetPropertyDrawer drawer = new();
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
            Rect baseRect = new(5f, 10f, 25f, 16f);
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

            SerializableSetPropertyDrawer drawer = new();
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

            SerializableSetPropertyDrawer drawer = new();
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
            ISerializableSetInspector inspector = host.set;
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

            SerializableSetPropertyDrawer drawer = new();
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
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, duplicateState.duplicateIndices);
        }

        [Test]
        public void TryAddNewElementPreservesSerializedDuplicatesForSortedSet()
        {
            SortedSetHost host = CreateScriptableObject<SortedSetHost>();
            ISerializableSetInspector inspector = host.set;
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

            SerializableSetPropertyDrawer drawer = new();
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
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, duplicateState.duplicateIndices);
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

            SerializableSetPropertyDrawer drawer = new();
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

            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, serialized);
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, host.set.ToArray());
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
        public void RemoveEntryShrinksSerializedArray()
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

            SerializableSetPropertyDrawer drawer = new();
            int removedValue = itemsProperty.GetArrayElementAtIndex(1).intValue;
            SerializableSetPropertyDrawer.RemoveEntry(itemsProperty, 1);
            drawer.RemoveValueFromSet(setProperty, setProperty.propertyPath, removedValue);
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
            CollectionAssert.AreEquivalent(new[] { 10, 30 }, remaining);
            Assert.AreEqual(2, host.set.Count);
        }

        [UnityTest]
        public IEnumerator HonorsGroupPaddingWithinGroups()
        {
            HashSetHost host = CreateScriptableObject<HashSetHost>();
            host.set.Add(10);
            host.set.Add(20);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(nameof(HashSetHost.set));
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 360f, 400f);
            GUIContent label = new("Set");
            const int IndentDepth = 1;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                setProperty.isExpanded = true;
                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = IndentDepth;
                try
                {
                    drawer.OnGUI(controlRect, setProperty, label);
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndent;
                }
            });

            Assert.IsTrue(
                drawer.HasItemsContainerRect,
                "Baseline draw should capture the rendered container."
            );

            int snapshotIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = IndentDepth;
            Rect expectedBaselineRect = EditorGUI.IndentedRect(controlRect);
            EditorGUI.indentLevel = snapshotIndent;
            Assert.That(
                drawer.LastResolvedPosition.xMin,
                Is.EqualTo(expectedBaselineRect.xMin).Within(0.0001f),
                "Indentation should influence the resolved content rectangle."
            );

            const float LeftPadding = 16f;
            const float RightPadding = 8f;
            const float HorizontalPadding = LeftPadding + RightPadding;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                setProperty.isExpanded = true;
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
                        drawer.OnGUI(controlRect, setProperty, label);
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
                "Group padding should offset the rendered content."
            );
            Assert.That(
                paddedRect.width,
                Is.EqualTo(Mathf.Max(0f, expectedBaselineRect.width - HorizontalPadding))
                    .Within(0.0001f),
                "Group padding should reduce usable width."
            );
            Assert.IsTrue(drawer.HasItemsContainerRect, "Container rect should be recorded.");
            Assert.That(
                drawer.LastItemsContainerRect.xMin,
                Is.EqualTo(paddedRect.xMin).Within(0.0001f),
                "Container rect should align with the padded content."
            );
        }

        [UnityTest]
        public IEnumerator ManualEntryHeaderHonorsGroupPadding()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PendingEntry pending = drawer.GetOrCreatePendingEntry(
                setProperty,
                setProperty.propertyPath,
                typeof(string),
                isSortedSet: false
            );
            pending.isExpanded = true;

            Rect controlRect = new(0f, 0f, 360f, 420f);
            GUIContent label = new("Set");

            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
            bool baselineSetExpandedBeforeDraw = setProperty.isExpanded;
            bool baselinePendingExpandedBeforeDraw = pending.isExpanded;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                pending.isExpanded = true;
                drawer.OnGUI(controlRect, setProperty, label);
            });

            serializedObject.Update();
            bool baselineSetExpandedAfterDraw = setProperty.isExpanded;
            TestContext.WriteLine(
                $"ManualEntryHeader baseline foldout states -> set: {baselineSetExpandedBeforeDraw}->{baselineSetExpandedAfterDraw}, pending: {baselinePendingExpandedBeforeDraw}->{pending.isExpanded}"
            );

            Assert.IsTrue(
                SerializableSetPropertyDrawer.HasLastManualEntryHeaderRect,
                "Baseline draw should capture the manual entry header layout."
            );

            Rect baselineHeader = SerializableSetPropertyDrawer.LastManualEntryHeaderRect;

            const float LeftPadding = 20f;
            const float RightPadding = 12f;
            float horizontalPadding = LeftPadding + RightPadding;

            bool groupedSetExpandedBeforeDraw = setProperty.isExpanded;
            bool groupedPendingExpandedBeforeDraw = pending.isExpanded;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
                    pending.isExpanded = true;
                    drawer.OnGUI(controlRect, setProperty, label);
                }
            });

            serializedObject.Update();
            bool groupedSetExpandedAfterDraw = setProperty.isExpanded;
            TestContext.WriteLine(
                $"ManualEntryHeader grouped foldout states -> set: {groupedSetExpandedBeforeDraw}->{groupedSetExpandedAfterDraw}, pending: {groupedPendingExpandedBeforeDraw}->{pending.isExpanded}"
            );

            Assert.IsTrue(
                SerializableSetPropertyDrawer.HasLastManualEntryHeaderRect,
                "Grouped draw should capture the manual entry header layout."
            );

            Rect groupedHeader = SerializableSetPropertyDrawer.LastManualEntryHeaderRect;

            Assert.That(
                groupedHeader.xMin,
                Is.EqualTo(baselineHeader.xMin + LeftPadding).Within(0.0001f),
                "Manual entry header should shift by the applied left padding."
            );
            Assert.That(
                groupedHeader.width,
                Is.EqualTo(Mathf.Max(0f, baselineHeader.width - horizontalPadding)).Within(0.0001f),
                "Manual entry header width should shrink by the total padding."
            );
        }

        [UnityTest]
        public IEnumerator ManualEntryValueFieldHonorsGroupPadding()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            SerializableSetPropertyDrawer.PendingEntry pending = drawer.GetOrCreatePendingEntry(
                setProperty,
                setProperty.propertyPath,
                typeof(string),
                isSortedSet: false
            );
            pending.isExpanded = true;

            Rect controlRect = new(0f, 0f, 360f, 420f);
            GUIContent label = new("Set");

            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
            bool baselineSetExpandedBeforeDraw = setProperty.isExpanded;
            bool baselinePendingExpandedBeforeDraw = pending.isExpanded;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                pending.isExpanded = true;
                drawer.OnGUI(controlRect, setProperty, label);
            });

            serializedObject.Update();
            bool baselineSetExpandedAfterDraw = setProperty.isExpanded;
            TestContext.WriteLine(
                $"ManualEntryValue baseline foldout states -> set: {baselineSetExpandedBeforeDraw}->{baselineSetExpandedAfterDraw}, pending: {baselinePendingExpandedBeforeDraw}->{pending.isExpanded}"
            );

            Assert.IsTrue(
                SerializableSetPropertyDrawer.HasLastManualEntryValueRect,
                "Baseline draw should capture the manual entry value layout."
            );

            Rect baselineValue = SerializableSetPropertyDrawer.LastManualEntryValueRect;

            const float LeftPadding = 18f;
            const float RightPadding = 14f;
            float horizontalPadding = LeftPadding + RightPadding;

            bool groupedSetExpandedBeforeDraw = setProperty.isExpanded;
            bool groupedPendingExpandedBeforeDraw = pending.isExpanded;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
                    pending.isExpanded = true;
                    drawer.OnGUI(controlRect, setProperty, label);
                }
            });

            serializedObject.Update();
            bool groupedSetExpandedAfterDraw = setProperty.isExpanded;
            TestContext.WriteLine(
                $"ManualEntryValue grouped foldout states -> set: {groupedSetExpandedBeforeDraw}->{groupedSetExpandedAfterDraw}, pending: {groupedPendingExpandedBeforeDraw}->{pending.isExpanded}"
            );

            Assert.IsTrue(
                SerializableSetPropertyDrawer.HasLastManualEntryValueRect,
                "Grouped draw should capture the manual entry value layout."
            );

            Rect groupedValue = SerializableSetPropertyDrawer.LastManualEntryValueRect;

            Assert.That(
                groupedValue.xMin,
                Is.EqualTo(baselineValue.xMin + LeftPadding).Within(0.0001f),
                "Manual entry value field should shift by the applied left padding."
            );
            Assert.That(
                groupedValue.width,
                Is.EqualTo(Mathf.Max(0f, baselineValue.width - horizontalPadding)).Within(0.0001f),
                "Manual entry value width should shrink by the total padding."
            );
        }

        [UnityTest]
        public IEnumerator ManualEntryFoldoutRespectsExplicitExpansionBeforeInitialization()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 360f, 420f);
            GUIContent label = new("Set");

            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
            bool setExpandedBeforeDraw = setProperty.isExpanded;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, setProperty, label);
            });

            serializedObject.Update();
            bool setExpandedAfterDraw = setProperty.isExpanded;
            TestContext.WriteLine(
                $"ManualEntry explicit expansion state -> {setExpandedBeforeDraw}->{setExpandedAfterDraw}"
            );

            Assert.IsTrue(
                setExpandedAfterDraw,
                "Set foldout should remain expanded when explicitly set before the first draw."
            );
            Assert.IsTrue(
                SerializableSetPropertyDrawer.HasLastManualEntryHeaderRect,
                "Manual entry header should render when the foldout remains expanded."
            );
        }

        [UnityTest]
        public IEnumerator RowContentHonorsGroupPadding()
        {
            ComplexSetHost host = CreateScriptableObject<ComplexSetHost>();
            ISerializableSetInspector inspector = host.set;
            Array snapshot = Array.CreateInstance(typeof(ComplexSetElement), 1);
            snapshot.SetValue(
                new ComplexSetElement
                {
                    primary = Color.yellow,
                    nested = new NestedComplexElement
                    {
                        intensity = 2.5f,
                        offset = new Vector2(3f, 1f),
                    },
                },
                0
            );
            inspector.SetSerializedItemsSnapshot(snapshot, preserveSerializedEntries: true);
            inspector.SynchronizeSerializedState();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(ComplexSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 420f, 520f);
            GUIContent label = new("Set");

            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
            bool baselineSetExpandedBeforeDraw = setProperty.isExpanded;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, setProperty, label);
            });

            serializedObject.Update();
            bool baselineSetExpandedAfterDraw = setProperty.isExpanded;
            TestContext.WriteLine(
                $"RowContent baseline set foldout state -> {baselineSetExpandedBeforeDraw}->{baselineSetExpandedAfterDraw}"
            );

            Assert.IsTrue(
                SerializableSetPropertyDrawer.HasLastRowContentRect,
                "Baseline draw should capture the row content layout."
            );

            Rect baselineRow = SerializableSetPropertyDrawer.LastRowContentRect;

            const float LeftPadding = 24f;
            const float RightPadding = 10f;
            float horizontalPadding = LeftPadding + RightPadding;

            bool groupedSetExpandedBeforeDraw = setProperty.isExpanded;

            yield return TestIMGUIExecutor.Run(() =>
            {
                setProperty.serializedObject.UpdateIfRequiredOrScript();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
                    drawer.OnGUI(controlRect, setProperty, label);
                }
            });

            serializedObject.Update();
            bool groupedSetExpandedAfterDraw = setProperty.isExpanded;
            TestContext.WriteLine(
                $"RowContent grouped set foldout state -> {groupedSetExpandedBeforeDraw}->{groupedSetExpandedAfterDraw}"
            );

            Assert.IsTrue(
                SerializableSetPropertyDrawer.HasLastRowContentRect,
                "Grouped draw should capture the row content layout."
            );

            Rect groupedRow = SerializableSetPropertyDrawer.LastRowContentRect;

            Assert.That(
                groupedRow.xMin,
                Is.EqualTo(baselineRow.xMin + LeftPadding).Within(0.0001f),
                "Row content should shift by the configured left padding."
            );
            Assert.That(
                groupedRow.width,
                Is.EqualTo(Mathf.Max(0f, baselineRow.width - horizontalPadding)).Within(0.0001f),
                "Row content width should shrink by the total padding."
            );
        }

        [UnityTest]
        public IEnumerator SetDrawerDoesNotResetUnappliedScalarChanges()
        {
            MixedFieldsSetHost host = Track(ScriptableObject.CreateInstance<MixedFieldsSetHost>());
            SerializedObject serializedHost = TrackDisposable(new SerializedObject(host));
            serializedHost.Update();

            SerializedProperty scalarProperty = serializedHost.FindProperty(
                nameof(MixedFieldsSetHost.scalarValue)
            );
            SerializedProperty setProperty = serializedHost.FindProperty(
                nameof(MixedFieldsSetHost.set)
            );

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            Rect controlRect = new Rect(0f, 0f, 320f, 280f);
            GUIContent label = new GUIContent("Set");

            drawer.GetPropertyHeight(setProperty, label);

            scalarProperty.intValue = 4242;

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(controlRect, setProperty, label);
            });

            serializedHost.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(host.scalarValue, Is.EqualTo(4242));
        }

        [UnityTest]
        public IEnumerator SetDrawerMultiObjectEditKeepsScalarChanges()
        {
            MixedFieldsSetHost first = Track(ScriptableObject.CreateInstance<MixedFieldsSetHost>());
            MixedFieldsSetHost second = Track(
                ScriptableObject.CreateInstance<MixedFieldsSetHost>()
            );
            SerializedObject serializedHosts = TrackDisposable(
                new SerializedObject(new Object[] { first, second })
            );
            serializedHosts.Update();

            SerializedProperty scalarProperty = serializedHosts.FindProperty(
                nameof(MixedFieldsSetHost.scalarValue)
            );
            SerializedProperty setProperty = serializedHosts.FindProperty(
                nameof(MixedFieldsSetHost.set)
            );

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            Rect controlRect = new Rect(0f, 0f, 320f, 280f);
            GUIContent label = new GUIContent("Set");

            drawer.GetPropertyHeight(setProperty, label);

            scalarProperty.intValue = 5150;

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(controlRect, setProperty, label);
            });

            serializedHosts.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(
                first.scalarValue,
                Is.EqualTo(5150),
                "First target should retain scalar edit."
            );
            Assert.That(
                second.scalarValue,
                Is.EqualTo(5150),
                "Second target should retain scalar edit."
            );
        }

        [UnityTest]
        public IEnumerator SetDrawerDoesNotResetTrailingScalarField()
        {
            SetScalarAfterHost host = Track(ScriptableObject.CreateInstance<SetScalarAfterHost>());
            SerializedObject serializedHost = TrackDisposable(new SerializedObject(host));
            serializedHost.Update();

            SerializedProperty setProperty = serializedHost.FindProperty(
                nameof(SetScalarAfterHost.set)
            );
            SerializedProperty trailingScalar = serializedHost.FindProperty(
                nameof(SetScalarAfterHost.trailingScalar)
            );

            SerializableSetPropertyDrawer drawer = new SerializableSetPropertyDrawer();
            Rect controlRect = new Rect(0f, 0f, 320f, 280f);
            GUIContent label = new GUIContent("Set");

            drawer.GetPropertyHeight(setProperty, label);

            trailingScalar.intValue = 777;

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(controlRect, setProperty, label);
            });

            serializedHost.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(host.trailingScalar, Is.EqualTo(777));
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

        private static string DumpPageEntries(SerializableSetPropertyDrawer.ListPageCache cache)
        {
            if (cache?.entries == null || cache.entries.Count == 0)
            {
                return "[]";
            }

            List<int> indices = new(cache.entries.Count);
            for (int i = 0; i < cache.entries.Count; i++)
            {
                indices.Add(cache.entries[i]?.arrayIndex ?? -1);
            }

            return $"[{string.Join(", ", indices)}]";
        }
    }
}
