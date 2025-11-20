namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using NUnit.Framework;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.Utils;
    using PropertyAttribute = UnityEngine.PropertyAttribute;

    [TestFixture]
    public sealed class IntDropdownDrawerTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator OnGUIClampsValuesOutsideConfiguredOptions()
        {
            IntDropdownTestAsset asset = CreateScriptableObject<IntDropdownTestAsset>();
            asset.missingValue = 999;

            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropdownTestAsset.missingValue)
            );
            Assert.IsNotNull(property, "Failed to locate missing value property.");

            IntDropdownDrawer drawer = new IntDropdownDrawer();
            AssignAttribute(drawer, new IntDropdownAttribute(5, 10, 15));
            Rect position = new Rect(0f, 0f, 240f, EditorGUIUtility.singleLineHeight);
            GUIContent label = new GUIContent("Missing");

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(position, property, label);
            });

            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.missingValue, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator OnGUILeavesValidSelectionUnchanged()
        {
            IntDropdownTestAsset asset = CreateScriptableObject<IntDropdownTestAsset>();
            asset.validValue = 10;

            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropdownTestAsset.validValue)
            );
            Assert.IsNotNull(property, "Failed to locate valid value property.");

            IntDropdownDrawer drawer = new IntDropdownDrawer();
            AssignAttribute(drawer, new IntDropdownAttribute(5, 10, 15));
            Rect position = new Rect(0f, 0f, 240f, EditorGUIUtility.singleLineHeight);
            GUIContent label = new GUIContent("Valid");

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(position, property, label);
            });

            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.validValue, Is.EqualTo(10));
        }

        [Serializable]
        private sealed class IntDropdownTestAsset : ScriptableObject
        {
            [IntDropdown(5, 10, 15)]
            public int missingValue = 5;

            [IntDropdown(5, 10, 15)]
            public int validValue = 10;
        }

        private static void AssignAttribute(PropertyDrawer drawer, PropertyAttribute attribute)
        {
            FieldInfo attributeField = typeof(PropertyDrawer).GetField(
                "m_Attribute",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(attributeField, "Unable to locate PropertyDrawer.m_Attribute.");
            attributeField.SetValue(drawer, attribute);
        }
    }
#endif
}
