namespace Tests.Runtime.Scripts
{
    using Core.Random;

    public sealed class SquirrelRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new SquirrelRandom();
    }
}
