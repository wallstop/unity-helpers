namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class PhotonSpinRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new PhotonSpinRandom(DeterministicSeed32);
        }
    }
}
