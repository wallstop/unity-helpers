namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class DeferredDisposalResultTests
    {
        [Test]
        public async Task DisposeAsyncInvokesDelegate()
        {
            bool disposed = false;
            DeferredDisposalResult<int> result = new(
                7,
                async () =>
                {
                    await Task.Yield();
                    disposed = true;
                }
            );

            Assert.AreEqual(7, result.result);
            await result.DisposeAsync();
            Assert.IsTrue(disposed);
        }

        [Test]
        public void ConstructorThrowsOnNullDisposeDelegate()
        {
            Assert.Throws<ArgumentNullException>(() => new DeferredDisposalResult<int>(1, null));
        }
    }
}
