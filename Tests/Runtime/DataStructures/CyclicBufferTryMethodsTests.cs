namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class CyclicBufferTryMethodsTests
    {
        [Test]
        public void TryPopFrontRemovesFromFront()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            bool success = buffer.TryPopFront(out int result);

            Assert.IsTrue(success);
            Assert.AreEqual(1, result);
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(2, buffer[0]);
        }

        [Test]
        public void TryPopFrontReturnsFalseWhenEmpty()
        {
            CyclicBuffer<int> buffer = new(5);

            bool success = buffer.TryPopFront(out int result);

            Assert.IsFalse(success);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void TryPopBackRemovesFromBack()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            bool success = buffer.TryPopBack(out int result);

            Assert.IsTrue(success);
            Assert.AreEqual(3, result);
            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(2, buffer[1]);
        }

        [Test]
        public void TryPopBackReturnsFalseWhenEmpty()
        {
            CyclicBuffer<int> buffer = new(5);

            bool success = buffer.TryPopBack(out int result);

            Assert.IsFalse(success);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void TryPopFrontMultipleTimes()
        {
            CyclicBuffer<int> buffer = new(10);
            for (int i = 0; i < 5; i++)
            {
                buffer.Add(i);
            }

            Assert.IsTrue(buffer.TryPopFront(out int first));
            Assert.AreEqual(0, first);

            Assert.IsTrue(buffer.TryPopFront(out int second));
            Assert.AreEqual(1, second);

            Assert.AreEqual(3, buffer.Count);
        }

        [Test]
        public void TryPopBackMultipleTimes()
        {
            CyclicBuffer<int> buffer = new(10);
            for (int i = 0; i < 5; i++)
            {
                buffer.Add(i);
            }

            Assert.IsTrue(buffer.TryPopBack(out int first));
            Assert.AreEqual(4, first);

            Assert.IsTrue(buffer.TryPopBack(out int second));
            Assert.AreEqual(3, second);

            Assert.AreEqual(3, buffer.Count);
        }

        [Test]
        public void TryPopFrontAndBackMixed()
        {
            CyclicBuffer<int> buffer = new(10);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            Assert.IsTrue(buffer.TryPopFront(out int front1));
            Assert.AreEqual(1, front1);

            Assert.IsTrue(buffer.TryPopBack(out int back1));
            Assert.AreEqual(4, back1);

            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
        }

        [Test]
        public void TryPopUntilEmpty()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);

            Assert.IsTrue(buffer.TryPopFront(out _));
            Assert.IsTrue(buffer.TryPopFront(out _));
            Assert.IsFalse(buffer.TryPopFront(out _));
            Assert.AreEqual(0, buffer.Count);
        }

        [Test]
        public void TryPopWithWrappedBuffer()
        {
            CyclicBuffer<int> buffer = new(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // Wraps, removes 1
            buffer.Add(5); // Wraps, removes 2

            Assert.IsTrue(buffer.TryPopFront(out int front));
            Assert.AreEqual(3, front);

            Assert.IsTrue(buffer.TryPopBack(out int back));
            Assert.AreEqual(5, back);

            Assert.AreEqual(1, buffer.Count);
        }
    }
}
