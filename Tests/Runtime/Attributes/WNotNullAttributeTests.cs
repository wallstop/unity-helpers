namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [TestFixture]
    public sealed class WNotNullAttributeTests
    {
        [Test]
        public void CheckForNullsThrowsWhenAnnotatedFieldIsNull()
        {
            WNotNullHolder holder = new();
            Assert.Throws<ArgumentNullException>(() => holder.CheckForNulls());

            holder.reference = new object();
            Assert.DoesNotThrow(() => holder.CheckForNulls());
        }
    }

    internal sealed class WNotNullHolder
    {
        [WNotNull]
        public object reference;
    }
}
