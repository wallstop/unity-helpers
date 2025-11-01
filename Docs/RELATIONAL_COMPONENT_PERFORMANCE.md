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

_Last updated 2025-11-01 00:53 UTC on Windows 11 (10.0.26200) 64bit_

Numbers capture repeated `Assign*Components` calls for one second per scenario.
Higher operations per second are better.

### Operations per second (higher is better)

| Scenario          | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |
| ----------------- | ---------------: | -----------: | ---------: | ---------: |
| Parent - Single   |        1,011,738 |    5,725,043 |      0.18x |  1,020,000 |
| Parent - Array    |          672,506 |    3,288,757 |      0.20x |    680,000 |
| Parent - List     |          756,936 |    4,263,894 |      0.18x |    760,000 |
| Parent - HashSet  |          751,567 |    2,924,044 |      0.26x |    760,000 |
| Child - Single    |        1,047,220 |    3,374,546 |      0.31x |  1,050,000 |
| Child - Array     |          265,924 |    2,389,668 |      0.11x |    270,000 |
| Child - List      |          237,883 |    2,569,979 |      0.09x |    240,000 |
| Child - HashSet   |          233,743 |    1,674,818 |      0.14x |    240,000 |
| Sibling - Single  |        3,866,877 |   14,384,412 |      0.27x |  3,870,000 |
| Sibling - Array   |          929,104 |    2,578,155 |      0.36x |    930,000 |
| Sibling - List    |        1,188,211 |    3,412,093 |      0.35x |  1,190,000 |
| Sibling - HashSet |        1,146,367 |    1,798,878 |      0.64x |  1,150,000 |

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
