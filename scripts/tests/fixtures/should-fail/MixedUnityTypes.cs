// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

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
