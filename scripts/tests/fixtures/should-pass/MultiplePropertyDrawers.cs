using UnityEditor;
using UnityEngine;

// This file should PASS: PropertyDrawer classes don't have the one-class-per-file restriction
[CustomPropertyDrawer(typeof(SomeAttribute))]
public class MultiplePropertyDrawersA : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) { }
}

[CustomPropertyDrawer(typeof(OtherAttribute))]
public class MultiplePropertyDrawersB : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) { }
}

// Mock attribute types
public class SomeAttribute : PropertyAttribute { }

public class OtherAttribute : PropertyAttribute { }
