// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes;

    [TestFixture]
    public sealed class SerializableNullablePropertyDrawerTests : CommonTestBase
    {
        [Test]
        public void HeightCollapsesWhenEmpty()
        {
            NullableContainer container = CreateScriptableObject<NullableContainer>();
            using SerializedObject serializedObject = new(container);
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

            using SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(NullableContainer.integerValue)
            );
            Assert.NotNull(property);

            SerializableNullablePropertyDrawer drawer = new();
            float height = drawer.GetPropertyHeight(property, new GUIContent("integerValue"));

            Assert.AreEqual(EditorGUIUtility.singleLineHeight, height);
        }
    }
}
