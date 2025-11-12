namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Buffers.Binary;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class WGuidPropertyDrawerTests : CommonTestBase
    {
        [SetUp]
        public void SetUpDrawerTests()
        {
            WGuidPropertyDrawer.ClearCachedStates();
        }

        [TearDown]
        public void TearDownDrawerTests()
        {
            WGuidPropertyDrawer.ClearCachedStates();
        }

        [Test]
        public void HandleTextChangeClearsGuidWhenInputEmpty()
        {
            GuidContainer container = CreateScriptableObject<GuidContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(nameof(GuidContainer.guid));
            Assert.NotNull(property);
            SerializedProperty lowProperty = property.FindPropertyRelative(WGuid.LowFieldName);
            SerializedProperty highProperty = property.FindPropertyRelative(WGuid.HighFieldName);
            WGuidPropertyDrawer.DrawerState state = WGuidPropertyDrawer.GetState(property);

            WGuidPropertyDrawer.GenerateNewGuid(property, lowProperty, highProperty, state);
            serializedObject.Update();
            Assert.IsFalse(ReadGuid(property).IsEmpty);

            WGuidPropertyDrawer.HandleTextChange(
                property,
                lowProperty,
                highProperty,
                state,
                string.Empty
            );
            serializedObject.Update();

            Assert.IsTrue(ReadGuid(property).IsEmpty);
            Assert.IsFalse(state.hasPendingInvalid);
            Assert.IsEmpty(state.warningMessage);
        }

        [Test]
        public void HandleTextChangeAppliesValidGuid()
        {
            GuidContainer container = CreateScriptableObject<GuidContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(nameof(GuidContainer.guid));
            SerializedProperty lowProperty = property.FindPropertyRelative(WGuid.LowFieldName);
            SerializedProperty highProperty = property.FindPropertyRelative(WGuid.HighFieldName);
            WGuidPropertyDrawer.DrawerState state = WGuidPropertyDrawer.GetState(property);

            Guid expected = Guid.NewGuid();
            WGuidPropertyDrawer.HandleTextChange(
                property,
                lowProperty,
                highProperty,
                state,
                expected.ToString("D")
            );
            serializedObject.Update();

            WGuid actual = ReadGuid(property);
            Assert.AreEqual(expected, actual.ToGuid());
            Assert.IsFalse(state.hasPendingInvalid);
            Assert.IsEmpty(state.warningMessage);
        }

        [Test]
        public void HandleTextChangeRejectsInvalidGuid()
        {
            GuidContainer container = CreateScriptableObject<GuidContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(nameof(GuidContainer.guid));
            SerializedProperty lowProperty = property.FindPropertyRelative(WGuid.LowFieldName);
            SerializedProperty highProperty = property.FindPropertyRelative(WGuid.HighFieldName);
            WGuidPropertyDrawer.DrawerState state = WGuidPropertyDrawer.GetState(property);

            WGuidPropertyDrawer.GenerateNewGuid(property, lowProperty, highProperty, state);
            serializedObject.Update();
            long originalLow = lowProperty.longValue;
            long originalHigh = highProperty.longValue;

            WGuidPropertyDrawer.HandleTextChange(
                property,
                lowProperty,
                highProperty,
                state,
                "invalid-guid"
            );
            serializedObject.Update();

            Assert.AreEqual(originalLow, lowProperty.longValue);
            Assert.AreEqual(originalHigh, highProperty.longValue);
            Assert.IsTrue(state.hasPendingInvalid);
            Assert.AreEqual($"Enter a valid {nameof(Guid)} string.", state.warningMessage);
        }

        [Test]
        public void HandleTextChangeRejectsNonVersionFourGuid()
        {
            GuidContainer container = CreateScriptableObject<GuidContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(nameof(GuidContainer.guid));
            SerializedProperty lowProperty = property.FindPropertyRelative(WGuid.LowFieldName);
            SerializedProperty highProperty = property.FindPropertyRelative(WGuid.HighFieldName);
            WGuidPropertyDrawer.DrawerState state = WGuidPropertyDrawer.GetState(property);

            WGuidPropertyDrawer.HandleTextChange(
                property,
                lowProperty,
                highProperty,
                state,
                "00000000-0000-1000-8000-000000000000"
            );
            serializedObject.Update();

            Assert.IsTrue(state.hasPendingInvalid);
            Assert.AreEqual(
                $"{nameof(WGuid)} expects a version 4 {nameof(Guid)}.",
                state.warningMessage
            );
            Assert.IsTrue(ReadGuid(property).IsEmpty);
        }

        [Test]
        public void GenerateNewGuidProducesVersionFourValue()
        {
            GuidContainer container = CreateScriptableObject<GuidContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(nameof(GuidContainer.guid));
            SerializedProperty lowProperty = property.FindPropertyRelative(WGuid.LowFieldName);
            SerializedProperty highProperty = property.FindPropertyRelative(WGuid.HighFieldName);
            WGuidPropertyDrawer.DrawerState state = WGuidPropertyDrawer.GetState(property);

            WGuidPropertyDrawer.GenerateNewGuid(property, lowProperty, highProperty, state);
            serializedObject.Update();

            WGuid result = ReadGuid(property);
            Assert.IsFalse(result.IsEmpty);
            Assert.IsTrue(result.IsVersion4);
            Assert.IsFalse(state.hasPendingInvalid);
            Assert.IsEmpty(state.warningMessage);
        }

        [Test]
        public void GetPropertyHeightIncludesWarningSpaceWhenInvalid()
        {
            GuidContainer container = CreateScriptableObject<GuidContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(nameof(GuidContainer.guid));
            SerializedProperty lowProperty = property.FindPropertyRelative(WGuid.LowFieldName);
            SerializedProperty highProperty = property.FindPropertyRelative(WGuid.HighFieldName);
            WGuidPropertyDrawer.DrawerState state = WGuidPropertyDrawer.GetState(property);

            WGuidPropertyDrawer.HandleTextChange(
                property,
                lowProperty,
                highProperty,
                state,
                "invalid-guid"
            );
            serializedObject.Update();

            WGuidPropertyDrawer drawer = new();
            float height = drawer.GetPropertyHeight(property, GUIContent.none);
            Assert.Greater(height, EditorGUIUtility.singleLineHeight);
        }

        private static WGuid ReadGuid(SerializedProperty property)
        {
            SerializedProperty lowProperty = property.FindPropertyRelative(WGuid.LowFieldName);
            SerializedProperty highProperty = property.FindPropertyRelative(WGuid.HighFieldName);
            Span<byte> buffer = stackalloc byte[16];
            BinaryPrimitives.WriteUInt64LittleEndian(
                buffer.Slice(0, 8),
                unchecked((ulong)lowProperty.longValue)
            );
            BinaryPrimitives.WriteUInt64LittleEndian(
                buffer.Slice(8, 8),
                unchecked((ulong)highProperty.longValue)
            );
            Guid guid = new(buffer);
            return new WGuid(guid);
        }

        private sealed class GuidContainer : ScriptableObject
        {
            public WGuid guid;
        }
    }
}
