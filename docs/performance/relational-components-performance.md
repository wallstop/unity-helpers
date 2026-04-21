# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2026-04-21 03:19 UTC on Windows 11 (10.0.26200) 64bit_

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
    <tr><td align="left">Parent - Single</td><td align="right">743,472</td><td align="right">5,713,435</td><td align="right">0.13x</td><td align="right">750,000</td></tr>
    <tr><td align="left">Parent - Array</td><td align="right">448,986</td><td align="right">3,369,056</td><td align="right">0.13x</td><td align="right">450,000</td></tr>
    <tr><td align="left">Parent - List</td><td align="right">489,226</td><td align="right">4,282,326</td><td align="right">0.11x</td><td align="right">490,000</td></tr>
    <tr><td align="left">Parent - HashSet</td><td align="right">492,256</td><td align="right">2,897,865</td><td align="right">0.17x</td><td align="right">500,000</td></tr>
    <tr><td align="left">Child - Single</td><td align="right">493,912</td><td align="right">3,549,388</td><td align="right">0.14x</td><td align="right">500,000</td></tr>
    <tr><td align="left">Child - Array</td><td align="right">187,391</td><td align="right">2,449,558</td><td align="right">0.08x</td><td align="right">190,000</td></tr>
    <tr><td align="left">Child - List</td><td align="right">192,235</td><td align="right">2,595,599</td><td align="right">0.07x</td><td align="right">200,000</td></tr>
    <tr><td align="left">Child - HashSet</td><td align="right">194,009</td><td align="right">1,705,553</td><td align="right">0.11x</td><td align="right">200,000</td></tr>
    <tr><td align="left">Sibling - Single</td><td align="right">3,831,725</td><td align="right">14,540,578</td><td align="right">0.26x</td><td align="right">3,840,000</td></tr>
    <tr><td align="left">Sibling - Array</td><td align="right">704,704</td><td align="right">2,604,095</td><td align="right">0.27x</td><td align="right">710,000</td></tr>
    <tr><td align="left">Sibling - List</td><td align="right">841,554</td><td align="right">3,152,693</td><td align="right">0.27x</td><td align="right">850,000</td></tr>
    <tr><td align="left">Sibling - HashSet</td><td align="right">825,542</td><td align="right">1,871,076</td><td align="right">0.44x</td><td align="right">830,000</td></tr>
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
