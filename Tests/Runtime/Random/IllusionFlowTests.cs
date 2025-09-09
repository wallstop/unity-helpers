namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class IllusionFlowTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new IllusionFlow();
        }
    }
}
