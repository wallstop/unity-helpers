using UnityEditor;
using UnityEngine;

// This file should PASS: EditorWindow classes don't have the one-class-per-file restriction
public class MultipleEditorWindowsA : EditorWindow
{
    [MenuItem("Tools/WindowA")]
    public static void ShowWindow()
    {
        GetWindow<MultipleEditorWindowsA>("Window A");
    }
}

public class MultipleEditorWindowsB : EditorWindow
{
    [MenuItem("Tools/WindowB")]
    public static void ShowWindow()
    {
        GetWindow<MultipleEditorWindowsB>("Window B");
    }
}
