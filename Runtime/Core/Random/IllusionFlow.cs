/*
    IllusionFlow is a significant enhancement upon the classic XoroShiroRandom discovered by Will Stafford Parsons.
        
    Reference: https://github.com/wstaffordp/illusionflow
    
    Copyright original author: https://github.com/wstaffordp
 */

namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using ProtoBuf;

    [Serializable]
    [DataContract]
    [ProtoContract]
    /// <summary>
    /// IllusionFlow: a modern, high-performance PRNG building on Xoroshiro concepts with additional state and mixing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IllusionFlow enhances the classic Xoroshiro approach with additional state and update rules for improved
    /// distribution characteristics. In this package, <see cref="PRNG.Instance"/> defaults to <see cref="IllusionFlow"/>
    /// to provide fast, high-quality randomness out of the box.
    /// </para>
    /// <para>Pros:</para>
    /// <list type="bullet">
    /// <item><description>Excellent performance; strong general-purpose statistical behavior.</description></item>
    /// <item><description>Deterministic and portable via <see cref="RandomState"/>.</description></item>
    /// </list>
    /// <para>Cons:</para>
    /// <list type="bullet">
    /// <item><description>Not cryptographically secure.</description></item>
    /// <item><description>Newer algorithm—choose established ones if you require historical precedence.</description></item>
    /// </list>
    /// <para>When to use:</para>
    /// <list type="bullet">
    /// <item><description>Default choice for most gameplay and procedural content needs.</description></item>
    /// </list>
    /// <para>When not to use:</para>
    /// <list type="bullet">
    /// <item><description>Cryptographic or adversarial contexts.</description></item>
    /// </list>
    /// <para>
    /// Threading: Prefer <see cref="ThreadLocalRandom{T}.Instance"/> or <see cref="PRNG.Instance"/> to avoid sharing state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using WallstopStudios.UnityHelpers.Core.Random;
    ///
    /// IRandom rng = PRNG.Instance; // IllusionFlow by default
    /// int index = rng.Next(0, items.Count);
    /// float gaussian = rng.NextGaussian(mean: 0f, stdDev: 1f);
    ///
    /// // Deterministic snapshot
    /// var state = rng.InternalState;
    /// var replay = new IllusionFlow(state);
    /// </code>
    /// </example>
    public sealed class IllusionFlow : AbstractRandom
    {
        private const int UintByteCount = sizeof(uint) * 8;

        public static IllusionFlow Instance => ThreadLocalRandom<IllusionFlow>.Instance;

        public override RandomState InternalState
        {
            get
            {
                ulong stateA = ((ulong)_a << UintByteCount) | _b;
                ulong stateB = ((ulong)_c << UintByteCount) | _d;
                // Pack _e into the low 32 bits of a double's bit pattern without allocations
                double packedE = BitConverter.Int64BitsToDouble(_e);
                return new RandomState(
                    stateA,
                    stateB,
                    packedE,
                    payload: null,
                    bitBuffer: _bitBuffer,
                    bitCount: _bitCount,
                    byteBuffer: _byteBuffer,
                    byteCount: _byteCount
                );
            }
        }

        [ProtoMember(6)]
        private uint _a;

        [ProtoMember(7)]
        private uint _b;

        [ProtoMember(8)]
        private uint _c;

        [ProtoMember(9)]
        private uint _d;

        [ProtoMember(10)]
        private uint _e;

        public IllusionFlow()
            : this(Guid.NewGuid()) { }

        public IllusionFlow(Guid guid, uint? extraSeed = null)
        {
            (uint a, uint b, uint c, uint d) = RandomUtilities.GuidToUInt32Quad(guid);
            _a = a;
            _b = b;
            _b = b;
            _c = c;
            _d = d;
            _e = extraSeed ?? unchecked((uint)guid.GetHashCode());
        }

        [JsonConstructor]
        public IllusionFlow(RandomState internalState)
        {
            unchecked
            {
                _a = (uint)(internalState.State1 >> UintByteCount);
                _b = (uint)internalState.State1;
                _c = (uint)(internalState.State2 >> UintByteCount);
                _d = (uint)internalState.State2;
                double? gaussian = internalState.Gaussian;
                if (gaussian != null)
                {
                    long bits = BitConverter.DoubleToInt64Bits(gaussian.Value);
                    _e = (uint)(unchecked((ulong)bits) & 0xFFFFFFFFUL);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"{nameof(IllusionFlow)} requires a Gaussian state."
                    );
                }
            }
            RestoreCommonState(internalState);
        }

        public override uint NextUint()
        {
            unchecked
            {
                uint result = _b + _e;
                ++_a;
                if (_a == 0U)
                {
                    _c += _e;
                    _d ^= _b;
                    _b += _c;
                    _e ^= _d;
                    return result;
                }

                _b = ((_b << 17) | (_b >> 15)) ^ _d;
                _d += 1111111111U;
                _e = (result << 13) | (result >> 19);
                return result;
            }
        }

        public override IRandom Copy()
        {
            return new IllusionFlow(InternalState);
        }
    }
}
