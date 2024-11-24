namespace UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    [Serializable]
    [DataContract]
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
                        gaussian: _seed != null ? 0.0 : null
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

        [JsonConstructor]
        public UnityRandom(RandomState internalState)
        {
            unchecked
            {
                _seed = internalState.Gaussian != null ? (int)internalState.State1 : null;
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
