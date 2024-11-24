namespace UnityHelpers.Core.Random
{
    public sealed class UnityRandom : AbstractRandom
    {
        public static readonly UnityRandom Instance = new();

        public override RandomState InternalState
        {
            get
            {
                unchecked
                {
                    return new RandomState(
                        (ulong)(_seed ?? 0),
                        gaussian: _seed != null ? 0.0f : null
                    );
                }
            }
        }

        private readonly int? _seed;

        public UnityRandom(int? seed = null)
        {
            if (seed != null)
            {
                _seed = seed.Value;
                UnityEngine.Random.InitState(seed.Value);
            }
        }

        public UnityRandom(RandomState state)
        {
            unchecked
            {
                _seed = state.Gaussian != null ? (int)state.State1 : null;
            }
        }

        public override uint NextUint()
        {
            return unchecked((uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public override IRandom Copy()
        {
            return new UnityRandom(_seed);
        }
    }
}
