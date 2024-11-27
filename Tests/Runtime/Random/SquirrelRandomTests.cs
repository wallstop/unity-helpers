namespace UnityHelpers.Tests.Random
{
    using UnityHelpers.Core.Random;

    public sealed class SquirrelRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new SquirrelRandom();

        protected override double GetDeviationFor(string caller)
        {
            return 0.075;
        }
    }
}
