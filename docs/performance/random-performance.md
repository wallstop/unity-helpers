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
    <tr><td>LinearCongruentialGenerator</td><td align="right">1,310,400,000</td><td>Fastest</td><td>Poor</td><td>Minimal standard LCG; fails spectral tests and exhibits lattice artifacts beyond small dimensions. <a href="https://doi.org/10.1145/63039.63042">Park &amp; Miller 1988</a></td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">1,164,400,000</td><td>Very Fast</td><td>Experimental</td><td>Single-word chaotic generator; author notes period 2^64 but provides no formal test results—treat as experimental. <a href="https://github.com/wileylooper/wavesplat">wileylooper/wavesplat</a></td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">957,800,000</td><td>Fast</td><td>Good</td><td>Empirical PractRand testing to 32GB shows strong diffusion; designed as a chaotic ARX mixer rather than a proven statistically optimal generator. <a href="https://github.com/wileylooper/blastcircuit">wileylooper/blastcircuit</a></td></tr>
    <tr><td>SplitMix64</td><td align="right">936,800,000</td><td>Fast</td><td>Very Good</td><td>Well-known SplitMix64 mixer; passes TestU01 BigCrush and PractRand up to large data sizes in literature. <a href="http://xoshiro.di.unimi.it/splitmix64.c">Vigna 2014</a></td></tr>
    <tr><td>PcgRandom</td><td align="right">893,400,000</td><td>Fast</td><td>Excellent</td><td>PCG XSH RR 64/32 variant; passes TestU01 BigCrush and PractRand in published results. <a href="https://www.pcg-random.org/paper.html">O&#39;Neill 2014</a></td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">863,400,000</td><td>Fast</td><td>Excellent</td><td>Hybrid Xoshiro/PCG variant tuned for all-around use; passes TestU01 BigCrush per upstream reference implementation. <a href="http://xoshiro.di.unimi.it">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>IllusionFlow</td><td align="right">794,800,000</td><td>Fast</td><td>Excellent</td><td>Hybridized PCG + xorshift design; upstream PractRand 64GB passes with no anomalies per author. <a href="https://github.com/wileylooper/illusionflow">wileylooper/illusionflow</a></td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">754,400,000</td><td>Fast</td><td>Very Good</td><td>xoshiro128** variant; authors recommend for general-purpose use and report clean BigCrush performance with jump functions. <a href="http://xoshiro.di.unimi.it/xoshiro128starstar.c">Blackman &amp; Vigna 2019</a></td></tr>
    <tr><td>RomuDuo</td><td align="right">753,300,000</td><td>Fast</td><td>Very Good</td><td>ROMU family member (RomuDuo); authors report strong BigCrush results with minor low-bit weaknesses in some rotations. <a href="https://romu-random.org/">Markus &amp; Crow 2019</a></td></tr>
    <tr><td>StormDropRandom</td><td align="right">715,200,000</td><td>Moderate</td><td>Excellent</td><td>20-word ARX generator derived from SHISHUA; author reports excellent PractRand performance and long periods. <a href="https://github.com/wileylooper/stormdrop">wileylooper/stormdrop</a></td></tr>
    <tr><td>XorShiftRandom</td><td align="right">589,800,000</td><td>Moderate</td><td>Fair</td><td>Classic 32-bit xorshift; known to fail portions of TestU01 and PractRand, acceptable for lightweight effects only. <a href="https://www.jstatsoft.org/article/view/v008i14">Marsaglia 2003</a></td></tr>
    <tr><td>WyRandom</td><td align="right">429,100,000</td><td>Slow</td><td>Very Good</td><td>Wyhash-based generator; published testing shows it clears BigCrush/PractRand with wide seed coverage. <a href="https://github.com/wangyi-fudan/wyhash">Wang Yi 2019</a></td></tr>
    <tr><td>SquirrelRandom</td><td align="right">408,900,000</td><td>Slow</td><td>Good</td><td>Hash-based generator built on Squirrel3; good equidistribution for table lookups but not extensively tested beyond moderate ranges. <a href="https://github.com/squirrel-org/squirrel3">Squirrel3</a></td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">261,600,000</td><td>Very Slow</td><td>Excellent</td><td>SHISHUA-inspired generator; independent testing (PractRand 128GB) by author indicates excellent distribution properties. <a href="https://github.com/wileylooper/photonspin">wileylooper/photonspin</a></td></tr>
    <tr><td>UnityRandom</td><td align="right">87,400,000</td><td>Very Slow</td><td>Fair</td><td>Mirrors UnityEngine.Random (Xorshift196 + additive); suitable for legacy compatibility but not high-stakes simulation. <a href="https://blog.unity.com/technology/random-numbers-on-the-gpu">Unity Random Internals</a></td></tr>
    <tr><td>SystemRandom</td><td align="right">59,400,000</td><td>Very Slow</td><td>Poor</td><td>Thin wrapper over System.Random; inherits same LCG weaknesses and fails modern statistical batteries. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
    <tr><td>DotNetRandom</td><td align="right">49,700,000</td><td>Very Slow</td><td>Poor</td><td>Linear congruential generator (mod 2^31) with known correlation failures; unsuitable for high-quality simulations. <a href="https://nullprogram.com/blog/2017/09/21/">System.Random considered harmful</a></td></tr>
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
    <tr><td>LinearCongruentialGenerator</td><td align="right">787,900,000</td><td align="right">871,200,000</td><td align="right">1,310,400,000</td><td align="right">187,800,000</td><td align="right">405,000,000</td><td align="right">584,200,000</td><td align="right">500,900,000</td></tr>
    <tr><td>WaveSplatRandom</td><td align="right">782,600,000</td><td align="right">722,800,000</td><td align="right">1,164,400,000</td><td align="right">187,800,000</td><td align="right">380,300,000</td><td align="right">525,900,000</td><td align="right">457,300,000</td></tr>
    <tr><td>BlastCircuitRandom</td><td align="right">787,900,000</td><td align="right">683,400,000</td><td align="right">957,800,000</td><td align="right">186,900,000</td><td align="right">323,800,000</td><td align="right">467,100,000</td><td align="right">412,700,000</td></tr>
    <tr><td>SplitMix64</td><td align="right">773,500,000</td><td align="right">736,800,000</td><td align="right">936,800,000</td><td align="right">184,000,000</td><td align="right">342,400,000</td><td align="right">467,100,000</td><td align="right">429,400,000</td></tr>
    <tr><td>PcgRandom</td><td align="right">788,800,000</td><td align="right">674,100,000</td><td align="right">893,400,000</td><td align="right">184,700,000</td><td align="right">331,100,000</td><td align="right">447,600,000</td><td align="right">404,500,000</td></tr>
    <tr><td>FlurryBurstRandom</td><td align="right">780,600,000</td><td align="right">518,200,000</td><td align="right">863,400,000</td><td align="right">181,000,000</td><td align="right">313,900,000</td><td align="right">375,000,000</td><td align="right">347,300,000</td></tr>
    <tr><td>IllusionFlow</td><td align="right">779,300,000</td><td align="right">647,400,000</td><td align="right">794,800,000</td><td align="right">177,900,000</td><td align="right">304,400,000</td><td align="right">438,400,000</td><td align="right">389,700,000</td></tr>
    <tr><td>XoroShiroRandom</td><td align="right">746,100,000</td><td align="right">574,200,000</td><td align="right">754,400,000</td><td align="right">169,700,000</td><td align="right">248,500,000</td><td align="right">421,100,000</td><td align="right">376,700,000</td></tr>
    <tr><td>RomuDuo</td><td align="right">776,800,000</td><td align="right">584,900,000</td><td align="right">753,300,000</td><td align="right">168,200,000</td><td align="right">248,900,000</td><td align="right">434,700,000</td><td align="right">384,400,000</td></tr>
    <tr><td>StormDropRandom</td><td align="right">743,700,000</td><td align="right">470,900,000</td><td align="right">715,200,000</td><td align="right">188,400,000</td><td align="right">231,800,000</td><td align="right">384,200,000</td><td align="right">332,400,000</td></tr>
    <tr><td>XorShiftRandom</td><td align="right">744,000,000</td><td align="right">554,600,000</td><td align="right">589,800,000</td><td align="right">183,000,000</td><td align="right">245,300,000</td><td align="right">430,600,000</td><td align="right">380,700,000</td></tr>
    <tr><td>WyRandom</td><td align="right">750,900,000</td><td align="right">381,000,000</td><td align="right">429,100,000</td><td align="right">167,400,000</td><td align="right">181,800,000</td><td align="right">287,800,000</td><td align="right">277,600,000</td></tr>
    <tr><td>SquirrelRandom</td><td align="right">753,400,000</td><td align="right">395,300,000</td><td align="right">408,900,000</td><td align="right">177,700,000</td><td align="right">199,300,000</td><td align="right">358,100,000</td><td align="right">335,500,000</td></tr>
    <tr><td>PhotonSpinRandom</td><td align="right">726,900,000</td><td align="right">242,900,000</td><td align="right">261,600,000</td><td align="right">118,900,000</td><td align="right">114,700,000</td><td align="right">214,200,000</td><td align="right">201,800,000</td></tr>
    <tr><td>UnityRandom</td><td align="right">638,400,000</td><td align="right">83,500,000</td><td align="right">87,400,000</td><td align="right">62,700,000</td><td align="right">40,700,000</td><td align="right">80,400,000</td><td align="right">80,800,000</td></tr>
    <tr><td>SystemRandom</td><td align="right">114,200,000</td><td align="right">144,400,000</td><td align="right">59,400,000</td><td align="right">109,000,000</td><td align="right">111,500,000</td><td align="right">58,500,000</td><td align="right">58,900,000</td></tr>
    <tr><td>DotNetRandom</td><td align="right">531,800,000</td><td align="right">54,400,000</td><td align="right">49,700,000</td><td align="right">46,700,000</td><td align="right">26,800,000</td><td align="right">53,500,000</td><td align="right">53,400,000</td></tr>
  </tbody>
</table>
<!-- RANDOM_BENCHMARKS_END -->
