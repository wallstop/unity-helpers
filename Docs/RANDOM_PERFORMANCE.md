# Random Number Generator Performance Benchmarks

> Auto-generated via RandomPerformanceTests.Benchmark. Run the test to refresh these summary and detail tables.

<!-- RANDOM_BENCHMARKS_START -->

## Summary (fastest first)

<table>
  <thead>
    <tr>
      <th align="left">Random</th>
      <th align="right">NextUint (ops/s)</th>
      <th align="left">Speed</th>
      <th align="left">Quality</th>
      <th align="left">Notes</th>
    </tr>
  </thead>
  <tbody>
    <tr><td>WaveSplatRandom</td><td align="right">1,314,600,000</td><td>Fastest</td><td>Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental. <a href="https://github.com/wileylooper/wavesplat">wileylooper/wavesplat</a></td></tr>
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,310,800,000</td><td>Fastest</td><td>Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions. <a href="https://doi.org/10.1145/63039.63042">Park &amp; Miller 1988</a></td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">1,060,400,000</td><td>Very Fast</td><td>Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator. <a href="https://github.com/wileylooper/blastcircuit">wileylooper/blastcircuit</a></td></tr>
    <tr><td>SplitMix64</td><td align="right">959,100,000</td><td>Fast</td><td>Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="http://xoshiro.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">948,000,000</td><td>Fast</td><td>Excellent</td><td>Hybrid Xoshiro/PCG variant tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="http://xoshiro.di.unimi.it">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">906,700,000</td><td>Fast</td><td>Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">813,800,000</td><td>Fast</td><td>Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author. <a href="https://github.com/wileylooper/illusionflow">wileylooper/illusionflow</a></td></tr>
    <tr><td>RomuDuo</td><td align="right">765,100,000</td><td>Fast</td><td>Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations. <a href="https://romu-random.org/">Markus &amp; Crow 2019</a></td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">759,500,000</td><td>Fast</td><td>Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="http://xoshiro.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>StormDropRandom</td><td align="right">720,300,000</td><td>Moderate</td><td>Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods. <a href="https://github.com/wileylooper/stormdrop">wileylooper/stormdrop</a></td></tr>
    <tr><td>XorShiftRandom</td><td align="right">596,100,000</td><td>Moderate</td><td>Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://www.jstatsoft.org/article/view/v008i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">446,500,000</td><td>Slow</td><td>Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">414,300,000</td><td>Slow</td><td>Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://github.com/squirrel-org/squirrel3">Squirrel3</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">265,200,000</td><td>Slow</td><td>Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties. <a href="https://github.com/wileylooper/photonspin">wileylooper/photonspin</a></td></tr>
    <tr><td>UnityRandom</td><td align="right">87,700,000</td><td>Very Slow</td><td>Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://blog.unity.com/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">64,900,000</td><td>Very Slow</td><td>Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">57,200,000</td><td>Very Slow</td><td>Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
  </tbody>
</table>

## Detailed Metrics

<table>
  <thead>
    <tr>
      <th align="left">Random</th>
      <th align="right">NextBool</th>
      <th align="right">Next</th>
      <th align="right">NextUint</th>
      <th align="right">NextFloat</th>
      <th align="right">NextDouble</th>
      <th align="right">NextUint (Range)</th>
      <th align="right">NextInt (Range)</th>
    </tr>
  </thead>
  <tbody>
    <tr><td>WaveSplatRandom</td><td align="right">784,300,000</td><td align="right">811,100,000</td><td align="right">1,314,600,000</td><td align="right">182,500,000</td><td align="right">410,200,000</td><td align="right">536,200,000</td><td align="right">465,700,000</td></tr>
    <tr><td>LinearCongruentialGenerator</td><td align="right">801,300,000</td><td align="right">874,300,000</td><td align="right">1,310,800,000</td><td align="right">182,700,000</td><td align="right">404,900,000</td><td align="right">578,000,000</td><td align="right">495,500,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">786,400,000</td><td align="right">704,100,000</td><td align="right">1,060,400,000</td><td align="right">182,000,000</td><td align="right">371,600,000</td><td align="right">479,500,000</td><td align="right">422,600,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">792,300,000</td><td align="right">654,300,000</td><td align="right">959,100,000</td><td align="right">182,200,000</td><td align="right">340,000,000</td><td align="right">482,100,000</td><td align="right">446,500,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">764,600,000</td><td align="right">650,400,000</td><td align="right">948,000,000</td><td align="right">183,700,000</td><td align="right">306,100,000</td><td align="right">444,800,000</td><td align="right">404,800,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">779,900,000</td><td align="right">650,300,000</td><td align="right">906,700,000</td><td align="right">182,200,000</td><td align="right">323,400,000</td><td align="right">455,000,000</td><td align="right">409,700,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">774,700,000</td><td align="right">589,000,000</td><td align="right">813,800,000</td><td align="right">178,000,000</td><td align="right">312,200,000</td><td align="right">444,000,000</td><td align="right">391,900,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">788,500,000</td><td align="right">588,900,000</td><td align="right">765,100,000</td><td align="right">166,000,000</td><td align="right">255,200,000</td><td align="right">427,800,000</td><td align="right">395,500,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">772,800,000</td><td align="right">543,800,000</td><td align="right">759,500,000</td><td align="right">167,100,000</td><td align="right">251,600,000</td><td align="right">423,500,000</td><td align="right">379,000,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">759,400,000</td><td align="right">559,300,000</td><td align="right">720,300,000</td><td align="right">182,500,000</td><td align="right">282,600,000</td><td align="right">405,900,000</td><td align="right">366,100,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">783,300,000</td><td align="right">558,200,000</td><td align="right">596,100,000</td><td align="right">182,000,000</td><td align="right">257,600,000</td><td align="right">440,200,000</td><td align="right">388,000,000</td></tr>
    <tr><td>WyRandom</td><td align="right">748,300,000</td><td align="right">384,100,000</td><td align="right">446,500,000</td><td align="right">163,200,000</td><td align="right">186,900,000</td><td align="right">293,500,000</td><td align="right">278,500,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">763,100,000</td><td align="right">395,600,000</td><td align="right">414,300,000</td><td align="right">172,300,000</td><td align="right">190,300,000</td><td align="right">333,200,000</td><td align="right">313,200,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">704,100,000</td><td align="right">243,500,000</td><td align="right">265,200,000</td><td align="right">120,600,000</td><td align="right">120,900,000</td><td align="right">212,800,000</td><td align="right">205,600,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">654,200,000</td><td align="right">84,800,000</td><td align="right">87,700,000</td><td align="right">61,600,000</td><td align="right">40,900,000</td><td align="right">80,900,000</td><td align="right">81,400,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">146,500,000</td><td align="right">148,500,000</td><td align="right">64,900,000</td><td align="right">129,400,000</td><td align="right">129,300,000</td><td align="right">59,500,000</td><td align="right">60,300,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">535,800,000</td><td align="right">54,500,000</td><td align="right">57,200,000</td><td align="right">45,900,000</td><td align="right">27,200,000</td><td align="right">53,400,000</td><td align="right">53,300,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->
