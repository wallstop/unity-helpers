// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEngine;

// This file should FAIL: contains two MonoBehaviour classes
public class TwoMonoBehavioursA : MonoBehaviour
{
    public int value;
}

public class TwoMonoBehavioursB : MonoBehaviour
{
    public string name;
}
