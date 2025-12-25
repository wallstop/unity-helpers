# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-12-09 01:28 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,044,928 |    5,632,251 |      0.19x |  1,050,000 |
| Parent - Array    |          729,279 |    3,351,363 |      0.22x |    730,000 |
| Parent - List     |          793,200 |    4,217,894 |      0.19x |    800,000 |
| Parent - HashSet  |          801,298 |    2,945,363 |      0.27x |    810,000 |
| Child - Single    |        1,138,687 |    3,596,362 |      0.32x |  1,140,000 |
| Child - Array     |          269,079 |    2,458,252 |      0.11x |    270,000 |
| Child - List      |          255,013 |    2,608,446 |      0.10x |    260,000 |
| Child - HashSet   |          246,441 |    1,704,844 |      0.14x |    250,000 |
| Sibling - Single  |        3,851,374 |   13,731,581 |      0.28x |  3,860,000 |
| Sibling - Array   |          980,940 |    2,485,294 |      0.39x |    990,000 |
| Sibling - List    |        1,246,924 |    3,308,514 |      0.38x |  1,250,000 |
| Sibling - HashSet |        1,204,150 |    1,839,997 |      0.65x |  1,210,000 |

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
