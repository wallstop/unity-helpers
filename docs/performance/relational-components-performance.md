# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2026-04-22 22:12 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Relational Ops/s</th>
      <th align="right">Manual Ops/s</th>
      <th align="right">Rel/Manual</th>
      <th align="right">Iterations</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Parent - Single</td><td align="right">725,544</td><td align="right">5,595,347</td><td align="right">0.13x</td><td align="right">730,000</td></tr>
    <tr><td align="left">Parent - Array</td><td align="right">449,950</td><td align="right">3,318,729</td><td align="right">0.14x</td><td align="right">450,000</td></tr>
    <tr><td align="left">Parent - List</td><td align="right">487,725</td><td align="right">4,217,996</td><td align="right">0.12x</td><td align="right">490,000</td></tr>
    <tr><td align="left">Parent - HashSet</td><td align="right">484,944</td><td align="right">2,899,413</td><td align="right">0.17x</td><td align="right">490,000</td></tr>
    <tr><td align="left">Child - Single</td><td align="right">492,106</td><td align="right">3,563,103</td><td align="right">0.14x</td><td align="right">500,000</td></tr>
    <tr><td align="left">Child - Array</td><td align="right">187,962</td><td align="right">2,440,233</td><td align="right">0.08x</td><td align="right">190,000</td></tr>
    <tr><td align="left">Child - List</td><td align="right">192,241</td><td align="right">2,586,416</td><td align="right">0.07x</td><td align="right">200,000</td></tr>
    <tr><td align="left">Child - HashSet</td><td align="right">192,582</td><td align="right">1,681,339</td><td align="right">0.11x</td><td align="right">200,000</td></tr>
    <tr><td align="left">Sibling - Single</td><td align="right">3,803,916</td><td align="right">14,394,475</td><td align="right">0.26x</td><td align="right">3,810,000</td></tr>
    <tr><td align="left">Sibling - Array</td><td align="right">685,594</td><td align="right">2,580,518</td><td align="right">0.27x</td><td align="right">690,000</td></tr>
    <tr><td align="left">Sibling - List</td><td align="right">823,158</td><td align="right">3,150,050</td><td align="right">0.26x</td><td align="right">830,000</td></tr>
    <tr><td align="left">Sibling - HashSet</td><td align="right">778,997</td><td align="right">1,835,821</td><td align="right">0.42x</td><td align="right">780,000</td></tr>
  </tbody>
</table>

<!-- RELATIONAL_COMPONENTS_WINDOWS_END -->

## macOS

<!-- RELATIONAL_COMPONENTS_MACOS_START -->

Pending — run the relational component benchmark suite on macOS to capture results.

<!-- RELATIONAL_COMPONENTS_MACOS_END -->

## Linux

<!-- RELATIONAL_COMPONENTS_LINUX_START -->

Pending — run the relational component benchmark suite on Linux to capture results.

<!-- RELATIONAL_COMPONENTS_LINUX_END -->

## Other Platforms

<!-- RELATIONAL_COMPONENTS_OTHER_START -->

Pending — run the relational component benchmark suite on the target platform to capture results.

<!-- RELATIONAL_COMPONENTS_OTHER_END -->
