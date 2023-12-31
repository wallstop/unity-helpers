namespace UnityHelpers.Core.Random
{
    using System;

    public sealed class UnityRandom : AbstractRandom
    {
        public static readonly UnityRandom Instance = new UnityRandom();

        private UnityRandom()
        {
        }

        public override RandomState InternalState => throw new NotSupportedException("Unity Random does not expose its internal state");
        public override uint NextUint()
        {
            return unchecked((uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        public override IRandom Copy()
        {
            throw new NotSupportedException("Unity Random does not support copying / seeding");
        }
    }
}
