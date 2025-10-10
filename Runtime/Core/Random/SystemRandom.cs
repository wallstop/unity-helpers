namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Helper;
    using ProtoBuf;

    /// <summary>
    /// A reimplementation of legacy .NET <c>System.Random</c> behavior for deterministic, serializable use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrors the algorithm used by .NET's classic <c>System.Random</c> so its sequence can be serialized and
    /// reproduced across platforms. This is not the same as .NET 6+ <c>Random</c> improvements and is slower than
    /// modern PRNGs.
    /// </para>
    /// <para>Pros:</para>
    /// <list type="bullet">
    /// <item><description>Behavioral parity with classic <c>System.Random</c> sequences for compatibility.</description></item>
    /// <item><description>Deterministic and serializable via <see cref="RandomState"/>.</description></item>
    /// </list>
    /// <para>Cons:</para>
    /// <list type="bullet">
    /// <item><description>Slower than PCG/Xoroshiro/RomuDuo; larger state.</description></item>
    /// <item><description>Not cryptographically secure; modest statistical quality.</description></item>
    /// </list>
    /// <para>When to use:</para>
    /// <list type="bullet">
    /// <item><description>When you need to match or migrate code that relied on <c>System.Random</c>.</description></item>
    /// </list>
    /// <para>When not to use:</para>
    /// <list type="bullet">
    /// <item><description>General gameplay—prefer faster, higher-quality PRNGs like PCG or IllusionFlow (via <see cref="PRNG.Instance"/>).</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// using WallstopStudios.UnityHelpers.Core.Random;
    ///
    /// var compatible = new SystemRandom(seed: 123);
    /// // Produces the same sequence as classic System.Random initialized with 123
    /// int a = compatible.Next();
    /// double d = compatible.NextDouble();
    /// </code>
    /// </example>
    [Serializable]
    [DataContract]
    [ProtoContract(SkipConstructor = true)]
    public sealed class SystemRandom : AbstractRandom
    {
        private const int HalfwayInt = int.MaxValue / 2;
        private const int SeedArraySize = 56;
        private const int LastSeedIndex = SeedArraySize - 1;

        public static SystemRandom Instance => ThreadLocalRandom<SystemRandom>.Instance;

        public override RandomState InternalState =>
            BuildState(
                unchecked((ulong)_inext),
                unchecked((ulong)_inextp),
                ArrayConverter.IntArrayToByteArrayBlockCopy(_seedArray)
            );

        /*
            Copied from Random.cs source. Apparently it isn't guaranteed to be the
            same across platforms, a fact which defeats the purpose of these serializable
            randoms.
         */
        [ProtoMember(6)]
        private int _inext;

        [ProtoMember(7)]
        private int _inextp;

        [ProtoMember(8)]
        private readonly int[] _seedArray = new int[SeedArraySize];

        public SystemRandom()
            : this(Guid.NewGuid().GetHashCode()) { }

        public SystemRandom(int seed)
        {
            int num1 = 161803398 - (seed == int.MinValue ? int.MaxValue : Math.Abs(seed));
            _seedArray[LastSeedIndex] = num1;
            int num2 = 1;
            for (int index1 = 1; index1 < LastSeedIndex; ++index1)
            {
                int index2 = 21 * index1 % LastSeedIndex;
                _seedArray[index2] = num2;
                num2 = num1 - num2;
                if (num2 < 0)
                {
                    num2 += int.MaxValue;
                }

                num1 = _seedArray[index2];
            }
            for (int index3 = 1; index3 < 5; ++index3)
            {
                for (int index4 = 1; index4 < SeedArraySize; ++index4)
                {
                    int value = _seedArray[index4] -= _seedArray[1 + (index4 + 30) % LastSeedIndex];
                    if (value < 0)
                    {
                        _seedArray[index4] += int.MaxValue;
                    }
                }
            }

            _inext = 0;
            _inextp = 21;
        }

        [JsonConstructor]
        public SystemRandom(RandomState internalState)
        {
            unchecked
            {
                _inext = (int)internalState.State1;
                _inextp = (int)internalState.State2;
            }
            RestoreCommonState(internalState);
            _seedArray = ArrayConverter.ByteArrayToIntArrayBlockCopy(internalState._payload);
        }

        public override int Next()
        {
            int localINext = _inext;
            int localINextP = _inextp;
            int index1;
            if ((index1 = localINext + 1) >= SeedArraySize)
            {
                index1 = 1;
            }

            int index2;
            if ((index2 = localINextP + 1) >= SeedArraySize)
            {
                index2 = 1;
            }

            int num = _seedArray[index1] - _seedArray[index2];
            if (num == int.MaxValue)
            {
                --num;
            }

            if (num < 0)
            {
                num += int.MaxValue;
            }

            _seedArray[index1] = num;
            _inext = index1;
            _inextp = index2;
            return num;
        }

        public override uint NextUint()
        {
            if (NextBool())
            {
                return unchecked((uint)(Next() ^ 0x80000000));
            }
            return unchecked((uint)Next());
        }

        public override bool NextBool()
        {
            return Next() < HalfwayInt;
        }

        public override double NextDouble()
        {
            double random;
            do
            {
                random = Next() / (1.0 * int.MaxValue);
            } while (1.0 <= random);

            return random;
        }

        public override float NextFloat()
        {
            float random;
            do
            {
                random = Next() * (1f / int.MaxValue);
            } while (1f <= random);

            return random;
        }

        public override IRandom Copy()
        {
            SystemRandom copy = new(InternalState);
            Array.Copy(_seedArray, copy._seedArray, _seedArray.Length);
            return copy;
        }
    }
}
