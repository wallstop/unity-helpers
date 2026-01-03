// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class AnimatorExtensionsTests : CommonTestBase
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
            GameObject gameObject = Track(new GameObject("AnimatorResetTest"));
            Animator animator = gameObject.AddComponent<Animator>();
            gameObject.SetActive(false);

            Assert.IsFalse(animator.isActiveAndEnabled);
            Assert.DoesNotThrow(() => animator.ResetTriggers());
        }
    }
}
