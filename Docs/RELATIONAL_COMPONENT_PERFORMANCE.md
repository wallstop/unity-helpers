# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`), and compare the allocation footprint against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-10-29 01:51 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second and lower bytes per operation are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |          973,365 |    5,041,530 |      0.19x |    973,376 |
| Parent - Array    |          682,836 |    3,071,713 |      0.22x |    682,836 |
| Parent - List     |          756,882 |    2,794,874 |      0.27x |    756,883 |
| Parent - HashSet  |          751,853 |    2,615,419 |      0.29x |    751,854 |
| Child - Single    |          999,311 |    3,216,713 |      0.31x |    999,312 |
| Child - Array     |          266,458 |    2,198,816 |      0.12x |    266,459 |
| Child - List      |          233,841 |    1,826,644 |      0.13x |    233,841 |
| Child - HashSet   |          234,138 |    1,633,074 |      0.14x |    234,138 |
| Sibling - Single  |        3,634,355 |   10,766,799 |      0.34x |  3,634,356 |
| Sibling - Array   |          884,797 |    2,345,998 |      0.38x |    884,798 |
| Sibling - List    |        1,116,826 |    1,990,472 |      0.56x |  1,116,826 |
| Sibling - HashSet |        1,103,782 |    1,676,946 |      0.66x |  1,103,783 |

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
