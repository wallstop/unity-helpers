// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEngine;

// This file should FAIL: contains two ScriptableObject classes
public class TwoScriptableObjectsA : ScriptableObject
{
    public int value;
}

public class TwoScriptableObjectsB : ScriptableObject
{
    public string name;
}
