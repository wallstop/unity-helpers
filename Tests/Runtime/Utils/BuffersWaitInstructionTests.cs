namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class BuffersWaitInstructionTests
    {
        [Test]
        public void GetWaitForSecondsCachesByValue()
        {
            WaitForSeconds a1 = Buffers.GetWaitForSeconds(0.25f);
            WaitForSeconds a2 = Buffers.GetWaitForSeconds(0.25f);
            WaitForSeconds b = Buffers.GetWaitForSeconds(0.5f);

            Assert.AreSame(a1, a2);
            Assert.AreNotSame(a1, b);
        }

        [Test]
        public void GetWaitForSecondsRealtimeCachesByValue()
        {
            WaitForSecondsRealtime a1 = Buffers.GetWaitForSecondsRealTime(1f);
            WaitForSecondsRealtime a2 = Buffers.GetWaitForSecondsRealTime(1f);
            WaitForSecondsRealtime b = Buffers.GetWaitForSecondsRealTime(2f);

            Assert.AreSame(a1, a2);
            Assert.AreNotSame(a1, b);
        }
    }
}
