// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(DrawerVisualRegressionSetValue))]
    internal sealed class DrawerVisualRegressionSetValueDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawerVisualRecorder.Record(DrawerVisualRole.SetElement, property, position);
            DrawerVisualRegressionValueDrawerHelpers.DrawValue(position, property);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return DrawerVisualRegressionValueDrawerHelpers.GetValueHeight(property);
        }
    }
}
