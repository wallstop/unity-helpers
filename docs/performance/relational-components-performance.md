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

_Last updated 2025-12-28 04:08 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |          969,815 |    5,595,619 |      0.17x |    970,000 |
| Parent - Array    |          658,178 |    3,295,832 |      0.20x |    660,000 |
| Parent - List     |          725,457 |    4,218,240 |      0.17x |    730,000 |
| Parent - HashSet  |          711,530 |    2,891,677 |      0.25x |    720,000 |
| Child - Single    |          966,474 |    3,563,710 |      0.27x |    970,000 |
| Child - Array     |          252,799 |    2,435,614 |      0.10x |    260,000 |
| Child - List      |          238,846 |    2,548,281 |      0.09x |    240,000 |
| Child - HashSet   |          237,971 |    1,703,023 |      0.14x |    240,000 |
| Sibling - Single  |        3,794,146 |   14,356,432 |      0.26x |  3,800,000 |
| Sibling - Array   |          900,523 |    2,587,384 |      0.35x |    910,000 |
| Sibling - List    |        1,158,435 |    3,339,996 |      0.35x |  1,160,000 |
| Sibling - HashSet |        1,119,343 |    1,819,796 |      0.62x |  1,120,000 |

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
