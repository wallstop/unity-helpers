// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.ValidateAssignment
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for ValidateAssignment combined with other attributes like Tooltip.
    /// </summary>
    internal sealed class OdinValidateAssignmentWithOtherAttributesTarget
        : SerializedScriptableObject
    {
        [ValidateAssignment]
        [Tooltip("This field must have a value")]
        public string validateWithTooltip;
    }
#endif
}
