namespace UnityHelpers.Tests.Extensions
{
    using System;
    using System.Linq;
    using Core.Extension;
    using NUnit.Framework;

    public sealed class IListExtensionTests
    {
        [Test]
        public void ShiftLeft()
        {
            int[] input = Enumerable.Range(0, 10).ToArray();
            for (int i = 0; i < input.Length * 2; ++i)
            {
                int[] shifted = input.ToArray();
                shifted.Shift(-1 * i);
                Assert.That(
                    input.Skip(i % input.Length).Concat(input.Take(i % input.Length)),
                    Is.EqualTo(shifted)
                );
            }
        }

        [Test]
        public void ShiftRight()
        {
            int[] input = Enumerable.Range(0, 10).ToArray();
            for (int i = 0; i < input.Length * 2; ++i)
            {
                int[] shifted = input.ToArray();
                shifted.Shift(i);
                Assert.That(
                    input
                        .Skip((input.Length * 3 - i) % input.Length)
                        .Concat(input.Take((input.Length * 3 - i) % input.Length)),
                    Is.EqualTo(shifted),
                    $"Shift failed for amount {i}."
                );
            }
        }

        [Test]
        public void Reverse()
        {
            int[] input = Enumerable.Range(0, 10).ToArray();
            for (int i = 0; i < input.Length; ++i)
            {
                int[] shifted = input.ToArray();
                shifted.Reverse(0, i);
                Assert.That(
                    input.Take(i + 1).Reverse().Concat(input.Skip(i + 1)),
                    Is.EqualTo(shifted),
                    $"Reverse failed for reversal from [0, {i}]."
                );
            }

            // TODO
        }

        [Test]
        public void ReverseInvalidArguments()
        {
            int[] input = Enumerable.Range(0, 10).ToArray();
            Assert.Throws<ArgumentException>(() => input.Reverse(-1, 1));
            Assert.Throws<ArgumentException>(() => input.Reverse(input.Length, 1));
            Assert.Throws<ArgumentException>(() => input.Reverse(int.MaxValue, 1));
            Assert.Throws<ArgumentException>(() => input.Reverse(int.MinValue, 1));

            Assert.Throws<ArgumentException>(() => input.Reverse(1, -1));
            Assert.Throws<ArgumentException>(() => input.Reverse(1, input.Length));
            Assert.Throws<ArgumentException>(() => input.Reverse(1, int.MaxValue));
            Assert.Throws<ArgumentException>(() => input.Reverse(1, int.MinValue));
        }
    }
}
