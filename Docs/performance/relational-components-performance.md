# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-11-22 03:10 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,012,966 |    5,644,492 |      0.18x |  1,020,000 |
| Parent - Array    |          678,090 |    3,298,281 |      0.21x |    680,000 |
| Parent - List     |          750,156 |    4,234,217 |      0.18x |    760,000 |
| Parent - HashSet  |          748,580 |    2,889,365 |      0.26x |    750,000 |
| Child - Single    |          917,000 |    3,484,254 |      0.26x |    920,000 |
| Child - Array     |          257,090 |    2,312,696 |      0.11x |    260,000 |
| Child - List      |          245,222 |    2,591,684 |      0.09x |    250,000 |
| Child - HashSet   |          244,837 |    1,679,880 |      0.15x |    250,000 |
| Sibling - Single  |        3,780,025 |   14,226,276 |      0.27x |  3,790,000 |
| Sibling - Array   |          909,200 |    2,443,672 |      0.37x |    910,000 |
| Sibling - List    |        1,164,233 |    3,244,523 |      0.36x |  1,170,000 |
| Sibling - HashSet |        1,131,318 |    1,734,872 |      0.65x |  1,140,000 |

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
