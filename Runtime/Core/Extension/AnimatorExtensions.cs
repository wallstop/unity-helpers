namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using UnityEngine;

    /// <summary>
    /// Extension methods for Unity's Animator class.
    /// </summary>
    public static class AnimatorExtensions
    {
        /// <summary>
        /// Resets all trigger parameters on the Animator to their default state.
        /// </summary>
        /// <param name="animator">The Animator whose triggers will be reset.</param>
        /// <remarks>
        /// This method iterates through all parameters on the Animator and resets any parameters of type Trigger.
        /// Only processes the Animator if it is not null and is active and enabled.
        /// Non-trigger parameters are left unchanged.
        /// This is useful for cleaning up trigger states between animation transitions or when resetting an Animator to a known state.
        /// Thread-safe: No. Must be called from the main Unity thread.
        /// Performance: O(n) where n is the number of parameters on the Animator.
        /// </remarks>
        public static void ResetTriggers(this Animator animator)
        {
            if (animator == null || !animator.isActiveAndEnabled)
            {
                return;
            }

            foreach (AnimatorControllerParameter animatorParameter in animator.parameters)
            {
                if (animatorParameter.type != AnimatorControllerParameterType.Trigger)
                {
                    continue;
                }

                animator.ResetTrigger(animatorParameter.name);
            }
        }
    }
}
