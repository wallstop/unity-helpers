namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    [Serializable]
    public sealed class IntStringDictionary : SerializableDictionary<int, string> { }

    [Serializable]
    public sealed class StringStringDictionary : SerializableDictionary<string, string> { }

    [Serializable]
    public sealed class StringComplexDictionary : SerializableDictionary<string, ComplexValue> { }

    [Serializable]
    public sealed class StringColorDataDictionary : SerializableDictionary<string, ColorData> { }

    [Serializable]
    public sealed class StringColorListDictionary
        : SerializableDictionary<string, ColorListData> { }

    [Serializable]
    public sealed class StringLabelStressDictionary
        : SerializableDictionary<string, LabelStressValue> { }

    [Serializable]
    public sealed class StringScriptableDictionary
        : SerializableDictionary<string, SampleScriptableObject> { }

    [Serializable]
    public sealed class PrivateComplexDictionary
        : SerializableDictionary<string, PrivateComplexValue> { }

    [Serializable]
    public sealed class GameObjectStringDictionary : SerializableDictionary<GameObject, string> { }

    [Serializable]
    public sealed class RectIntDictionary : SerializableDictionary<Rect, int> { }

    [Serializable]
    public sealed class PrivateCtorDictionary
        : SerializableDictionary<PrivateCtorKey, PrivateCtorValue> { }

    /// <summary>
    /// Complex value type with button and text color fields.
    /// </summary>
    [Serializable]
    public sealed class ComplexValue
    {
        public Color button;
        public Color text;
    }

    /// <summary>
    /// Complex value type with private backing fields.
    /// </summary>
    [Serializable]
    public sealed class PrivateComplexValue
    {
        [FormerlySerializedAs("primary")]
        [SerializeField]
        private Color _primary = Color.white;

        [FormerlySerializedAs("secondary")]
        [SerializeField]
        private Color _secondary = Color.black;

        public Color Primary
        {
            get => _primary;
            set => _primary = value;
        }

        public Color Secondary
        {
            get => _secondary;
            set => _secondary = value;
        }
    }

    /// <summary>
    /// Key type with private constructor for testing default value handling.
    /// </summary>
    [Serializable]
    public sealed class PrivateCtorKey
    {
        [FormerlySerializedAs("token")]
        [SerializeField]
        private string _token;

        // ReSharper disable once UnusedMember.Global
        public string Token => _token;

        private PrivateCtorKey()
        {
            _token = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Value type with private constructor for testing default value handling.
    /// </summary>
    [Serializable]
    public sealed class PrivateCtorValue
    {
        [FormerlySerializedAs("accent")]
        [SerializeField]
        private Color _accent = Color.magenta;

        [FormerlySerializedAs("intensity")]
        [SerializeField]
        private float _intensity = 1f;

        private PrivateCtorValue() { }

        // ReSharper disable once UnusedMember.Global
        public Color Accent => _accent;

        // ReSharper disable once UnusedMember.Global
        public float Intensity => _intensity;
    }

    /// <summary>
    /// Struct value for testing pending entry operations.
    /// </summary>
    [Serializable]
    public struct PendingStructValue
    {
        public string label;
        public Color tint;
    }

    /// <summary>
    /// Struct with multiple color fields and array for testing complex value serialization.
    /// </summary>
    [Serializable]
    public struct ColorData
    {
        public Color color1;

        // ReSharper disable once NotAccessedField.Global
        public Color color2;

        // ReSharper disable once NotAccessedField.Global
        public Color color3;

        // ReSharper disable once NotAccessedField.Global
        public Color color4;
        public Color[] otherColors;
    }

    /// <summary>
    /// Struct with a list of colors for testing list serialization within dictionary values.
    /// </summary>
    [Serializable]
    public struct ColorListData
    {
        public List<Color> colors;
    }

    /// <summary>
    /// Struct for testing label width stress scenarios.
    /// </summary>
    [Serializable]
    public struct LabelStressValue
    {
        // ReSharper disable once NotAccessedField.Global
        public float shortName;

        [FormerlySerializedAs("RidiculouslyVerboseFieldNameRequiringSpace")]
        // ReSharper disable once NotAccessedField.Global
        public float ridiculouslyVerboseFieldNameRequiringSpace;
    }
}
