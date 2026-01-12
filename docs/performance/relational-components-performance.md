---
---

# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2026-01-12 01:51 UTC on Windows 11 (10.0.26200) 64bit_

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
    <tr><td align="left">Parent - Single</td><td align="right">9,767</td><td align="right">5,654,126</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Parent - Array</td><td align="right">2,916</td><td align="right">3,311,542</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Parent - List</td><td align="right">2,899</td><td align="right">4,236,790</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Parent - HashSet</td><td align="right">2,934</td><td align="right">2,871,959</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Child - Single</td><td align="right">2,672</td><td align="right">3,554,134</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Child - Array</td><td align="right">1,452</td><td align="right">2,312,928</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Child - List</td><td align="right">1,907</td><td align="right">2,576,993</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Child - HashSet</td><td align="right">1,914</td><td align="right">1,705,880</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Sibling - Single</td><td align="right">3,687,340</td><td align="right">14,312,500</td><td align="right">0.26x</td><td align="right">3,690,000</td></tr>
    <tr><td align="left">Sibling - Array</td><td align="right">5,831</td><td align="right">2,491,710</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Sibling - List</td><td align="right">5,761</td><td align="right">3,383,640</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
    <tr><td align="left">Sibling - HashSet</td><td align="right">5,827</td><td align="right">1,829,998</td><td align="right">0.00x</td><td align="right">10,000</td></tr>
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
