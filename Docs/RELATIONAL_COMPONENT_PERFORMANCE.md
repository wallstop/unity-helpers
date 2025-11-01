# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-11-01 00:03 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |          977,291 |    4,962,140 |      0.20x |    977,448 |
| Parent - Array    |          659,941 |    3,100,723 |      0.21x |    659,941 |
| Parent - List     |          745,395 |    2,858,832 |      0.26x |    745,395 |
| Parent - HashSet  |          732,929 |    2,726,856 |      0.27x |    732,929 |
| Child - Single    |        1,020,144 |    3,251,056 |      0.31x |  1,020,145 |
| Child - Array     |          258,944 |    2,173,773 |      0.12x |    258,944 |
| Child - List      |          229,753 |    1,830,074 |      0.13x |    229,753 |
| Child - HashSet   |          228,732 |    1,615,915 |      0.14x |    228,733 |
| Sibling - Single  |        3,523,017 |   10,585,518 |      0.33x |  3,523,018 |
| Sibling - Array   |          868,563 |    2,353,304 |      0.37x |    868,564 |
| Sibling - List    |        1,120,143 |    1,991,601 |      0.56x |  1,120,144 |
| Sibling - HashSet |        1,093,720 |    1,693,664 |      0.65x |  1,093,720 |

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
