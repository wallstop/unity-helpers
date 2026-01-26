# IList Sorting Performance Benchmarks

Unity Helpers ships several custom sorting algorithms for `IList<T>` that cover different trade-offs between adaptability, allocation patterns, and stability. This page gathers context and benchmark snapshots so you can choose the right algorithm for your workload and compare results across operating systems.

## Algorithm Cheatsheet

| Algorithm                   | Stable? | Best For                                                                   | Reference                                                                                                 |
| --------------------------- | ------- | -------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Ghost Sort                  | No      | Mixed workloads that benefit from adaptive gap sorting and few allocations | Upstream project by Will Stafford Parsons (public repository currently offline)                           |
| Meteor Sort                 | No      | Almost-sorted data where gap shrinking beats plain insertion sort          | Upstream project by Will Stafford Parsons (public repository currently offline)                           |
| Pattern-Defeating QuickSort | No      | General-purpose quicksort with protections against worst-case inputs       | [pdqsort by Orson Peters](https://github.com/orlp/pdqsort)                                                |
| Grail Sort                  | Yes     | Large datasets where stability + low allocations matter                    | [GrailSort](https://github.com/Mrrl/GrailSort)                                                            |
| Power Sort                  | Yes     | Partially ordered data that benefits from adaptive run detection           | [PowerSort (Munro & Wild)](https://arxiv.org/abs/1805.04154)                                              |
| Tim Sort                    | Yes     | General-purpose stable sorting with abundant natural runs                  | [Wikipedia - Timsort](https://en.wikipedia.org/wiki/Timsort)                                              |
| Jesse Sort                  | No      | Data with long runs or duplicates where dual patience piles shine          | [JesseSort](https://github.com/lewj85/jessesort)                                                          |
| Green Sort                  | Yes     | Sustainable stable merges that trim ordered prefixes                       | [greeNsort](https://www.greensort.org/index.html)                                                         |
| Ska Sort                    | No      | Branch-friendly partitioning on large unstable datasets                    | [Ska Sort](https://probablydance.com/2016/12/27/i-wrote-a-faster-sorting-algorithm/)                      |
| Ipn Sort                    | No      | In-place adaptive quicksort scenarios needing robust pivots                | [ipnsort write-up](https://github.com/Voultapher/sort-research-rs/tree/main/writeup/ipnsort_introduction) |
| Smooth Sort                 | No      | Weak-heap hybrid that approaches O(n) for presorted data                   | [Smoothsort - Wikipedia](https://en.wikipedia.org/wiki/Smoothsort)                                        |
| Block Merge Sort            | Yes     | Stable merges with √n buffer (WikiSort style)                              | [WikiSort](https://github.com/BonzaiThePenguin/WikiSort)                                                  |
| IPS⁴o Sort                  | No      | Cache-aware samplesort with multiway partitioning                          | [IPS⁴o paper](https://arxiv.org/abs/1705.02257)                                                           |
| Power Sort Plus             | Yes     | Enhanced run-priority merges inspired by Wild & Nebel                      | [PowerSort paper](https://arxiv.org/abs/1805.04154)                                                       |
| Glide Sort                  | Yes     | Stable galloping merges from the Rust glidesort research                   | [sort-research-rs](https://github.com/Voultapher/sort-research-rs)                                        |
| Flux Sort                   | No      | Dual-pivot quicksort tuned for modern CPUs                                 | [sort-research-rs](https://github.com/Voultapher/sort-research-rs)                                        |
| Insertion Sort              | Yes     | Tiny or nearly sorted collections where O(n²) is acceptable                | [Wikipedia - Insertion sort](https://en.wikipedia.org/wiki/Insertion_sort)                                |

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

_Last updated 2026-01-12 01:17 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

<table data-sortable>
  <thead>
    <tr>
      <th align="left">List Size</th>
      <th align="right">Ghost</th>
      <th align="right">Meteor</th>
      <th align="right">Pattern-Defeating QuickSort</th>
      <th align="right">Grail</th>
      <th align="right">Power</th>
      <th align="right">Insertion</th>
      <th align="right">Tim</th>
      <th align="right">Jesse</th>
      <th align="right">Green</th>
      <th align="right">Ska</th>
      <th align="right">Ipn</th>
      <th align="right">Smooth</th>
      <th align="right">Block</th>
      <th align="right">IPS4o</th>
      <th align="right">Power+</th>
      <th align="right">Glide</th>
      <th align="right">Flux</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">100</td><td align="right">0.029 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.624 ms</td><td align="right">0.001 ms</td><td align="right">0.007 ms</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">0.002 ms</td></tr>
    <tr><td align="left">1,000</td><td align="right">0.023 ms</td><td align="right">0.027 ms</td><td align="right">0.008 ms</td><td align="right">0.007 ms</td><td align="right">0.007 ms</td><td align="right">0.006 ms</td><td align="right">0.006 ms</td><td align="right">54.3 ms</td><td align="right">0.007 ms</td><td align="right">0.112 ms</td><td align="right">0.009 ms</td><td align="right">0.018 ms</td><td align="right">0.005 ms</td><td align="right">0.040 ms</td><td align="right">0.007 ms</td><td align="right">0.007 ms</td><td align="right">0.029 ms</td></tr>
    <tr><td align="left">10,000</td><td align="right">0.305 ms</td><td align="right">0.394 ms</td><td align="right">0.077 ms</td><td align="right">0.066 ms</td><td align="right">0.049 ms</td><td align="right">0.066 ms</td><td align="right">0.047 ms</td><td align="right">3.24 s</td><td align="right">0.071 ms</td><td align="right">1.45 ms</td><td align="right">0.083 ms</td><td align="right">0.175 ms</td><td align="right">0.056 ms</td><td align="right">0.571 ms</td><td align="right">0.048 ms</td><td align="right">0.048 ms</td><td align="right">0.386 ms</td></tr>
    <tr><td align="left">100,000</td><td align="right">3.51 ms</td><td align="right">5.14 ms</td><td align="right">0.764 ms</td><td align="right">0.658 ms</td><td align="right">0.469 ms</td><td align="right">n/a</td><td align="right">0.459 ms</td><td align="right">136.86 s</td><td align="right">0.670 ms</td><td align="right">18.5 ms</td><td align="right">0.773 ms</td><td align="right">1.80 ms</td><td align="right">0.512 ms</td><td align="right">7.57 ms</td><td align="right">0.473 ms</td><td align="right">0.479 ms</td><td align="right">4.93 ms</td></tr>
  </tbody>
</table>

### Nearly Sorted (2% swaps)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">List Size</th>
      <th align="right">Ghost</th>
      <th align="right">Meteor</th>
      <th align="right">Pattern-Defeating QuickSort</th>
      <th align="right">Grail</th>
      <th align="right">Power</th>
      <th align="right">Insertion</th>
      <th align="right">Tim</th>
      <th align="right">Jesse</th>
      <th align="right">Green</th>
      <th align="right">Ska</th>
      <th align="right">Ipn</th>
      <th align="right">Smooth</th>
      <th align="right">Block</th>
      <th align="right">IPS4o</th>
      <th align="right">Power+</th>
      <th align="right">Glide</th>
      <th align="right">Flux</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">100</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.003 ms</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">241 ms</td><td align="right">0.001 ms</td><td align="right">0.007 ms</td><td align="right">0.002 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">0.004 ms</td><td align="right">0.003 ms</td><td align="right">0.002 ms</td></tr>
    <tr><td align="left">1,000</td><td align="right">0.023 ms</td><td align="right">0.031 ms</td><td align="right">0.033 ms</td><td align="right">0.007 ms</td><td align="right">0.018 ms</td><td align="right">0.007 ms</td><td align="right">0.014 ms</td><td align="right">2.43 s</td><td align="right">0.007 ms</td><td align="right">0.100 ms</td><td align="right">0.030 ms</td><td align="right">0.018 ms</td><td align="right">0.007 ms</td><td align="right">0.045 ms</td><td align="right">0.025 ms</td><td align="right">0.016 ms</td><td align="right">0.030 ms</td></tr>
    <tr><td align="left">10,000</td><td align="right">0.293 ms</td><td align="right">0.395 ms</td><td align="right">0.443 ms</td><td align="right">0.073 ms</td><td align="right">0.232 ms</td><td align="right">0.063 ms</td><td align="right">0.173 ms</td><td align="right">23.44 s</td><td align="right">0.071 ms</td><td align="right">1.44 ms</td><td align="right">0.405 ms</td><td align="right">0.189 ms</td><td align="right">0.069 ms</td><td align="right">0.674 ms</td><td align="right">0.433 ms</td><td align="right">0.166 ms</td><td align="right">0.390 ms</td></tr>
    <tr><td align="left">100,000</td><td align="right">3.60 ms</td><td align="right">5.19 ms</td><td align="right">5.30 ms</td><td align="right">0.843 ms</td><td align="right">3.53 ms</td><td align="right">n/a</td><td align="right">2.43 ms</td><td align="right">137.09 s</td><td align="right">0.727 ms</td><td align="right">18.8 ms</td><td align="right">4.98 ms</td><td align="right">1.95 ms</td><td align="right">0.633 ms</td><td align="right">8.46 ms</td><td align="right">5.37 ms</td><td align="right">2.28 ms</td><td align="right">4.96 ms</td></tr>
  </tbody>
</table>

### Shuffled (deterministic)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">List Size</th>
      <th align="right">Ghost</th>
      <th align="right">Meteor</th>
      <th align="right">Pattern-Defeating QuickSort</th>
      <th align="right">Grail</th>
      <th align="right">Power</th>
      <th align="right">Insertion</th>
      <th align="right">Tim</th>
      <th align="right">Jesse</th>
      <th align="right">Green</th>
      <th align="right">Ska</th>
      <th align="right">Ipn</th>
      <th align="right">Smooth</th>
      <th align="right">Block</th>
      <th align="right">IPS4o</th>
      <th align="right">Power+</th>
      <th align="right">Glide</th>
      <th align="right">Flux</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">100</td><td align="right">0.008 ms</td><td align="right">0.007 ms</td><td align="right">0.006 ms</td><td align="right">0.007 ms</td><td align="right">0.008 ms</td><td align="right">0.017 ms</td><td align="right">0.011 ms</td><td align="right">61.7 ms</td><td align="right">0.008 ms</td><td align="right">0.011 ms</td><td align="right">0.008 ms</td><td align="right">0.012 ms</td><td align="right">0.005 ms</td><td align="right">0.006 ms</td><td align="right">0.025 ms</td><td align="right">0.012 ms</td><td align="right">0.007 ms</td></tr>
    <tr><td align="left">1,000</td><td align="right">0.156 ms</td><td align="right">0.132 ms</td><td align="right">0.088 ms</td><td align="right">0.105 ms</td><td align="right">0.152 ms</td><td align="right">1.49 ms</td><td align="right">0.160 ms</td><td align="right">216 ms</td><td align="right">0.121 ms</td><td align="right">0.133 ms</td><td align="right">0.097 ms</td><td align="right">0.200 ms</td><td align="right">0.089 ms</td><td align="right">0.107 ms</td><td align="right">0.434 ms</td><td align="right">0.159 ms</td><td align="right">0.098 ms</td></tr>
    <tr><td align="left">10,000</td><td align="right">2.50 ms</td><td align="right">1.95 ms</td><td align="right">1.22 ms</td><td align="right">1.46 ms</td><td align="right">1.49 ms</td><td align="right">147 ms</td><td align="right">1.57 ms</td><td align="right">669 ms</td><td align="right">1.38 ms</td><td align="right">1.78 ms</td><td align="right">1.33 ms</td><td align="right">2.76 ms</td><td align="right">1.20 ms</td><td align="right">1.57 ms</td><td align="right">5.98 ms</td><td align="right">1.80 ms</td><td align="right">1.44 ms</td></tr>
    <tr><td align="left">100,000</td><td align="right">31.9 ms</td><td align="right">26.0 ms</td><td align="right">15.3 ms</td><td align="right">18.2 ms</td><td align="right">18.3 ms</td><td align="right">n/a</td><td align="right">20.4 ms</td><td align="right">2.21 s</td><td align="right">18.3 ms</td><td align="right">23.0 ms</td><td align="right">15.9 ms</td><td align="right">35.5 ms</td><td align="right">15.2 ms</td><td align="right">20.2 ms</td><td align="right">77.1 ms</td><td align="right">23.6 ms</td><td align="right">17.6 ms</td></tr>
  </tbody>
</table>

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
