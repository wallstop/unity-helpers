# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`), and compare the allocation footprint against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-10-29 02:16 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second and lower bytes per operation are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |          969,868 |    5,043,927 |      0.19x |    970,034 |
| Parent - Array    |          677,132 |    3,051,072 |      0.22x |    677,132 |
| Parent - List     |          757,995 |    2,805,808 |      0.27x |    757,996 |
| Parent - HashSet  |          748,237 |    2,689,130 |      0.28x |    748,238 |
| Child - Single    |          998,004 |    3,219,095 |      0.31x |    998,005 |
| Child - Array     |          266,959 |    2,189,596 |      0.12x |    266,959 |
| Child - List      |          237,332 |    1,851,231 |      0.13x |    237,333 |
| Child - HashSet   |          235,315 |    1,637,097 |      0.14x |    235,315 |
| Sibling - Single  |        3,656,776 |   10,794,297 |      0.34x |  3,656,777 |
| Sibling - Array   |          891,994 |    2,364,355 |      0.38x |    891,994 |
| Sibling - List    |        1,133,150 |    1,999,333 |      0.57x |  1,133,151 |
| Sibling - HashSet |        1,105,136 |    1,632,675 |      0.68x |  1,105,137 |

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
