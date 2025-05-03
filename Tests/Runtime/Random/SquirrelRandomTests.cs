namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class SquirrelRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new SquirrelRandom();

        protected override double GetDeviationFor(string caller)
        {
            return 0.075;
        }
    }
}
