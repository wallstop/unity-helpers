namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class StringInListNoOptionsAsset : ScriptableObject
    {
        [StringInList]
        public string unspecified = string.Empty;
    }
#endif
}
