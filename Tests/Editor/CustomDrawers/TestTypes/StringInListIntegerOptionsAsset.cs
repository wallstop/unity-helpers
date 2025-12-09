namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class StringInListIntegerOptionsAsset : ScriptableObject
    {
        [StringInList("Low", "Medium", "High")]
        public int selection;
    }
#endif
}
