# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-11-25 23:00 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |          975,470 |    5,530,770 |      0.18x |    980,000 |
| Parent - Array    |          634,831 |    3,324,226 |      0.19x |    640,000 |
| Parent - List     |          729,325 |    4,123,577 |      0.18x |    730,000 |
| Parent - HashSet  |          740,702 |    2,872,357 |      0.26x |    750,000 |
| Child - Single    |          838,257 |    3,496,053 |      0.24x |    840,000 |
| Child - Array     |          247,699 |    2,266,901 |      0.11x |    250,000 |
| Child - List      |          242,404 |    2,564,449 |      0.09x |    250,000 |
| Child - HashSet   |          239,222 |    1,697,862 |      0.14x |    240,000 |
| Sibling - Single  |        3,542,012 |   13,685,291 |      0.26x |  3,550,000 |
| Sibling - Array   |          887,430 |    2,380,543 |      0.37x |    890,000 |
| Sibling - List    |        1,138,700 |    3,299,650 |      0.35x |  1,140,000 |
| Sibling - HashSet |        1,127,812 |    1,812,435 |      0.62x |  1,130,000 |

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
