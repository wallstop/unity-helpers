# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`), and compare the allocation footprint against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-10-31 21:18 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second and lower bytes per operation are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |          968,212 |    5,010,577 |      0.19x |    968,224 |
| Parent - Array    |          662,186 |    2,984,203 |      0.22x |    662,187 |
| Parent - List     |          741,504 |    2,809,643 |      0.26x |    741,505 |
| Parent - HashSet  |          731,353 |    2,689,417 |      0.27x |    731,353 |
| Child - Single    |          994,613 |    3,205,796 |      0.31x |    994,614 |
| Child - Array     |          260,156 |    2,131,930 |      0.12x |    260,157 |
| Child - List      |          231,785 |    1,710,938 |      0.14x |    231,786 |
| Child - HashSet   |          229,790 |    1,589,610 |      0.14x |    229,791 |
| Sibling - Single  |        3,636,086 |   10,729,084 |      0.34x |  3,636,086 |
| Sibling - Array   |          896,428 |    2,351,319 |      0.38x |    896,428 |
| Sibling - List    |        1,132,225 |    1,998,260 |      0.57x |  1,132,226 |
| Sibling - HashSet |        1,105,504 |    1,694,422 |      0.65x |  1,105,505 |

### Allocations per operation (bytes, lower is better)

| Scenario          | Relational (B/op) | Manual (B/op) | Manual-Rel (B/op) |
| ----------------- | ----------------: | ------------: | ----------------: |
| Parent - Single   |              0.00 |          0.00 |              0.00 |
| Parent - Array    |              0.00 |          0.00 |              0.00 |
| Parent - List     |              0.00 |          0.00 |              0.00 |
| Parent - HashSet  |              0.00 |          0.00 |              0.00 |
| Child - Single    |              0.00 |          0.00 |              0.00 |
| Child - Array     |              0.00 |          0.00 |              0.00 |
| Child - List      |              0.00 |          0.00 |              0.00 |
| Child - HashSet   |              0.00 |          0.00 |              0.00 |
| Sibling - Single  |              0.00 |          0.00 |              0.00 |
| Sibling - Array   |              0.00 |          0.00 |              0.00 |
| Sibling - List    |              0.00 |          0.00 |              0.00 |
| Sibling - HashSet |              0.00 |          0.00 |              0.00 |

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
