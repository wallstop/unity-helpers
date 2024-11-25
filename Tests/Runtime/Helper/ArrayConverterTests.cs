namespace UnityHelpers.Tests.Tests.Runtime.Helper
{
    using System.Linq;
    using Core.Helper;
    using Core.Random;
    using NUnit.Framework;

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
