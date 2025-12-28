// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment on Queue fields.
    /// </summary>
    internal sealed class OdinValidateAssignmentQueueTarget : SerializedScriptableObject
    {
        [ValidateAssignment]
        public Queue<string> validateQueue;
    }
#endif
}
