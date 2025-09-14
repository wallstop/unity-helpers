namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using NUnit.Framework;
    using UnityEngine.TestTools.Constraints;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using Is = NUnit.Framework.Is;

    public sealed class EnumExtensionTests
    {
        private enum TestEnum
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
        }

        private enum TinyTestEnum : byte
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
        }

        private enum SmallTestEnum : short
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
        }

        private enum BigTestEnum : long
        {
            None = 0,
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
        }

        [Test]
        public void HasFlagNoAlloc()
        {
            Assert.IsTrue(TestEnum.First.HasFlagNoAlloc(TestEnum.First));
            Assert.IsFalse(TestEnum.First.HasFlagNoAlloc(TestEnum.Second));
            Assert.IsTrue((TestEnum.First | TestEnum.Second).HasFlagNoAlloc(TestEnum.First));
            Assert.IsTrue((TestEnum.First | TestEnum.Second).HasFlagNoAlloc(TestEnum.Second));
            Assert.IsFalse((TestEnum.First | TestEnum.Second).HasFlagNoAlloc(TestEnum.Third));
            Assert.IsFalse(TestEnum.First.HasFlagNoAlloc((TestEnum.First | TestEnum.Second)));
        }

        [Test]
        public void HasFlagNoAllocTiny()
        {
            Assert.IsTrue(TinyTestEnum.First.HasFlagNoAlloc(TinyTestEnum.First));
            Assert.IsFalse(TinyTestEnum.First.HasFlagNoAlloc(TinyTestEnum.Second));
            Assert.IsTrue(
                (TinyTestEnum.First | TinyTestEnum.Second).HasFlagNoAlloc(TinyTestEnum.First)
            );
            Assert.IsTrue(
                (TinyTestEnum.First | TinyTestEnum.Second).HasFlagNoAlloc(TinyTestEnum.Second)
            );
            Assert.IsFalse(
                (TinyTestEnum.First | TinyTestEnum.Second).HasFlagNoAlloc(TinyTestEnum.Third)
            );
            Assert.IsFalse(
                TinyTestEnum.First.HasFlagNoAlloc((TinyTestEnum.First | TinyTestEnum.Second))
            );
        }

        [Test]
        public void HasFlagNoAllocSmall()
        {
            Assert.IsTrue(SmallTestEnum.First.HasFlagNoAlloc(SmallTestEnum.First));
            Assert.IsFalse(SmallTestEnum.First.HasFlagNoAlloc(SmallTestEnum.Second));
            Assert.IsTrue(
                (SmallTestEnum.First | SmallTestEnum.Second).HasFlagNoAlloc(SmallTestEnum.First)
            );
            Assert.IsTrue(
                (SmallTestEnum.First | SmallTestEnum.Second).HasFlagNoAlloc(SmallTestEnum.Second)
            );
            Assert.IsFalse(
                (SmallTestEnum.First | SmallTestEnum.Second).HasFlagNoAlloc(SmallTestEnum.Third)
            );
            Assert.IsFalse(
                SmallTestEnum.First.HasFlagNoAlloc((SmallTestEnum.First | SmallTestEnum.Second))
            );
        }

        [Test]
        public void HasFlagNoAllocBig()
        {
            Assert.IsTrue(BigTestEnum.First.HasFlagNoAlloc(BigTestEnum.First));
            Assert.IsFalse(BigTestEnum.First.HasFlagNoAlloc(BigTestEnum.Second));
            Assert.IsTrue(
                (BigTestEnum.First | BigTestEnum.Second).HasFlagNoAlloc(BigTestEnum.First)
            );
            Assert.IsTrue(
                (BigTestEnum.First | BigTestEnum.Second).HasFlagNoAlloc(BigTestEnum.Second)
            );
            Assert.IsFalse(
                (BigTestEnum.First | BigTestEnum.Second).HasFlagNoAlloc(BigTestEnum.Third)
            );
            Assert.IsFalse(
                BigTestEnum.First.HasFlagNoAlloc((BigTestEnum.First | BigTestEnum.Second))
            );
        }

        [Test]
        public void HasFlagsNoAllocDoesNotAlloc()
        {
            Assert.That(
                () =>
                {
                    TestEnum.First.HasFlagNoAlloc(TestEnum.First);
                    TinyTestEnum.First.HasFlagNoAlloc(TinyTestEnum.First);
                    BigTestEnum.First.HasFlagNoAlloc(BigTestEnum.First);

                    TestEnum.First.HasFlagNoAlloc(TestEnum.Second);
                    TinyTestEnum.First.HasFlagNoAlloc(TinyTestEnum.Second);
                    BigTestEnum.First.HasFlagNoAlloc(BigTestEnum.Second);
                },
                Is.Not.AllocatingGCMemory()
            );
        }
    }
}
