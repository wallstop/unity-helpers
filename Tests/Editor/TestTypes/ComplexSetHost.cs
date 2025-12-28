// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class ComplexSetHost : ScriptableObject
    {
        public SerializableHashSet<ComplexSetElement> set = new();
    }

    [Serializable]
    internal sealed class ComplexSetElement
    {
        // ReSharper disable once NotAccessedField.Local
        public Color primary = Color.cyan;

        // ReSharper disable once NotAccessedField.Local
        public NestedComplexElement nested = new();
    }

    [Serializable]
    internal sealed class NestedComplexElement
    {
        // ReSharper disable once NotAccessedField.Local
        public float intensity = 1.25f;

        // ReSharper disable once NotAccessedField.Local
        public Vector2 offset = new(0.5f, -0.5f);
    }
}
