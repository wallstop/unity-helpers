namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [Serializable]
    internal sealed class StringInListStringOptionsAsset : ScriptableObject
    {
        [StringInList("Idle", "Run", "Jump")]
        public string state = "Idle";
    }
#endif
}
