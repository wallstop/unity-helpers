namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    [TestFixture]
    public sealed class SerializableNullablePropertyDrawerTests : CommonTestBase
    {
        [Test]
        public void HeightCollapsesWhenEmpty()
        {
            NullableContainer container = CreateScriptableObject<NullableContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(NullableContainer.integerValue)
            );
            Assert.NotNull(property);

            SerializableNullablePropertyDrawer drawer = new();
            float height = drawer.GetPropertyHeight(property, new GUIContent("integerValue"));

            Assert.AreEqual(EditorGUIUtility.singleLineHeight, height);
        }

        [Test]
        public void HeightStaysSingleLineWhenValuePresent()
        {
            NullableContainer container = CreateScriptableObject<NullableContainer>();
            container.integerValue.SetValue(10);

            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(NullableContainer.integerValue)
            );
            Assert.NotNull(property);

            SerializableNullablePropertyDrawer drawer = new();
            float height = drawer.GetPropertyHeight(property, new GUIContent("integerValue"));

            Assert.AreEqual(EditorGUIUtility.singleLineHeight, height);
        }

        private sealed class NullableContainer : ScriptableObject
        {
            public SerializableNullable<int> integerValue;
        }
    }
}
