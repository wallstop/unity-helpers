namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class MathHelpersTests : CommonTestBase
    {
        [Test]
        public void IsLeftReturnsTrueWhenPointIsLeftOfRay()
        {
            Vector2 a = Vector2.zero;
            Vector2 b = Vector2.right;
            Vector2 point = Vector2.up;
            Assert.IsTrue(Helpers.IsLeft(a, b, point));
        }

        [Test]
        public void IsLeftReturnsFalseWhenPointIsOnRay()
        {
            Vector2 a = Vector2.zero;
            Vector2 b = Vector2.right;
            Vector2 point = new(0.5f, 0f);
            Assert.IsFalse(Helpers.IsLeft(a, b, point));
        }

        [Test]
        public void IsLeftReturnsFalseWhenPointIsRightOfRay()
        {
            Vector2 a = Vector2.zero;
            Vector2 b = Vector2.right;
            Vector2 point = Vector2.down;
            Assert.IsFalse(Helpers.IsLeft(a, b, point));
        }

        [Test]
        public void RadianToVector2ProducesUnitVector()
        {
            Vector2 vector = Helpers.RadianToVector2(Mathf.PI / 2f);
            Assert.That(vector.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(vector.y, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void DegreeToVector2ProducesExpectedDirection()
        {
            Vector2 vector = Helpers.DegreeToVector2(180f);
            Assert.That(vector.x, Is.EqualTo(-1f).Within(1e-5f));
            Assert.That(vector.y, Is.EqualTo(0f).Within(1e-5f));
        }
    }
}
