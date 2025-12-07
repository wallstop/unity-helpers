namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class PrivateCtorSetHost : ScriptableObject
    {
        public SerializableHashSet<PrivateCtorElement> set = new();
    }

    [Serializable]
    internal sealed class PrivateCtorElement
    {
        [SerializeField]
        private int magnitude;

        private PrivateCtorElement()
        {
            magnitude = 5;
        }

        // ReSharper disable once UnusedMember.Local
        public int Magnitude => magnitude;
    }
}
