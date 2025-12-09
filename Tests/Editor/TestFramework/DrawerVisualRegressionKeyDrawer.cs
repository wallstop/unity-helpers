namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(DrawerVisualRegressionKey))]
    internal sealed class DrawerVisualRegressionKeyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawerVisualRecorder.Record(DrawerVisualRole.DictionaryKey, property, position);
            SerializedProperty idProperty = property?.FindPropertyRelative(
                nameof(DrawerVisualRegressionKey.id)
            );
            if (idProperty != null)
            {
                EditorGUI.PropertyField(position, idProperty, GUIContent.none);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty idProperty = property?.FindPropertyRelative(
                nameof(DrawerVisualRegressionKey.id)
            );
            return idProperty != null
                ? EditorGUI.GetPropertyHeight(idProperty, GUIContent.none, true)
                : EditorGUIUtility.singleLineHeight;
        }
    }
}
