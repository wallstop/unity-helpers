namespace Core.Extension
{
    using UnityEngine;

    public static class AnimatorExtensions
    {
        public static void ResetTriggers(this Animator animator)
        {
            if (!animator || !animator.isActiveAndEnabled)
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
