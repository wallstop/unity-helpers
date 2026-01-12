// MIT License - Copyright (c) 2024 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;

    [CustomEditor(typeof(MatchColliderToSprite))]
    public sealed class MatchColliderToSpriteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            MatchColliderToSprite matchColliderToSprite = target as MatchColliderToSprite;
            if (matchColliderToSprite == null)
            {
                this.LogError(
                    $"Target was of type {target?.GetType()}, expected {nameof(MatchColliderToSprite)}."
                );
                return;
            }

            if (GUILayout.Button("MatchColliderToSprite"))
            {
                matchColliderToSprite.OnValidate();
            }
        }
    }
#endif
}
