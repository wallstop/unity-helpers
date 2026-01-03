// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEngine;

// This file should FAIL: contains two ScriptableObjectSingleton types
public class TwoSingletonScriptableObjectsA
    : ScriptableObjectSingleton<TwoSingletonScriptableObjectsA>
{
    public int value;
}

public class TwoSingletonScriptableObjectsB : ScriptableObject
{
    public string name;
}
