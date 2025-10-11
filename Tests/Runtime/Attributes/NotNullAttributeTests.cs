namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [TestFixture]
    public sealed class NotNullAttributeTests
    {
        [Test]
        public void CheckForNullsThrowsWhenAnnotatedFieldIsNull()
        {
            NotNullHolder holder = new();
            Assert.Throws<ArgumentNullException>(() => holder.CheckForNulls());

            holder.reference = new object();
            Assert.DoesNotThrow(() => holder.CheckForNulls());
        }
    }

    internal sealed class NotNullHolder
    {
        [NotNull]
        public object reference;
    }
}
