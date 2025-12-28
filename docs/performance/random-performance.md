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
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,309,300,000</td><td>Fastest</td><td>Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions.</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">1,159,700,000</td><td>Very Fast</td><td>Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental.</td></tr>
    <tr><td>SplitMix64</td><td align="right">1,048,800,000</td><td>Very Fast</td><td>Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="http://xoshiro.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">1,037,300,000</td><td>Very Fast</td><td>Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator.</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">938,900,000</td><td>Fast</td><td>Excellent</td><td>Hybrid Xoshiro/PCG variant tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="http://xoshiro.di.unimi.it">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">905,200,000</td><td>Fast</td><td>Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">882,600,000</td><td>Fast</td><td>Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author.</td></tr>
    <tr><td>RomuDuo</td><td align="right">757,300,000</td><td>Fast</td><td>Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations.</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">755,300,000</td><td>Fast</td><td>Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="http://xoshiro.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>StormDropRandom</td><td align="right">699,700,000</td><td>Moderate</td><td>Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods.</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">593,700,000</td><td>Moderate</td><td>Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://www.jstatsoft.org/article/view/v008i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">448,600,000</td><td>Slow</td><td>Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">401,900,000</td><td>Slow</td><td>Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://youtu.be/LWFzPP8ZbdU?t=2673">Squirrel Eiserloh</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">260,200,000</td><td>Very Slow</td><td>Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties.</td></tr>
    <tr><td>UnityRandom</td><td align="right">84,200,000</td><td>Very Slow</td><td>Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://blog.unity.com/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">63,500,000</td><td>Very Slow</td><td>Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">58,200,000</td><td>Very Slow</td><td>Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
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
    <tr><td>LinearCongruentialGenerator</td><td align="right">796,900,000</td><td align="right">832,800,000</td><td align="right">1,309,300,000</td><td align="right">210,800,000</td><td align="right">408,400,000</td><td align="right">586,000,000</td><td align="right">502,500,000</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">778,800,000</td><td align="right">792,200,000</td><td align="right">1,159,700,000</td><td align="right">204,400,000</td><td align="right">375,300,000</td><td align="right">515,100,000</td><td align="right">454,700,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">776,700,000</td><td align="right">751,900,000</td><td align="right">1,048,800,000</td><td align="right">210,200,000</td><td align="right">367,100,000</td><td align="right">479,400,000</td><td align="right">439,800,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">767,700,000</td><td align="right">704,200,000</td><td align="right">1,037,300,000</td><td align="right">207,800,000</td><td align="right">330,500,000</td><td align="right">468,300,000</td><td align="right">416,200,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">775,300,000</td><td align="right">642,400,000</td><td align="right">938,900,000</td><td align="right">210,500,000</td><td align="right">332,300,000</td><td align="right">446,500,000</td><td align="right">406,900,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">780,500,000</td><td align="right">656,800,000</td><td align="right">905,200,000</td><td align="right">205,900,000</td><td align="right">334,400,000</td><td align="right">450,400,000</td><td align="right">408,500,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">778,400,000</td><td align="right">631,100,000</td><td align="right">882,600,000</td><td align="right">202,800,000</td><td align="right">320,100,000</td><td align="right">442,100,000</td><td align="right">391,700,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">773,500,000</td><td align="right">581,000,000</td><td align="right">757,300,000</td><td align="right">187,500,000</td><td align="right">251,600,000</td><td align="right">439,100,000</td><td align="right">376,300,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">762,900,000</td><td align="right">584,600,000</td><td align="right">755,300,000</td><td align="right">186,000,000</td><td align="right">249,600,000</td><td align="right">420,200,000</td><td align="right">372,300,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">754,700,000</td><td align="right">548,500,000</td><td align="right">699,700,000</td><td align="right">206,400,000</td><td align="right">248,700,000</td><td align="right">398,000,000</td><td align="right">353,200,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">771,000,000</td><td align="right">580,900,000</td><td align="right">593,700,000</td><td align="right">208,300,000</td><td align="right">246,400,000</td><td align="right">437,500,000</td><td align="right">380,900,000</td></tr>
    <tr><td>WyRandom</td><td align="right">744,500,000</td><td align="right">384,500,000</td><td align="right">448,600,000</td><td align="right">181,800,000</td><td align="right">185,600,000</td><td align="right">288,100,000</td><td align="right">276,700,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">766,500,000</td><td align="right">388,600,000</td><td align="right">401,900,000</td><td align="right">192,500,000</td><td align="right">197,600,000</td><td align="right">355,700,000</td><td align="right">330,700,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">693,700,000</td><td align="right">247,300,000</td><td align="right">260,200,000</td><td align="right">129,200,000</td><td align="right">113,400,000</td><td align="right">212,200,000</td><td align="right">201,800,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">636,500,000</td><td align="right">82,000,000</td><td align="right">84,200,000</td><td align="right">63,500,000</td><td align="right">39,600,000</td><td align="right">78,900,000</td><td align="right">78,600,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">140,300,000</td><td align="right">145,000,000</td><td align="right">63,500,000</td><td align="right">129,300,000</td><td align="right">135,600,000</td><td align="right">58,500,000</td><td align="right">58,200,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">538,000,000</td><td align="right">55,200,000</td><td align="right">58,200,000</td><td align="right">48,300,000</td><td align="right">27,600,000</td><td align="right">53,500,000</td><td align="right">54,100,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->
