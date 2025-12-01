# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-12-01 00:27 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,039,496 |    5,604,790 |      0.19x |  1,040,000 |
| Parent - Array    |          717,145 |    3,268,772 |      0.22x |    720,000 |
| Parent - List     |          797,918 |    4,271,532 |      0.19x |    800,000 |
| Parent - HashSet  |          814,231 |    2,926,216 |      0.28x |    820,000 |
| Child - Single    |        1,117,674 |    3,511,058 |      0.32x |  1,130,000 |
| Child - Array     |          272,189 |    2,343,154 |      0.12x |    280,000 |
| Child - List      |          249,373 |    2,562,684 |      0.10x |    250,000 |
| Child - HashSet   |          248,728 |    1,691,709 |      0.15x |    250,000 |
| Sibling - Single  |        3,861,760 |   14,263,045 |      0.27x |  3,870,000 |
| Sibling - Array   |          943,044 |    2,496,228 |      0.38x |    950,000 |
| Sibling - List    |        1,233,961 |    3,369,953 |      0.37x |  1,240,000 |
| Sibling - HashSet |        1,189,911 |    1,735,710 |      0.69x |  1,190,000 |

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
