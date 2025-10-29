# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`), and compare the allocation footprint against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-10-29 17:54 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second and lower bytes per operation are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |          933,414 |    4,946,787 |      0.19x |    933,567 |
| Parent - Array    |          653,811 |    3,015,493 |      0.22x |    653,811 |
| Parent - List     |          711,840 |    2,752,766 |      0.26x |    711,841 |
| Parent - HashSet  |          712,752 |    2,696,221 |      0.26x |    712,752 |
| Child - Single    |          986,752 |    3,202,365 |      0.31x |    986,752 |
| Child - Array     |          259,534 |    2,147,453 |      0.12x |    259,534 |
| Child - List      |          231,263 |    1,813,055 |      0.13x |    231,263 |
| Child - HashSet   |          228,941 |    1,559,521 |      0.15x |    228,941 |
| Sibling - Single  |        1,895,421 |   10,646,058 |      0.18x |  1,895,421 |
| Sibling - Array   |          707,255 |    2,343,246 |      0.30x |    707,256 |
| Sibling - List    |          865,033 |    2,009,015 |      0.43x |    865,034 |
| Sibling - HashSet |          842,848 |    1,671,913 |      0.50x |    842,848 |

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
