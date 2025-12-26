using UnityEngine;

// This file should FAIL: contains a MonoBehaviour and a ScriptableObject
public class MixedUnityTypesA : MonoBehaviour
{
    public int value;
}

public class MixedUnityTypesB : ScriptableObject
{
    public string name;
}
