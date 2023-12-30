namespace Tests.Runtime.Scripts
{
    using Core.Random;

    public sealed class PcgRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new PcgRandom();
    }
}
