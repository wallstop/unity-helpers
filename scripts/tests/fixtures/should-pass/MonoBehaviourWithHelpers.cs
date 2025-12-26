using UnityEngine;

// This file should PASS: MonoBehaviour with regular helper classes is allowed
// The helper classes don't inherit from Unity types
public class MonoBehaviourWithHelpers : MonoBehaviour
{
    public HelperData data;

    private void Start()
    {
        HelperUtility.DoSomething(data);
    }
}

// Regular C# class - doesn't count as Unity type
[System.Serializable]
public class HelperData
{
    public string name;
    public int value;
}

// Static helper - doesn't count as Unity type
public static class HelperUtility
{
    public static void DoSomething(HelperData data) { }
}
