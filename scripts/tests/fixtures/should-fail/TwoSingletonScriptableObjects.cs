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
