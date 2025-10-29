# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-10-29 00:09:34 UTC

### Boxed Access (object)

| Scenario                       | ReflectionHelpers (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | --------------------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 22.99M                      | 7.25M                       | 3.17x                 |
| Instance Field Set (boxed)     | 23.23M                      | 4.48M                       | 5.19x                 |
| Static Field Get (boxed)       | 27.47M                      | 6.85M                       | 4.01x                 |
| Static Field Set (boxed)       | 26.87M                      | 6.19M                       | 4.34x                 |
| Instance Property Get (boxed)  | 21.05M                      | 26.83M                      | 0.78x                 |
| Instance Property Set (boxed)  | 25.77M                      | 1.55M                       | 16.62x                |
| Static Property Get (boxed)    | 24.82M                      | 12.87M                      | 1.93x                 |
| Static Property Set (boxed)    | 7.65M                       | 2.92M                       | 2.62x                 |
| Instance Method Invoke (boxed) | 22.82M                      | 1.96M                       | 11.67x                |
| Static Method Invoke (boxed)   | 20.89M                      | 2.69M                       | 7.78x                 |
| Constructor Invoke (boxed)     | 27.53M                      | 2.57M                       | 10.71x                |

### Typed Access (no boxing)

| Scenario                       | ReflectionHelpers (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | --------------------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 587.83M                     | 669.98M                     | 7.25M                       | 0.88x               | 81.10x                |
| Instance Field Set (typed)     | 653.31M                     | 666.84M                     | 4.48M                       | 0.98x               | 145.93x               |
| Static Field Get (typed)       | 621.44M                     | 694.19M                     | 6.85M                       | 0.90x               | 90.72x                |
| Static Field Set (typed)       | 684.93M                     | 662.96M                     | 6.19M                       | 1.03x               | 110.69x               |
| Instance Property Get (typed)  | 690.41M                     | 694.37M                     | 26.83M                      | 0.99x               | 25.74x                |
| Instance Property Set (typed)  | 665.65M                     | 703.43M                     | 1.55M                       | 0.95x               | 429.39x               |
| Static Property Get (typed)    | 624.04M                     | 691.34M                     | 12.87M                      | 0.90x               | 48.48x                |
| Static Property Set (typed)    | 670.72M                     | 668.03M                     | 2.92M                       | 1.00x               | 229.82x               |
| Instance Method Invoke (typed) | 700.77M                     | 687.27M                     | 1.96M                       | 1.02x               | 358.31x               |
| Static Method Invoke (typed)   | 661.77M                     | 686.00M                     | 2.69M                       | 0.96x               | 246.28x               |

<!-- REFLECTION_PERFORMANCE_WINDOWS_END -->

## macOS

<!-- REFLECTION_PERFORMANCE_MACOS_START -->

_No benchmark data generated yet._

<!-- REFLECTION_PERFORMANCE_MACOS_END -->

## Linux

<!-- REFLECTION_PERFORMANCE_LINUX_START -->

_No benchmark data generated yet._

<!-- REFLECTION_PERFORMANCE_LINUX_END -->

## Unknown / Other

<!-- REFLECTION_PERFORMANCE_UNKNOWN_START -->

_No benchmark data generated yet._

<!-- REFLECTION_PERFORMANCE_UNKNOWN_END -->
