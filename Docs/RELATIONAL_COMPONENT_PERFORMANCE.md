# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-11-01 02:11 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,009,079 |    5,700,123 |      0.18x |  1,010,000 |
| Parent - Array    |          676,149 |    3,327,317 |      0.20x |    680,000 |
| Parent - List     |          766,704 |    4,238,343 |      0.18x |    770,000 |
| Parent - HashSet  |          746,757 |    2,906,359 |      0.26x |    750,000 |
| Child - Single    |          948,288 |    3,562,341 |      0.27x |    950,000 |
| Child - Array     |          263,898 |    2,357,185 |      0.11x |    270,000 |
| Child - List      |          247,098 |    2,623,979 |      0.09x |    250,000 |
| Child - HashSet   |          247,462 |    1,708,595 |      0.14x |    250,000 |
| Sibling - Single  |        3,855,983 |   14,542,964 |      0.27x |  3,860,000 |
| Sibling - Array   |          925,080 |    2,537,309 |      0.36x |    930,000 |
| Sibling - List    |        1,170,239 |    3,356,368 |      0.35x |  1,180,000 |
| Sibling - HashSet |        1,137,739 |    1,773,824 |      0.64x |  1,140,000 |

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
