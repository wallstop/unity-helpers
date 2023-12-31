namespace UnityHelpers.Core.Random
{
    using System;

    /// <summary>
    ///     Implementation dependent upon .Net's Random class.
    /// </summary>
    public sealed class SystemRandom : AbstractRandom
    {
        /*
            Copied from Random.cs source. Apparently it isn't guaranteed to be the
            same across platforms and we depend on that.
         */
        private int inext;
        private int inextp;
        private readonly int[] SeedArray = new int[56];

        public static IRandom Instance => ThreadLocalRandom<SystemRandom>.Instance;

        public SystemRandom() : this (Environment.TickCount)
        {
        }

        public SystemRandom(int seed)
        {
            int num1 = 161803398 - (seed == int.MinValue ? int.MaxValue : Math.Abs(seed));
            this.SeedArray[55] = num1;
            int num2 = 1;
            for (int index1 = 1; index1 < 55; ++index1)
            {
                int index2 = 21 * index1 % 55;
                this.SeedArray[index2] = num2;
                num2 = num1 - num2;
                if (num2 < 0)
                    num2 += int.MaxValue;
                num1 = this.SeedArray[index2];
            }
            for (int index3 = 1; index3 < 5; ++index3)
            {
                for (int index4 = 1; index4 < 56; ++index4)
                {
                    this.SeedArray[index4] -= this.SeedArray[1 + (index4 + 30) % 55];
                    if (this.SeedArray[index4] < 0)
                        this.SeedArray[index4] += int.MaxValue;
                }
            }
            this.inext = 0;
            this.inextp = 21;
        }

        public SystemRandom(RandomState randomState)
        {
            inext = unchecked((int)randomState.State1);
            inextp = unchecked((int)randomState.State2);
            _cachedGaussian = randomState.Gaussian;
        }

        public override RandomState InternalState => new(unchecked((ulong)inext), unchecked((ulong)inextp), _cachedGaussian);

        public override uint NextUint()
        {
            int inext = this.inext;
            int inextp = this.inextp;
            int index1;
            if ((index1 = inext + 1) >= 56)
                index1 = 1;
            int index2;
            if ((index2 = inextp + 1) >= 56)
                index2 = 1;
            int num = this.SeedArray[index1] - this.SeedArray[index2];
            if (num == int.MaxValue)
                --num;
            if (num < 0)
                num += int.MaxValue;
            this.SeedArray[index1] = num;
            this.inext = index1;
            this.inextp = index2;
            return unchecked((uint) num);
        }

        public override double NextDouble()
        {
            return (int) NextUint() * 4.6566128752458E-10;
        }

        public override float NextFloat()
        {
            return (float) NextDouble();
        }

        public override IRandom Copy()
        {
            SystemRandom copy = new(InternalState);

            for (int i = 0; i < SeedArray.Length; ++i)
            {
                copy.SeedArray[i] = SeedArray[i];
            }

            return copy;
        }
    }
}
