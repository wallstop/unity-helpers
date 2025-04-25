namespace UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using Core.Extension;
    using UnityEditor;
    using UnityEngine;
    using UnityHelpers.Utils;

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
