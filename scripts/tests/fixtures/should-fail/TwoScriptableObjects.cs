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
