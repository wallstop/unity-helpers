# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-12-08 06:27 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,026,493 |    5,447,240 |      0.19x |  1,030,000 |
| Parent - Array    |          720,133 |    3,307,603 |      0.22x |    730,000 |
| Parent - List     |          799,709 |    4,174,800 |      0.19x |    800,000 |
| Parent - HashSet  |          793,143 |    2,913,347 |      0.27x |    800,000 |
| Child - Single    |        1,157,816 |    3,510,479 |      0.33x |  1,160,000 |
| Child - Array     |          268,178 |    2,261,733 |      0.12x |    270,000 |
| Child - List      |          253,755 |    2,610,476 |      0.10x |    260,000 |
| Child - HashSet   |          252,462 |    1,676,820 |      0.15x |    260,000 |
| Sibling - Single  |        3,765,117 |   14,294,375 |      0.26x |  3,770,000 |
| Sibling - Array   |          973,518 |    2,472,567 |      0.39x |    980,000 |
| Sibling - List    |        1,234,033 |    3,302,100 |      0.37x |  1,240,000 |
| Sibling - HashSet |        1,198,421 |    1,830,203 |      0.65x |  1,200,000 |

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
