/*
    GroundZeroRandom is a truly novel random implementation discovered by Will Stafford Parsons.
    In particular, this is a port of their XoroShiro enhancement which uses <= 193 bits of state.
    Perhaps there could be something even better if the remaining state bits were used. Perhaps not.
    
    Reference: https://github.com/wstaffordp/bsearch-enhanced/blob/master/examples/benchmark.c#L4-L29
    
    Regardless, all credit to the legend: https://github.com/wstaffordp
    
    I do not own this. Please contact the above-mentioned person for usage information.
    
    I made the name up.
    @wallstop
 */

namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    [Serializable]
    [DataContract]
    public sealed class XoroShiroEnhancedRandom : AbstractRandom
    {
        public override RandomState InternalState
        {
            get
            {
                ulong stateA = ((ulong)_a << 32) | _b;
                ulong stateB = ((ulong)_c << 32) | _d;
                byte[] eBytes = BitConverter.GetBytes(_e);
                Array.Resize(ref eBytes, sizeof(double));
                Array.Fill<byte>(eBytes, 0, sizeof(uint), sizeof(double) - sizeof(uint));
                return new RandomState(stateA, stateB, BitConverter.ToDouble(eBytes, 0));
            }
        }

        private uint _a;
        private uint _b;
        private uint _c;
        private uint _d;
        private uint _e;

        public XoroShiroEnhancedRandom()
            : this(Guid.NewGuid()) { }

        public XoroShiroEnhancedRandom(Guid guid, uint? extraSeed = null)
        {
            byte[] guidArray = guid.ToByteArray();
            _a = BitConverter.ToUInt32(guidArray, 0);
            _b = BitConverter.ToUInt32(guidArray, sizeof(uint));
            _c = BitConverter.ToUInt32(guidArray, sizeof(uint) * 2);
            _d = BitConverter.ToUInt32(guidArray, sizeof(uint) * 3);
            _e = extraSeed ?? unchecked((uint)guid.GetHashCode());
        }

        [JsonConstructor]
        public XoroShiroEnhancedRandom(RandomState internalState)
        {
            unchecked
            {
                _a = (uint)(internalState.State1 >> 32);
                _b = (uint)internalState.State1;
                _c = (uint)(internalState.State2 >> 32);
                _d = (uint)internalState.State2;
                double? gaussian = internalState.Gaussian;
                if (gaussian != null)
                {
                    byte[] eBytes = BitConverter.GetBytes(gaussian.Value);
                    Array.Resize(ref eBytes, sizeof(uint));
                    _e = BitConverter.ToUInt32(eBytes, 0);
                }
                else
                {
                    throw new InvalidOperationException(
                        "GroundZeroRandom requires a Gaussian state."
                    );
                }
            }
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
            return new XoroShiroEnhancedRandom(InternalState);
        }
    }
}
