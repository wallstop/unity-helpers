namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class ArrayConverterTests
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
