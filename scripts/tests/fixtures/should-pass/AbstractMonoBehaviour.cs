// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEngine;

// This file should PASS: abstract base classes are typically in their own file
// and having helper classes alongside is a common pattern for inheritance hierarchies
public abstract class AbstractMonoBehaviour : MonoBehaviour
{
    public abstract void DoSomething();

    protected virtual void OnValidate() { }
}

// Supporting interface for the abstract base - this is fine
public interface IAbstractMonoBehaviourHelper
{
    void HelperMethod();
}
