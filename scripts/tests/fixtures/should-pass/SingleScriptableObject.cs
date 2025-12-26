using UnityEngine;

// This file should PASS: single ScriptableObject with matching filename
[CreateAssetMenu(fileName = "SingleScriptableObject", menuName = "Test/Single Scriptable Object")]
public class SingleScriptableObject : ScriptableObject
{
    public int value;
    public string description;
}
