// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Random;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class ArrayConverterTests : CommonTestBase
    {
        [Test]
        public void IntToByteArray()
        {
            int[] ints = Enumerable.Range(0, 1000).Select(_ => PRNG.Instance.Next()).ToArray();
            byte[] bytes = ArrayConverter.IntArrayToByteArrayBlockCopy(ints);
            int[] decoded = ArrayConverter.ByteArrayToIntArrayBlockCopy(bytes);
            Assert.IsTrue(ints.SequenceEqual(decoded));
        }
    }
}
