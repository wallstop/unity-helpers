// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEngine;

// This file should PASS: single ScriptableObject with matching filename
[CreateAssetMenu(fileName = "SingleScriptableObject", menuName = "Test/Single Scriptable Object")]
public class SingleScriptableObject : ScriptableObject
{
    public int value;
    public string description;
}
