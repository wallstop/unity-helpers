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

_Last updated 2026-04-21 03:15 UTC on Windows 11 (10.0.26200) 64bit_

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
    <tr><td align="left">100</td><td align="right">0.005 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.098 ms</td><td align="right">0.001 ms</td><td align="right">0.006 ms</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td></tr>
    <tr><td align="left">1,000</td><td align="right">0.022 ms</td><td align="right">0.026 ms</td><td align="right">0.007 ms</td><td align="right">0.007 ms</td><td align="right">0.005 ms</td><td align="right">0.006 ms</td><td align="right">0.005 ms</td><td align="right">1.23 ms</td><td align="right">0.006 ms</td><td align="right">0.098 ms</td><td align="right">0.007 ms</td><td align="right">0.017 ms</td><td align="right">0.005 ms</td><td align="right">0.036 ms</td><td align="right">0.005 ms</td><td align="right">0.005 ms</td><td align="right">0.028 ms</td></tr>
    <tr><td align="left">10,000</td><td align="right">0.276 ms</td><td align="right">0.376 ms</td><td align="right">0.071 ms</td><td align="right">0.062 ms</td><td align="right">0.044 ms</td><td align="right">0.056 ms</td><td align="right">0.042 ms</td><td align="right">14.6 ms</td><td align="right">0.062 ms</td><td align="right">1.46 ms</td><td align="right">0.072 ms</td><td align="right">0.168 ms</td><td align="right">0.048 ms</td><td align="right">0.585 ms</td><td align="right">0.044 ms</td><td align="right">0.044 ms</td><td align="right">0.404 ms</td></tr>
    <tr><td align="left">100,000</td><td align="right">3.36 ms</td><td align="right">4.94 ms</td><td align="right">0.717 ms</td><td align="right">0.621 ms</td><td align="right">0.430 ms</td><td align="right">n/a</td><td align="right">0.415 ms</td><td align="right">186 ms</td><td align="right">0.609 ms</td><td align="right">18.1 ms</td><td align="right">0.745 ms</td><td align="right">1.67 ms</td><td align="right">0.490 ms</td><td align="right">7.01 ms</td><td align="right">0.429 ms</td><td align="right">0.422 ms</td><td align="right">4.75 ms</td></tr>
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
    <tr><td align="left">100</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.001 ms</td><td align="right">0.093 ms</td><td align="right">0.001 ms</td><td align="right">0.006 ms</td><td align="right">0.002 ms</td><td align="right">0.002 ms</td><td align="right">0.001 ms</td><td align="right">0.002 ms</td><td align="right">0.003 ms</td><td align="right">0.002 ms</td><td align="right">0.002 ms</td></tr>
    <tr><td align="left">1,000</td><td align="right">0.022 ms</td><td align="right">0.026 ms</td><td align="right">0.031 ms</td><td align="right">0.007 ms</td><td align="right">0.016 ms</td><td align="right">0.006 ms</td><td align="right">0.014 ms</td><td align="right">1.21 ms</td><td align="right">0.007 ms</td><td align="right">0.098 ms</td><td align="right">0.027 ms</td><td align="right">0.017 ms</td><td align="right">0.006 ms</td><td align="right">0.043 ms</td><td align="right">0.023 ms</td><td align="right">0.015 ms</td><td align="right">0.029 ms</td></tr>
    <tr><td align="left">10,000</td><td align="right">0.277 ms</td><td align="right">0.375 ms</td><td align="right">0.409 ms</td><td align="right">0.071 ms</td><td align="right">0.226 ms</td><td align="right">0.064 ms</td><td align="right">0.167 ms</td><td align="right">14.2 ms</td><td align="right">0.068 ms</td><td align="right">1.41 ms</td><td align="right">0.386 ms</td><td align="right">0.174 ms</td><td align="right">0.069 ms</td><td align="right">0.646 ms</td><td align="right">0.370 ms</td><td align="right">0.160 ms</td><td align="right">0.380 ms</td></tr>
    <tr><td align="left">100,000</td><td align="right">3.33 ms</td><td align="right">4.90 ms</td><td align="right">4.95 ms</td><td align="right">0.710 ms</td><td align="right">3.58 ms</td><td align="right">n/a</td><td align="right">2.38 ms</td><td align="right">173 ms</td><td align="right">0.695 ms</td><td align="right">18.1 ms</td><td align="right">4.63 ms</td><td align="right">1.72 ms</td><td align="right">0.603 ms</td><td align="right">7.97 ms</td><td align="right">5.34 ms</td><td align="right">2.24 ms</td><td align="right">4.84 ms</td></tr>
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
    <tr><td align="left">100</td><td align="right">0.008 ms</td><td align="right">0.007 ms</td><td align="right">0.005 ms</td><td align="right">0.006 ms</td><td align="right">0.007 ms</td><td align="right">0.016 ms</td><td align="right">0.010 ms</td><td align="right">0.041 ms</td><td align="right">0.007 ms</td><td align="right">0.008 ms</td><td align="right">0.007 ms</td><td align="right">0.010 ms</td><td align="right">0.005 ms</td><td align="right">0.005 ms</td><td align="right">0.023 ms</td><td align="right">0.011 ms</td><td align="right">0.006 ms</td></tr>
    <tr><td align="left">1,000</td><td align="right">0.146 ms</td><td align="right">0.126 ms</td><td align="right">0.093 ms</td><td align="right">0.102 ms</td><td align="right">0.108 ms</td><td align="right">1.38 ms</td><td align="right">0.139 ms</td><td align="right">0.439 ms</td><td align="right">0.106 ms</td><td align="right">0.130 ms</td><td align="right">0.094 ms</td><td align="right">0.186 ms</td><td align="right">0.086 ms</td><td align="right">0.106 ms</td><td align="right">0.422 ms</td><td align="right">0.150 ms</td><td align="right">0.098 ms</td></tr>
    <tr><td align="left">10,000</td><td align="right">2.04 ms</td><td align="right">1.83 ms</td><td align="right">1.19 ms</td><td align="right">1.39 ms</td><td align="right">1.46 ms</td><td align="right">137 ms</td><td align="right">1.54 ms</td><td align="right">5.42 ms</td><td align="right">1.35 ms</td><td align="right">1.77 ms</td><td align="right">1.28 ms</td><td align="right">2.63 ms</td><td align="right">1.17 ms</td><td align="right">1.52 ms</td><td align="right">5.89 ms</td><td align="right">1.72 ms</td><td align="right">1.41 ms</td></tr>
    <tr><td align="left">100,000</td><td align="right">29.4 ms</td><td align="right">24.5 ms</td><td align="right">15.0 ms</td><td align="right">17.6 ms</td><td align="right">18.0 ms</td><td align="right">n/a</td><td align="right">19.8 ms</td><td align="right">68.4 ms</td><td align="right">17.5 ms</td><td align="right">22.5 ms</td><td align="right">15.6 ms</td><td align="right">33.6 ms</td><td align="right">14.7 ms</td><td align="right">20.0 ms</td><td align="right">74.4 ms</td><td align="right">22.6 ms</td><td align="right">17.5 ms</td></tr>
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
