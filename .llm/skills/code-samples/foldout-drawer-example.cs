// NOTE: This is a code snippet for reference. Place inside the proper namespace and #if UNITY_EDITOR wrapper:
// namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
// {
// #if UNITY_EDITOR
//     using UnityEditor;
//     using UnityEngine;
//     ... (your code here)
// #endif
// }

[CustomPropertyDrawer(typeof(MyComplexType))]
public sealed class MyComplexTypeDrawer : PropertyDrawer
{
    private const float LineHeight = 18f;
    private const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return LineHeight;
        }

        int lineCount = 1; // Foldout
        lineCount += 3; // Three child properties
        return lineCount * (LineHeight + Spacing);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        try
        {
            Rect foldoutRect = new(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float y = position.y + LineHeight + Spacing;
                DrawChildProperty(ref y, position.width, property, "childField1");
                DrawChildProperty(ref y, position.width, property, "childField2");
                DrawChildProperty(ref y, position.width, property, "childField3");

                EditorGUI.indentLevel--;
            }
        }
        finally
        {
            EditorGUI.EndProperty();
        }
    }

    private void DrawChildProperty(
        ref float y,
        float width,
        SerializedProperty parent,
        string childName
    )
    {
        SerializedProperty child = parent.FindPropertyRelative(childName);
        if (child != null)
        {
            Rect rect = new(0, y, width, LineHeight);
            rect = EditorGUI.IndentedRect(rect);
            EditorGUI.PropertyField(rect, child);
            y += LineHeight + Spacing;
        }
    }
}
