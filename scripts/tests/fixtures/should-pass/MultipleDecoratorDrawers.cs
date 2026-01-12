// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEditor;
using UnityEngine;

// This file should PASS: DecoratorDrawer classes don't have the one-class-per-file restriction
[CustomPropertyDrawer(typeof(HeaderAttribute))]
public class MultipleDecoratorDrawersA : DecoratorDrawer
{
    public override void OnGUI(Rect position) { }
}

[CustomPropertyDrawer(typeof(SpaceAttribute))]
public class MultipleDecoratorDrawersB : DecoratorDrawer
{
    public override void OnGUI(Rect position) { }
}
