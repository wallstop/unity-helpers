# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-12-01 04:03:54 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 21.94M           | 6.93M                       | 3.16x                 |
| Instance Field Set (boxed)     | 24.44M           | 4.24M                       | 5.76x                 |
| Static Field Get (boxed)       | 23.81M           | 8.71M                       | 2.73x                 |
| Static Field Set (boxed)       | 24.22M           | 6.20M                       | 3.91x                 |
| Instance Property Get (boxed)  | 24.97M           | 24.30M                      | 1.03x                 |
| Instance Property Set (boxed)  | 25.83M           | 901.3K                      | 28.66x                |
| Static Property Get (boxed)    | 3.27M            | 13.17M                      | 0.25x                 |
| Static Property Set (boxed)    | 23.90M           | 2.92M                       | 8.18x                 |
| Instance Method Invoke (boxed) | 22.07M           | 1.88M                       | 11.73x                |
| Static Method Invoke (boxed)   | 21.20M           | 2.66M                       | 7.97x                 |
| Constructor Invoke (boxed)     | 23.71M           | 2.55M                       | 9.29x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 674.88M          | 671.71M                     | 6.93M                       | 1.00x               | 97.33x                |
| Instance Field Set (typed)     | 654.04M          | 665.43M                     | 4.24M                       | 0.98x               | 154.26x               |
| Static Field Get (typed)       | 661.23M          | 692.13M                     | 8.71M                       | 0.96x               | 75.88x                |
| Static Field Set (typed)       | 670.56M          | 668.20M                     | 6.20M                       | 1.00x               | 108.17x               |
| Instance Property Get (typed)  | 669.83M          | 694.15M                     | 24.30M                      | 0.96x               | 27.56x                |
| Instance Property Set (typed)  | 667.41M          | 701.96M                     | 901.3K                      | 0.95x               | 740.49x               |
| Static Property Get (typed)    | 660.18M          | 687.11M                     | 13.17M                      | 0.96x               | 50.13x                |
| Static Property Set (typed)    | 662.81M          | 654.01M                     | 2.92M                       | 1.01x               | 226.86x               |
| Instance Method Invoke (typed) | 616.25M          | 678.17M                     | 1.88M                       | 0.91x               | 327.55x               |
| Static Method Invoke (typed)   | 631.03M          | 686.06M                     | 2.66M                       | 0.92x               | 237.21x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 19.22M           | 7.12M                       | 2.70x                 |
| Instance Field Set (boxed)     | 23.36M           | 3.92M                       | 5.96x                 |
| Static Field Get (boxed)       | 25.15M           | 8.58M                       | 2.93x                 |
| Static Field Set (boxed)       | 20.47M           | 6.22M                       | 3.29x                 |
| Instance Property Get (boxed)  | 23.22M           | 16.86M                      | 1.38x                 |
| Instance Property Set (boxed)  | 19.77M           | 2.07M                       | 9.54x                 |
| Static Property Get (boxed)    | 19.35M           | 23.70M                      | 0.82x                 |
| Static Property Set (boxed)    | 23.95M           | 2.95M                       | 8.11x                 |
| Instance Method Invoke (boxed) | 13.10M           | 1.90M                       | 6.88x                 |
| Static Method Invoke (boxed)   | 17.42M           | 2.62M                       | 6.65x                 |
| Constructor Invoke (boxed)     | 23.20M           | 2.50M                       | 9.26x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 654.20M          | 659.78M                     | 7.12M                       | 0.99x               | 91.83x                |
| Instance Field Set (typed)     | 644.96M          | 659.62M                     | 3.92M                       | 0.98x               | 164.52x               |
| Static Field Get (typed)       | 656.57M          | 692.46M                     | 8.58M                       | 0.95x               | 76.54x                |
| Static Field Set (typed)       | 671.42M          | 668.18M                     | 6.22M                       | 1.00x               | 107.92x               |
| Instance Property Get (typed)  | 671.92M          | 693.88M                     | 16.86M                      | 0.97x               | 39.85x                |
| Instance Property Set (typed)  | 667.62M          | 703.51M                     | 2.07M                       | 0.95x               | 322.05x               |
| Static Property Get (typed)    | 648.11M          | 682.65M                     | 23.70M                      | 0.95x               | 27.34x                |
| Static Property Set (typed)    | 667.67M          | 657.16M                     | 2.95M                       | 1.02x               | 226.05x               |
| Instance Method Invoke (typed) | 625.09M          | 678.03M                     | 1.90M                       | 0.92x               | 328.37x               |
| Static Method Invoke (typed)   | 621.44M          | 676.96M                     | 2.62M                       | 0.92x               | 237.33x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 21.45M           | 7.15M                       | 3.00x                 |
| Instance Field Set (boxed)     | 16.55M           | 2.51M                       | 6.59x                 |
| Static Field Get (boxed)       | 9.37M            | 8.59M                       | 1.09x                 |
| Static Field Set (boxed)       | 19.52M           | 6.30M                       | 3.10x                 |
| Instance Property Get (boxed)  | 20.89M           | 19.91M                      | 1.05x                 |
| Instance Property Set (boxed)  | 19.69M           | 2.08M                       | 9.45x                 |
| Static Property Get (boxed)    | 23.77M           | 25.41M                      | 0.94x                 |
| Static Property Set (boxed)    | 26.87M           | 2.96M                       | 9.09x                 |
| Instance Method Invoke (boxed) | 22.92M           | 1.77M                       | 12.94x                |
| Static Method Invoke (boxed)   | 23.77M           | 1.81M                       | 13.13x                |
| Constructor Invoke (boxed)     | 23.67M           | 2.54M                       | 9.31x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 652.10M          | 662.46M                     | 7.15M                       | 0.98x               | 91.24x                |
| Instance Field Set (typed)     | 656.62M          | 654.92M                     | 2.51M                       | 1.00x               | 261.39x               |
| Static Field Get (typed)       | 653.52M          | 681.41M                     | 8.59M                       | 0.96x               | 76.07x                |
| Static Field Set (typed)       | 663.22M          | 658.26M                     | 6.30M                       | 1.01x               | 105.35x               |
| Instance Property Get (typed)  | 639.44M          | 686.19M                     | 19.91M                      | 0.93x               | 32.11x                |
| Instance Property Set (typed)  | 658.18M          | 690.77M                     | 2.08M                       | 0.95x               | 315.74x               |
| Static Property Get (typed)    | 646.33M          | 683.77M                     | 25.41M                      | 0.95x               | 25.43x                |
| Static Property Set (typed)    | 662.30M          | 656.30M                     | 2.96M                       | 1.01x               | 224.10x               |
| Instance Method Invoke (typed) | 628.47M          | 677.31M                     | 1.77M                       | 0.93x               | 354.69x               |
| Static Method Invoke (typed)   | 612.73M          | 675.85M                     | 1.81M                       | 0.91x               | 338.50x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 7.12M            | 7.09M                       | 1.00x                 |
| Instance Field Set (boxed)     | 5.35M            | 3.91M                       | 1.37x                 |
| Static Field Get (boxed)       | 8.60M            | 8.71M                       | 0.99x                 |
| Static Field Set (boxed)       | 4.80M            | 6.29M                       | 0.76x                 |
| Instance Property Get (boxed)  | 24.88M           | 24.95M                      | 1.00x                 |
| Instance Property Set (boxed)  | 2.08M            | 2.13M                       | 0.98x                 |
| Static Property Get (boxed)    | 22.68M           | 25.67M                      | 0.88x                 |
| Static Property Set (boxed)    | 2.11M            | 2.96M                       | 0.71x                 |
| Instance Method Invoke (boxed) | 1.93M            | 1.93M                       | 1.00x                 |
| Static Method Invoke (boxed)   | 2.67M            | 2.67M                       | 1.00x                 |
| Constructor Invoke (boxed)     | 2.56M            | 2.56M                       | 1.00x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 5.10M            | 659.97M                     | 7.09M                       | 0.01x               | 0.72x                 |
| Instance Field Set (typed)     | 5.40M            | 660.28M                     | 3.91M                       | 0.01x               | 1.38x                 |
| Static Field Get (typed)       | 8.49M            | 682.80M                     | 8.71M                       | 0.01x               | 0.98x                 |
| Static Field Set (typed)       | 6.09M            | 658.03M                     | 6.29M                       | 0.01x               | 0.97x                 |
| Instance Property Get (typed)  | 654.68M          | 685.65M                     | 24.95M                      | 0.95x               | 26.24x                |
| Instance Property Set (typed)  | 658.57M          | 693.61M                     | 2.13M                       | 0.95x               | 309.81x               |
| Static Property Get (typed)    | 647.05M          | 682.71M                     | 25.67M                      | 0.95x               | 25.21x                |
| Static Property Set (typed)    | 667.03M          | 656.93M                     | 2.96M                       | 1.02x               | 225.19x               |
| Instance Method Invoke (typed) | 627.89M          | 677.32M                     | 1.93M                       | 0.93x               | 324.69x               |
| Static Method Invoke (typed)   | 617.75M          | 676.62M                     | 2.67M                       | 0.91x               | 231.77x               |

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
