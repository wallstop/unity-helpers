namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class AsyncOperationExtensionsTests
    {
        [Test]
        public void WithContinuationOnValueTaskExecutesAction()
        {
            bool invoked = false;
            new ValueTask()
                .WithContinuation(() => invoked = true)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            Assert.IsTrue(invoked);
        }

        [Test]
        public void WithContinuationOnValueTaskWithResultTransformsValue()
        {
            ValueTask<int> valueTask = new(5);
            int result = valueTask
                .WithContinuation(v => v * 2)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            Assert.AreEqual(10, result);
        }

        [Test]
        public void WithContinuationOnValueTaskWithResultPassesValueToAction()
        {
            ValueTask<int> valueTask = new(7);
            int observed = 0;
            valueTask
                .WithContinuation(v => observed = v)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            Assert.AreEqual(7, observed);
        }
    }
}
