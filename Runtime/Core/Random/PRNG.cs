namespace WallstopStudios.UnityHelpers.Core.Random
{
    public static class PRNG
    {
        public static IRandom Instance => PcgRandom.Instance;
    }
}
