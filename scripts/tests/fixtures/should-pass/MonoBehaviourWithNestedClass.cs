// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEngine;

// This file should PASS: nested classes are allowed
public class MonoBehaviourWithNestedClass : MonoBehaviour
{
    public int value;

    // Nested class - this should NOT trigger the multi-class check
    public class NestedData
    {
        public string name;
        public int count;
    }

    // Another nested class
    private class NestedHelper
    {
        public void DoSomething() { }
    }

    // Deeply nested class
    public class OuterNested
    {
        public class InnerNested
        {
            public int value;
        }
    }
}
