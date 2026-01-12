// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEditor;
using UnityEngine;

// This file should PASS: Editor classes don't have the one-class-per-file restriction
[CustomEditor(typeof(SomeComponent))]
public class MultipleEditorsA : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(OtherComponent))]
public class MultipleEditorsB : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

// Mock types for editor targets
public class SomeComponent { }

public class OtherComponent { }
