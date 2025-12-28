// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.NotNull
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WNotNull attribute on MonoBehaviour with Odin Inspector.
    /// </summary>
    public sealed class OdinNotNullMonoBehaviourTarget : SerializedMonoBehaviour
    {
        [WNotNull]
        public Transform notNullReference;
    }
#endif
}
