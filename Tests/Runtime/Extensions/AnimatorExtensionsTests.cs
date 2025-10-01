namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class AnimatorExtensionsTests
    {
        [Test]
        public void ResetTriggersAllowsNullAnimator()
        {
            Animator animator = null;
            Assert.DoesNotThrow(() => animator.ResetTriggers());
        }

        [Test]
        public void ResetTriggersDoesNotThrowWhenDisabled()
        {
            GameObject gameObject = new("AnimatorResetTest");
            try
            {
                Animator animator = gameObject.AddComponent<Animator>();
                gameObject.SetActive(false);

                Assert.IsFalse(animator.isActiveAndEnabled);
                Assert.DoesNotThrow(() => animator.ResetTriggers());
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
