// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// Test asset for WValueDropDown attribute with SerializableType fields.
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownSerializableTypeAsset : ScriptableObject
    {
        [WValueDropDown(
            typeof(WValueDropDownSerializableTypeSource),
            nameof(WValueDropDownSerializableTypeSource.GetTypeOptions)
        )]
        public SerializableType selectedType;

        [WValueDropDown(
            typeof(WValueDropDownSerializableTypeSource),
            nameof(WValueDropDownSerializableTypeSource.GetSerializableTypeOptions)
        )]
        public SerializableType selectedSerializableType;

        [WValueDropDown(
            typeof(WValueDropDownSerializableTypeSource),
            nameof(WValueDropDownSerializableTypeSource.GetStringTypeOptions)
        )]
        public SerializableType selectedFromStrings;

        [WValueDropDown(nameof(GetInstanceTypeOptions), typeof(SerializableType))]
        public SerializableType instanceSelectedType;

        public List<Type> dynamicTypes = new();

        public IEnumerable<SerializableType> GetInstanceTypeOptions()
        {
            foreach (Type type in dynamicTypes)
            {
                yield return new SerializableType(type);
            }
        }
    }

    /// <summary>
    /// Static provider for SerializableType dropdown options.
    /// </summary>
    internal static class WValueDropDownSerializableTypeSource
    {
        private static readonly Type[] TypeOptions = new[]
        {
            typeof(int),
            typeof(string),
            typeof(float),
            typeof(double),
            typeof(bool),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Color),
        };

        private static readonly SerializableType[] SerializableTypeOptions = new[]
        {
            new SerializableType(typeof(int)),
            new SerializableType(typeof(string)),
            new SerializableType(typeof(float)),
            new SerializableType(typeof(double)),
        };

        public static IEnumerable<Type> GetTypeOptions()
        {
            return TypeOptions;
        }

        public static IEnumerable<SerializableType> GetSerializableTypeOptions()
        {
            return SerializableTypeOptions;
        }

        public static IEnumerable<string> GetStringTypeOptions()
        {
            foreach (Type type in TypeOptions)
            {
                yield return SerializableType.NormalizeTypeName(type);
            }
        }
    }
#endif
}
