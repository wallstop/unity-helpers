namespace UnityHelpers.Tests.Random
{
    using UnityHelpers.Core.Random;

    public sealed class SquirrelRandomTests : RandomTestBase<SquirrelRandom>
    {
        protected override SquirrelRandom NewRandom() => new SquirrelRandom();
    }
}
