# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-12-01 04:04 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |          998,472 |    5,593,486 |      0.18x |  1,000,000 |
| Parent - Array    |          710,259 |    3,303,583 |      0.21x |    720,000 |
| Parent - List     |          812,837 |    4,270,898 |      0.19x |    820,000 |
| Parent - HashSet  |          789,078 |    2,980,434 |      0.26x |    790,000 |
| Child - Single    |        1,150,685 |    3,627,636 |      0.32x |  1,160,000 |
| Child - Array     |          269,269 |    2,318,881 |      0.12x |    270,000 |
| Child - List      |          253,670 |    2,598,777 |      0.10x |    260,000 |
| Child - HashSet   |          255,140 |    1,745,226 |      0.15x |    260,000 |
| Sibling - Single  |        3,798,195 |   14,330,652 |      0.27x |  3,800,000 |
| Sibling - Array   |          947,969 |    2,589,505 |      0.37x |    950,000 |
| Sibling - List    |        1,238,554 |    3,382,219 |      0.37x |  1,240,000 |
| Sibling - HashSet |        1,211,560 |    1,852,609 |      0.65x |  1,220,000 |

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
