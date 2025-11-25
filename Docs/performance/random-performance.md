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
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,298,500,000</td><td>Fastest</td><td>Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions. <a href="https://doi.org/10.1145/63039.63042">Park &amp; Miller 1988</a></td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">1,297,000,000</td><td>Fastest</td><td>Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental. <a href="https://github.com/wileylooper/wavesplat">wileylooper/wavesplat</a></td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">1,045,200,000</td><td>Very Fast</td><td>Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator. <a href="https://github.com/wileylooper/blastcircuit">wileylooper/blastcircuit</a></td></tr>
    <tr><td>SplitMix64</td><td align="right">1,004,800,000</td><td>Very Fast</td><td>Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="http://xoshiro.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">884,200,000</td><td>Fast</td><td>Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">874,800,000</td><td>Fast</td><td>Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author. <a href="https://github.com/wileylooper/illusionflow">wileylooper/illusionflow</a></td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">845,000,000</td><td>Fast</td><td>Excellent</td><td>Hybrid Xoshiro/PCG variant tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="http://xoshiro.di.unimi.it">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">742,800,000</td><td>Fast</td><td>Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="http://xoshiro.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>RomuDuo</td><td align="right">721,600,000</td><td>Fast</td><td>Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations. <a href="https://romu-random.org/">Markus &amp; Crow 2019</a></td></tr>
    <tr><td>StormDropRandom</td><td align="right">692,300,000</td><td>Moderate</td><td>Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods. <a href="https://github.com/wileylooper/stormdrop">wileylooper/stormdrop</a></td></tr>
    <tr><td>XorShiftRandom</td><td align="right">588,900,000</td><td>Moderate</td><td>Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://www.jstatsoft.org/article/view/v008i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">438,600,000</td><td>Slow</td><td>Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">409,200,000</td><td>Slow</td><td>Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://github.com/squirrel-org/squirrel3">Squirrel3</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">213,900,000</td><td>Very Slow</td><td>Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties. <a href="https://github.com/wileylooper/photonspin">wileylooper/photonspin</a></td></tr>
    <tr><td>UnityRandom</td><td align="right">85,000,000</td><td>Very Slow</td><td>Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://blog.unity.com/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">63,600,000</td><td>Very Slow</td><td>Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">55,700,000</td><td>Very Slow</td><td>Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
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
    <tr><td>LinearCongruentialGenerator</td><td align="right">761,800,000</td><td align="right">810,500,000</td><td align="right">1,298,500,000</td><td align="right">184,600,000</td><td align="right">375,200,000</td><td align="right">583,600,000</td><td align="right">488,600,000</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">769,800,000</td><td align="right">753,500,000</td><td align="right">1,297,000,000</td><td align="right">188,000,000</td><td align="right">403,900,000</td><td align="right">525,900,000</td><td align="right">457,400,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">758,900,000</td><td align="right">723,900,000</td><td align="right">1,045,200,000</td><td align="right">185,400,000</td><td align="right">362,500,000</td><td align="right">476,100,000</td><td align="right">420,600,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">761,600,000</td><td align="right">718,200,000</td><td align="right">1,004,800,000</td><td align="right">183,800,000</td><td align="right">350,700,000</td><td align="right">468,300,000</td><td align="right">428,900,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">756,900,000</td><td align="right">662,800,000</td><td align="right">884,200,000</td><td align="right">183,900,000</td><td align="right">331,600,000</td><td align="right">444,300,000</td><td align="right">404,500,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">763,700,000</td><td align="right">644,800,000</td><td align="right">874,800,000</td><td align="right">180,900,000</td><td align="right">318,000,000</td><td align="right">437,900,000</td><td align="right">382,600,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">745,900,000</td><td align="right">609,300,000</td><td align="right">845,000,000</td><td align="right">181,800,000</td><td align="right">293,300,000</td><td align="right">431,500,000</td><td align="right">390,400,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">766,600,000</td><td align="right">566,100,000</td><td align="right">742,800,000</td><td align="right">164,800,000</td><td align="right">243,000,000</td><td align="right">409,600,000</td><td align="right">363,500,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">746,500,000</td><td align="right">561,700,000</td><td align="right">721,600,000</td><td align="right">166,900,000</td><td align="right">243,300,000</td><td align="right">425,900,000</td><td align="right">377,000,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">748,800,000</td><td align="right">545,300,000</td><td align="right">692,300,000</td><td align="right">183,300,000</td><td align="right">274,700,000</td><td align="right">387,400,000</td><td align="right">358,900,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">763,400,000</td><td align="right">542,500,000</td><td align="right">588,900,000</td><td align="right">183,600,000</td><td align="right">256,100,000</td><td align="right">474,200,000</td><td align="right">385,000,000</td></tr>
    <tr><td>WyRandom</td><td align="right">733,800,000</td><td align="right">371,100,000</td><td align="right">438,600,000</td><td align="right">164,800,000</td><td align="right">180,000,000</td><td align="right">282,700,000</td><td align="right">276,000,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">743,000,000</td><td align="right">387,500,000</td><td align="right">409,200,000</td><td align="right">175,100,000</td><td align="right">185,000,000</td><td align="right">357,400,000</td><td align="right">306,800,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">670,700,000</td><td align="right">232,800,000</td><td align="right">213,900,000</td><td align="right">117,700,000</td><td align="right">115,800,000</td><td align="right">207,800,000</td><td align="right">202,700,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">630,200,000</td><td align="right">81,700,000</td><td align="right">85,000,000</td><td align="right">61,200,000</td><td align="right">40,700,000</td><td align="right">80,300,000</td><td align="right">79,500,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">145,600,000</td><td align="right">142,600,000</td><td align="right">63,600,000</td><td align="right">129,600,000</td><td align="right">135,500,000</td><td align="right">56,600,000</td><td align="right">57,400,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">528,400,000</td><td align="right">54,500,000</td><td align="right">55,700,000</td><td align="right">45,300,000</td><td align="right">26,900,000</td><td align="right">52,800,000</td><td align="right">51,500,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->
