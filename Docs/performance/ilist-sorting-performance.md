# IList Sorting Performance Benchmarks

Unity Helpers ships several custom sorting algorithms for `IList<T>` that cover different trade-offs between adaptability, allocation patterns, and stability. This page gathers context and benchmark snapshots so you can choose the right algorithm for your workload and compare results across operating systems.

## Algorithm Cheatsheet

| Algorithm                   | Stable? | Best For                                                                   | Reference                                                                                                     |
| --------------------------- | ------- | -------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| Ghost Sort                  | No      | Mixed workloads that benefit from adaptive gap sorting and few allocations | Upstream project by Will Stafford Parsons (public repository currently offline)                               |
| Meteor Sort                 | No      | Almost-sorted data where gap shrinking beats plain insertion sort          | [meteorsort by Wiley Looper](https://github.com/wileylooper/meteorsort)                                       |
| Pattern-Defeating QuickSort | No      | General-purpose quicksort with protections against worst-case inputs       | [pdqsort by Orson Peters](https://github.com/orlp/pdqsort)                                                    |
| Grail Sort                  | Yes     | Large datasets where stability + low allocations matter                    | [GrailSort](https://github.com/Mrrl/GrailSort)                                                                |
| Power Sort                  | Yes     | Partially ordered data that benefits from adaptive run detection           | [PowerSort (Munro & Wild)](https://arxiv.org/abs/1805.04154)                                                  |
| Shear Sort                  | No      | Mesh-friendly workloads that tolerate multiple deterministic passes        | [Wikipedia - Shear sort](https://en.wikipedia.org/wiki/Shear_sort)                                            |
| Tim Sort                    | Yes     | General-purpose stable sorting with abundant natural runs                  | [Wikipedia - Timsort](https://en.wikipedia.org/wiki/Timsort)                                                  |
| Jesse Sort                  | No      | Data with long runs or duplicates where dual patience piles shine          | [JesseSort](https://github.com/lewj85/jessesort)                                                              |
| Green Sort                  | Yes     | Sustainable stable merges that trim ordered prefixes                       | [greeNsort](https://www.greensort.org/index.html)                                                             |
| Ska Sort                    | No      | Branch-friendly partitioning on large unstable datasets                    | [Ska Sort](https://probablydance.com/2016/12/27/i-wrote-a-faster-sorting-algorithm/)                          |
| Drift Sort                  | Yes     | Stable merges of blocky data with localized drift                          | [driftsort write-up](https://github.com/Voultapher/sort-research-rs/tree/main/writeup/driftsort_introduction) |
| Ipn Sort                    | No      | In-place adaptive quicksort scenarios needing robust pivots                | [ipnsort write-up](https://github.com/Voultapher/sort-research-rs/tree/main/writeup/ipnsort_introduction)     |
| Smooth Sort                 | No      | Weak-heap hybrid that approaches O(n) for presorted data                   | [Smoothsort - Wikipedia](https://en.wikipedia.org/wiki/Smoothsort)                                            |
| Block Merge Sort            | Yes     | Stable merges with √n buffer (WikiSort style)                              | [WikiSort](https://github.com/BonzaiThePenguin/WikiSort)                                                      |
| IPS⁴o Sort                  | No      | Cache-aware samplesort with multiway partitioning                          | [IPS⁴o paper](https://arxiv.org/abs/1705.02257)                                                               |
| Power Sort Plus             | Yes     | Enhanced run-priority merges inspired by Wild & Nebel                      | [PowerSort paper](https://arxiv.org/abs/1805.04154)                                                           |
| Glide Sort                  | Yes     | Stable galloping merges from the Rust glidesort research                   | [Glidesort write-up](https://github.com/Voultapher/sort-research-rs/tree/main/writeup/glidesort)              |
| Flux Sort                   | No      | Dual-pivot quicksort tuned for modern CPUs                                 | [Fluxsort write-up](https://github.com/Voultapher/sort-research-rs/tree/main/writeup/fluxsort)                |
| Indy Sort                   | Yes     | Queue-based stable merging (Rust indiesort)                                | [Indiesort write-up](https://github.com/Voultapher/sort-research-rs/tree/main/writeup/glidesort)              |
| Sled Sort                   | Yes     | Buffered sled merges from the greeNsort portfolio                          | [Sledsort overview](https://www.greensort.org/portfolio.html)                                                 |
| Insertion Sort              | Yes     | Tiny or nearly sorted collections where O(n²) is acceptable                | [Wikipedia - Insertion sort](https://en.wikipedia.org/wiki/Insertion_sort)                                    |

> **What does “stable” mean?** Stable sorting algorithms preserve the relative order of elements that compare as equal. This matters when items carry secondary keys (e.g., sorting people by last name but keeping first-name order deterministic). Unstable algorithms can reshuffle equal entries, which is usually fine for numeric keys but can break deterministic pipelines.
>
> **Heads up:** The original Ghost Sort repository was formerly hosted on GitHub under `wstaffordp/ghostsort`, but it currently returns 404. The Unity Helpers implementation remains based on that source; we will relink if/when an official mirror returns.

## Dataset Scenarios

- **Sorted** – ascending integers, verifying best-case behavior.
- **Nearly Sorted (2% swaps)** – deterministic neighbor swaps introduce light disorder to expose adaptive optimizations.
- **Shuffled (deterministic)** – Fisher–Yates shuffle using a fixed seed for reproducibility across runs and machines.

Each benchmark sorts a fresh copy of the dataset once and reports wall-clock duration. If a cell still shows `pending`, re-run the benchmark suite to collect fresh data for that algorithm/dataset size.

Run the `IListSortingPerformanceTests.Benchmark` test inside Unity’s Test Runner to refresh the tables below. Results automatically land in the section that matches the current operating system.

## Windows (Editor/Player)

<!-- ILIST_SORT_WINDOWS_START -->

_Last updated 2025-11-28 19:32 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `pending` marks algorithms that have not been benchmarked yet on this machine, and `n/a` indicates the algorithm (currently insertion sort) is intentionally skipped for that dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Shear    | Tim      | Jesse    | Green    | Ska      | Drift    | Ipn      | Smooth  | Block   | IPS4o   | Power+  | Glide   | Flux    | Indy    | Sled    |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | ------- | ------- | ------- | ------- | ------- | ------- | ------- | ------- |
| 100       | 0.028 ms | 0.002 ms | 0.001 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.010 ms | 0.001 ms | 0.056 ms | 0.001 ms | 0.006 ms | 0.001 ms | 0.001 ms | pending | pending | pending | pending | pending | pending | pending | pending |
| 1,000     | 0.023 ms | 0.028 ms | 0.008 ms                    | 0.007 ms | 0.005 ms | 0.006 ms  | 0.008 ms | 0.005 ms | 0.802 ms | 0.007 ms | 0.106 ms | 0.016 ms | 0.008 ms | pending | pending | pending | pending | pending | pending | pending | pending |
| 10,000    | 0.296 ms | 0.407 ms | 0.078 ms                    | 0.066 ms | 0.048 ms | 0.062 ms  | 4.14 ms  | 0.046 ms | 10.7 ms  | 0.070 ms | 1.50 ms  | 0.151 ms | 0.078 ms | pending | pending | pending | pending | pending | pending | pending | pending |
| 100,000   | 3.55 ms  | 5.21 ms  | 0.797 ms                    | 0.689 ms | 0.497 ms | n/a       | 0.799 ms | 0.477 ms | 175 ms   | 0.665 ms | 19.8 ms  | 1.51 ms  | 0.789 ms | pending | pending | pending | pending | pending | pending | pending | pending |
| 1,000,000 | 43.4 ms  | 67.3 ms  | 8.00 ms                     | 7.06 ms  | 4.84 ms  | n/a       | 3.24 s   | 4.89 ms  | 2.00 s   | 6.50 ms  | 236 ms   | 15.1 ms  | 7.89 ms  | pending | pending | pending | pending | pending | pending | pending | pending |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Shear    | Tim      | Jesse    | Green    | Ska      | Drift    | Ipn      | Smooth  | Block   | IPS4o   | Power+  | Glide   | Flux    | Indy    | Sled    |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | ------- | ------- | ------- | ------- | ------- | ------- | ------- | ------- |
| 100       | 0.001 ms | 0.002 ms | 0.002 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.009 ms | 0.001 ms | 0.051 ms | 0.001 ms | 0.006 ms | 0.001 ms | 0.002 ms | pending | pending | pending | pending | pending | pending | pending | pending |
| 1,000     | 0.024 ms | 0.028 ms | 0.033 ms                    | 0.008 ms | 0.016 ms | 0.007 ms  | 0.033 ms | 0.014 ms | 0.783 ms | 0.007 ms | 0.104 ms | 0.016 ms | 0.030 ms | pending | pending | pending | pending | pending | pending | pending | pending |
| 10,000    | 0.296 ms | 0.405 ms | 0.441 ms                    | 0.077 ms | 0.243 ms | 0.064 ms  | 4.14 ms  | 0.179 ms | 10.1 ms  | 0.072 ms | 1.50 ms  | 0.152 ms | 0.412 ms | pending | pending | pending | pending | pending | pending | pending | pending |
| 100,000   | 3.58 ms  | 5.25 ms  | 5.43 ms                     | 0.814 ms | 3.62 ms  | n/a       | 5.37 ms  | 2.54 ms  | 137 ms   | 0.742 ms | 19.4 ms  | 1.51 ms  | 5.06 ms  | pending | pending | pending | pending | pending | pending | pending | pending |
| 1,000,000 | 42.0 ms  | 64.8 ms  | 63.0 ms                     | 8.44 ms  | 51.8 ms  | n/a       | 3.27 s   | 28.3 ms  | 1.98 s   | 8.18 ms  | 252 ms   | 15.4 ms  | 61.8 ms  | pending | pending | pending | pending | pending | pending | pending | pending |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Shear    | Tim      | Jesse    | Green    | Ska      | Drift    | Ipn      | Smooth  | Block   | IPS4o   | Power+  | Glide   | Flux    | Indy    | Sled    |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | ------- | ------- | ------- | ------- | ------- | ------- | ------- | ------- |
| 100       | 0.008 ms | 0.007 ms | 0.006 ms                    | 0.006 ms | 0.006 ms | 0.017 ms  | 0.017 ms | 0.010 ms | 0.029 ms | 0.007 ms | 0.009 ms | 0.008 ms | 0.007 ms | pending | pending | pending | pending | pending | pending | pending | pending |
| 1,000     | 0.158 ms | 0.133 ms | 0.092 ms                    | 0.107 ms | 0.116 ms | 1.51 ms   | 0.093 ms | 0.152 ms | 0.476 ms | 0.122 ms | 0.135 ms | 0.174 ms | 0.101 ms | pending | pending | pending | pending | pending | pending | pending | pending |
| 10,000    | 2.18 ms  | 1.96 ms  | 1.25 ms                     | 1.47 ms  | 1.57 ms  | 149 ms    | 13.5 ms  | 1.77 ms  | 6.46 ms  | 1.44 ms  | 1.94 ms  | 4.84 ms  | 1.40 ms  | pending | pending | pending | pending | pending | pending | pending | pending |
| 100,000   | 32.3 ms  | 26.7 ms  | 15.9 ms                     | 18.8 ms  | 19.0 ms  | n/a       | 15.9 ms  | 21.3 ms  | 80.6 ms  | 18.8 ms  | 25.1 ms  | 147 ms   | 16.5 ms  | pending | pending | pending | pending | pending | pending | pending | pending |
| 1,000,000 | 450 ms   | 336 ms   | 186 ms                      | 226 ms   | 228 ms   | n/a       | 15.50 s  | 274 ms   | 884 ms   | 237 ms   | 301 ms   | 4.68 s   | 196 ms   | pending | pending | pending | pending | pending | pending | pending | pending |

<!-- ILIST_SORT_WINDOWS_END -->

## macOS

<!-- ILIST_SORT_MACOS_START -->

Pending — run the IList sorting benchmark suite on macOS to capture results.

<!-- ILIST_SORT_MACOS_END -->

## Linux

<!-- ILIST_SORT_LINUX_START -->

Pending — run the IList sorting benchmark suite on Linux to capture results.

<!-- ILIST_SORT_LINUX_END -->

## Other Platforms

<!-- ILIST_SORT_OTHER_START -->

Pending — run the IList sorting benchmark suite on the target platform to capture results.

<!-- ILIST_SORT_OTHER_END -->
