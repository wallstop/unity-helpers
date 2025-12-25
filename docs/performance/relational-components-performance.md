# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-12-25 07:11 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,039,815 |    5,660,407 |      0.18x |  1,040,000 |
| Parent - Array    |          721,101 |    3,319,570 |      0.22x |    730,000 |
| Parent - List     |          810,960 |    4,225,889 |      0.19x |    820,000 |
| Parent - HashSet  |          776,119 |    2,892,488 |      0.27x |    780,000 |
| Child - Single    |        1,104,282 |    3,591,094 |      0.31x |  1,110,000 |
| Child - Array     |          267,550 |    2,438,317 |      0.11x |    270,000 |
| Child - List      |          247,370 |    2,576,128 |      0.10x |    250,000 |
| Child - HashSet   |          248,874 |    1,714,371 |      0.15x |    250,000 |
| Sibling - Single  |        3,807,941 |   14,422,212 |      0.26x |  3,810,000 |
| Sibling - Array   |          953,300 |    2,567,381 |      0.37x |    960,000 |
| Sibling - List    |        1,237,070 |    3,327,139 |      0.37x |  1,240,000 |
| Sibling - HashSet |        1,195,260 |    1,811,384 |      0.66x |  1,200,000 |

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
