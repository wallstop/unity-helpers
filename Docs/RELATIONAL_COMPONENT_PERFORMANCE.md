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

_Last updated 2025-11-01 01:17 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,008,742 |    5,732,750 |      0.18x |  1,010,000 |
| Parent - Array    |          680,802 |    3,353,863 |      0.20x |    690,000 |
| Parent - List     |          763,803 |    4,293,490 |      0.18x |    770,000 |
| Parent - HashSet  |          752,377 |    2,946,458 |      0.26x |    760,000 |
| Child - Single    |          943,852 |    3,456,837 |      0.27x |    950,000 |
| Child - Array     |          260,230 |    2,382,860 |      0.11x |    270,000 |
| Child - List      |          245,431 |    2,622,003 |      0.09x |    250,000 |
| Child - HashSet   |          246,195 |    1,677,008 |      0.15x |    250,000 |
| Sibling - Single  |        3,892,516 |   14,545,457 |      0.27x |  3,900,000 |
| Sibling - Array   |          925,862 |    2,584,238 |      0.36x |    930,000 |
| Sibling - List    |        1,183,063 |    3,369,882 |      0.35x |  1,190,000 |
| Sibling - HashSet |        1,136,774 |    1,803,478 |      0.63x |  1,140,000 |

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
