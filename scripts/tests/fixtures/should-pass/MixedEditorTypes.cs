// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEditor;
using UnityEngine;

// This file should PASS: mixing Editor and PropertyDrawer is allowed
[CustomEditor(typeof(SomeComponent))]
public class MixedEditorTypesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

[CustomPropertyDrawer(typeof(SomeAttribute))]
public class MixedEditorTypesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) { }
}

// Mock types
public class SomeComponent { }

public class SomeAttribute : PropertyAttribute { }
