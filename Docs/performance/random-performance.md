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
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,305,200,000</td><td>Fastest</td><td>Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions. <a href="https://doi.org/10.1145/63039.63042">Park &amp; Miller 1988</a></td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">1,145,200,000</td><td>Very Fast</td><td>Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental. <a href="https://github.com/wileylooper/wavesplat">wileylooper/wavesplat</a></td></tr>
    <tr><td>SplitMix64</td><td align="right">950,100,000</td><td>Fast</td><td>Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="http://xoshiro.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">933,400,000</td><td>Fast</td><td>Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator. <a href="https://github.com/wileylooper/blastcircuit">wileylooper/blastcircuit</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">907,700,000</td><td>Fast</td><td>Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">877,400,000</td><td>Fast</td><td>Excellent</td><td>Hybrid Xoshiro/PCG variant tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="http://xoshiro.di.unimi.it">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">806,500,000</td><td>Fast</td><td>Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author. <a href="https://github.com/wileylooper/illusionflow">wileylooper/illusionflow</a></td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">747,000,000</td><td>Fast</td><td>Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="http://xoshiro.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>RomuDuo</td><td align="right">740,700,000</td><td>Fast</td><td>Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations. <a href="https://romu-random.org/">Markus &amp; Crow 2019</a></td></tr>
    <tr><td>StormDropRandom</td><td align="right">717,100,000</td><td>Moderate</td><td>Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods. <a href="https://github.com/wileylooper/stormdrop">wileylooper/stormdrop</a></td></tr>
    <tr><td>XorShiftRandom</td><td align="right">602,600,000</td><td>Moderate</td><td>Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://www.jstatsoft.org/article/view/v008i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">437,200,000</td><td>Slow</td><td>Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">412,200,000</td><td>Slow</td><td>Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://github.com/squirrel-org/squirrel3">Squirrel3</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">269,200,000</td><td>Slow</td><td>Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties. <a href="https://github.com/wileylooper/photonspin">wileylooper/photonspin</a></td></tr>
    <tr><td>UnityRandom</td><td align="right">86,400,000</td><td>Very Slow</td><td>Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://blog.unity.com/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">64,000,000</td><td>Very Slow</td><td>Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">57,700,000</td><td>Very Slow</td><td>Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
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
    <tr><td>LinearCongruentialGenerator</td><td align="right">787,100,000</td><td align="right">866,400,000</td><td align="right">1,305,200,000</td><td align="right">209,500,000</td><td align="right">404,700,000</td><td align="right">584,400,000</td><td align="right">501,200,000</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">792,100,000</td><td align="right">718,300,000</td><td align="right">1,145,200,000</td><td align="right">207,700,000</td><td align="right">373,300,000</td><td align="right">525,300,000</td><td align="right">457,500,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">720,700,000</td><td align="right">748,900,000</td><td align="right">950,100,000</td><td align="right">209,100,000</td><td align="right">356,700,000</td><td align="right">475,700,000</td><td align="right">434,300,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">775,300,000</td><td align="right">665,600,000</td><td align="right">933,400,000</td><td align="right">207,500,000</td><td align="right">329,800,000</td><td align="right">478,100,000</td><td align="right">420,700,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">787,400,000</td><td align="right">679,300,000</td><td align="right">907,700,000</td><td align="right">211,200,000</td><td align="right">332,800,000</td><td align="right">446,400,000</td><td align="right">400,200,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">783,700,000</td><td align="right">677,100,000</td><td align="right">877,400,000</td><td align="right">209,600,000</td><td align="right">328,100,000</td><td align="right">448,100,000</td><td align="right">404,800,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">790,000,000</td><td align="right">648,900,000</td><td align="right">806,500,000</td><td align="right">200,300,000</td><td align="right">313,700,000</td><td align="right">434,300,000</td><td align="right">386,500,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">772,500,000</td><td align="right">571,300,000</td><td align="right">747,000,000</td><td align="right">188,300,000</td><td align="right">249,000,000</td><td align="right">421,700,000</td><td align="right">375,700,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">778,100,000</td><td align="right">575,500,000</td><td align="right">740,700,000</td><td align="right">185,300,000</td><td align="right">251,900,000</td><td align="right">438,600,000</td><td align="right">390,200,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">756,700,000</td><td align="right">471,600,000</td><td align="right">717,100,000</td><td align="right">208,900,000</td><td align="right">234,200,000</td><td align="right">384,800,000</td><td align="right">329,300,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">785,700,000</td><td align="right">564,000,000</td><td align="right">602,600,000</td><td align="right">212,500,000</td><td align="right">251,700,000</td><td align="right">436,500,000</td><td align="right">383,100,000</td></tr>
    <tr><td>WyRandom</td><td align="right">754,900,000</td><td align="right">377,200,000</td><td align="right">437,200,000</td><td align="right">186,100,000</td><td align="right">183,800,000</td><td align="right">292,600,000</td><td align="right">278,300,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">773,700,000</td><td align="right">408,700,000</td><td align="right">412,200,000</td><td align="right">191,600,000</td><td align="right">198,100,000</td><td align="right">357,500,000</td><td align="right">335,100,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">717,700,000</td><td align="right">244,200,000</td><td align="right">269,200,000</td><td align="right">130,900,000</td><td align="right">118,800,000</td><td align="right">215,600,000</td><td align="right">207,200,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">634,600,000</td><td align="right">83,500,000</td><td align="right">86,400,000</td><td align="right">64,400,000</td><td align="right">41,500,000</td><td align="right">81,700,000</td><td align="right">82,400,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">141,800,000</td><td align="right">148,100,000</td><td align="right">64,000,000</td><td align="right">131,800,000</td><td align="right">137,100,000</td><td align="right">58,500,000</td><td align="right">59,500,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">542,800,000</td><td align="right">55,400,000</td><td align="right">57,700,000</td><td align="right">47,700,000</td><td align="right">27,400,000</td><td align="right">55,400,000</td><td align="right">53,800,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->
