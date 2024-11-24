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
            int[] ints = Enumerable.Range(0, 1000).Select(_ => PcgRandom.Instance.Next()).ToArray();
            byte[] bytes = ArrayConverter.IntArrayToByteArray_BlockCopy(ints);
            int[] decoded = ArrayConverter.ByteArrayToIntArray_BlockCopy(bytes);
            Assert.IsTrue(ints.SequenceEqual(decoded));
        }
    }
}
