# Relational Component Performance Benchmarks

Relational component attributes (`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`) remove repetitive `GetComponent*` code. These benchmarks quantify the runtime cost of calling `Assign*Components` for common field shapes (single component, array, `List<T>`, and `HashSet<T>`) against hand-written lookups.

**How to refresh these tables:**

1. Open Unity’s Test Runner (EditMode/PlayMode as appropriate for your setup).
2. Run `RelationalComponentBenchmarkTests.Benchmark` inside `Tests/Runtime/Performance`.
3. The test logs the tables to the console and rewrites the section that matches the current operating system.

Alternatively, run the `scripts/run-relational-benchmarks.ps1` helper from the repository root:

```powershell
pwsh ./scripts/run-relational-benchmarks.ps1 `
    -UnityPath "C:\Program Files\Unity\Hub\Editor\2021.3.39f1\Editor\Unity.exe" `
    -ProjectPath "D:\Projects\BenchmarkHarness"
```

The script executes the benchmark test in batch mode, captures the markdown tables to `BenchmarkLogs/RelationalBenchmark.log`, and preserves the raw `TestResults.xml` when `-KeepResults` is specified.

## Windows (Editor/Player)

<!-- RELATIONAL_COMPONENTS_WINDOWS_START -->

_Last updated 2025-11-01 02:02 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,000,567 |    5,721,519 |      0.17x |  1,010,000 |
| Parent - Array    |          679,609 |    3,320,188 |      0.20x |    680,000 |
| Parent - List     |          761,662 |    4,294,566 |      0.18x |    770,000 |
| Parent - HashSet  |          740,692 |    2,940,243 |      0.25x |    750,000 |
| Child - Single    |          947,102 |    3,437,016 |      0.28x |    950,000 |
| Child - Array     |          258,864 |    2,351,738 |      0.11x |    260,000 |
| Child - List      |          244,627 |    2,609,863 |      0.09x |    250,000 |
| Child - HashSet   |          246,569 |    1,646,883 |      0.15x |    250,000 |
| Sibling - Single  |        3,841,839 |   14,277,857 |      0.27x |  3,850,000 |
| Sibling - Array   |          921,135 |    2,554,467 |      0.36x |    930,000 |
| Sibling - List    |        1,153,327 |    3,365,980 |      0.34x |  1,160,000 |
| Sibling - HashSet |        1,128,114 |    1,755,090 |      0.64x |  1,130,000 |

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
