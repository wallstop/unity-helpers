// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;

    internal static class AnimationEventTimeFieldRenderer
    {
        public static void DrawTimeFields(
            AnimationEventItem item,
            int currentFrame,
            float frameRate,
            float clipLength,
            bool controlFrameTime,
            Action<string> recordUndo,
            Action onTimeChanged = null
        )
        {
            AnimationEvent animEvent = item.animationEvent;
            EditorGUI.BeginChangeCheck();
            float proposedTime;

            if (controlFrameTime)
            {
                proposedTime = EditorGUILayout.FloatField("FrameTime", animEvent.time);
                proposedTime = Mathf.Clamp(proposedTime, 0f, clipLength);
            }
            else
            {
                int proposedFrame = EditorGUILayout.IntField("FrameIndex", currentFrame);
                float safeFrameRate = frameRate <= 0f ? 1f : frameRate;
                int maxFrameIndex = Mathf.RoundToInt(clipLength * safeFrameRate);
                proposedTime =
                    Mathf.Clamp(proposedFrame, 0, maxFrameIndex) / Math.Max(safeFrameRate, 0.0001f);
            }

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            recordUndo?.Invoke("Change Animation Event Time");
            animEvent.time = proposedTime;
            onTimeChanged?.Invoke();
        }
    }
#endif
}
