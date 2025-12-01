# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-12-01 01:25 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,061,609 |    5,627,814 |      0.19x |  1,070,000 |
| Parent - Array    |          714,827 |    3,233,627 |      0.22x |    720,000 |
| Parent - List     |          798,328 |    4,192,037 |      0.19x |    800,000 |
| Parent - HashSet  |          794,752 |    2,877,773 |      0.28x |    800,000 |
| Child - Single    |        1,137,549 |    3,495,489 |      0.33x |  1,140,000 |
| Child - Array     |          267,972 |    2,303,626 |      0.12x |    270,000 |
| Child - List      |          248,851 |    2,541,149 |      0.10x |    250,000 |
| Child - HashSet   |          255,507 |    1,709,650 |      0.15x |    260,000 |
| Sibling - Single  |        3,858,125 |   14,544,187 |      0.27x |  3,860,000 |
| Sibling - Array   |          947,062 |    2,105,823 |      0.45x |    950,000 |
| Sibling - List    |        1,219,182 |    3,387,072 |      0.36x |  1,220,000 |
| Sibling - HashSet |        1,208,001 |    1,747,411 |      0.69x |  1,210,000 |

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
