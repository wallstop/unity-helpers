// ReSharper disable once CheckNamespace
namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System.Collections.Generic;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public static partial class UnityExtensions
    {
#if UNITY_EDITOR
        /// <summary>
        /// Extracts all Sprite objects referenced in an AnimationClip.
        /// </summary>
        /// <param name="clip">The AnimationClip to extract sprites from.</param>
        /// <returns>An enumerable of all Sprite objects found in the animation clip.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread. Editor-only.
        /// Null Handling: Returns empty enumerable if clip is null.
        /// Performance: O(n*m) where n is number of bindings and m is keyframes per binding.
        /// Allocations: Allocates arrays for bindings and keyframes.
        /// Unity Behavior: Only available in Unity Editor. Uses AnimationUtility.
        /// Edge Cases: Only returns Sprite object references, ignores other object types.
        /// </remarks>
        public static IEnumerable<Sprite> GetSpritesFromClip(this AnimationClip clip)
        {
            if (clip == null)
            {
                yield break;
            }

            foreach (
                EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)
            )
            {
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(
                    clip,
                    binding
                );
                foreach (ObjectReferenceKeyframe frame in keyframes)
                {
                    if (frame.value is Sprite sprite)
                    {
                        yield return sprite;
                    }
                }
            }
        }
#endif
    }
}
