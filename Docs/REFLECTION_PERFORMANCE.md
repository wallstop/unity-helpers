# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-11-01 02:00:00 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 27.40M           | 5.95M                       | 4.61x                 |
| Instance Field Set (boxed)     | 27.96M           | 5.53M                       | 5.06x                 |
| Static Field Get (boxed)       | 18.43M           | 8.71M                       | 2.12x                 |
| Static Field Set (boxed)       | 27.04M           | 5.32M                       | 5.09x                 |
| Instance Property Get (boxed)  | 14.59M           | 7.26M                       | 2.01x                 |
| Instance Property Set (boxed)  | 28.92M           | 1.56M                       | 18.56x                |
| Static Property Get (boxed)    | 26.97M           | 25.64M                      | 1.05x                 |
| Static Property Set (boxed)    | 22.93M           | 2.88M                       | 7.98x                 |
| Instance Method Invoke (boxed) | 22.87M           | 1.82M                       | 12.60x                |
| Static Method Invoke (boxed)   | 23.61M           | 2.69M                       | 8.79x                 |
| Constructor Invoke (boxed)     | 26.69M           | 2.16M                       | 12.37x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 599.17M          | 679.43M                     | 5.95M                       | 0.88x               | 100.73x               |
| Instance Field Set (typed)     | 632.62M          | 666.08M                     | 5.53M                       | 0.95x               | 114.49x               |
| Static Field Get (typed)       | 624.56M          | 692.57M                     | 8.71M                       | 0.90x               | 71.70x                |
| Static Field Set (typed)       | 619.60M          | 664.57M                     | 5.32M                       | 0.93x               | 116.54x               |
| Instance Property Get (typed)  | 597.60M          | 693.89M                     | 7.26M                       | 0.86x               | 82.29x                |
| Instance Property Set (typed)  | 629.46M          | 705.79M                     | 1.56M                       | 0.89x               | 403.93x               |
| Static Property Get (typed)    | 603.61M          | 704.30M                     | 25.64M                      | 0.86x               | 23.54x                |
| Static Property Set (typed)    | 628.48M          | 664.57M                     | 2.88M                       | 0.95x               | 218.60x               |
| Instance Method Invoke (typed) | 633.41M          | 687.80M                     | 1.82M                       | 0.92x               | 348.92x               |
| Static Method Invoke (typed)   | 599.31M          | 686.79M                     | 2.69M                       | 0.87x               | 223.16x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 28.34M           | 6.15M                       | 4.61x                 |
| Instance Field Set (boxed)     | 28.49M           | 5.55M                       | 5.14x                 |
| Static Field Get (boxed)       | 23.49M           | 8.76M                       | 2.68x                 |
| Static Field Set (boxed)       | 22.11M           | 6.26M                       | 3.53x                 |
| Instance Property Get (boxed)  | 28.70M           | 4.80M                       | 5.98x                 |
| Instance Property Set (boxed)  | 18.56M           | 1.82M                       | 10.17x                |
| Static Property Get (boxed)    | 25.24M           | 24.88M                      | 1.01x                 |
| Static Property Set (boxed)    | 26.38M           | 2.90M                       | 9.10x                 |
| Instance Method Invoke (boxed) | 21.21M           | 1.72M                       | 12.34x                |
| Static Method Invoke (boxed)   | 29.17M           | 2.67M                       | 10.91x                |
| Constructor Invoke (boxed)     | 22.09M           | 2.53M                       | 8.73x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 608.02M          | 678.17M                     | 6.15M                       | 0.90x               | 98.87x                |
| Instance Field Set (typed)     | 638.16M          | 666.54M                     | 5.55M                       | 0.96x               | 115.06x               |
| Static Field Get (typed)       | 624.53M          | 692.89M                     | 8.76M                       | 0.90x               | 71.27x                |
| Static Field Set (typed)       | 620.24M          | 663.51M                     | 6.26M                       | 0.93x               | 99.05x                |
| Instance Property Get (typed)  | 604.12M          | 695.95M                     | 4.80M                       | 0.87x               | 125.85x               |
| Instance Property Set (typed)  | 637.64M          | 703.79M                     | 1.82M                       | 0.91x               | 349.48x               |
| Static Property Get (typed)    | 603.33M          | 704.56M                     | 24.88M                      | 0.86x               | 24.25x                |
| Static Property Set (typed)    | 627.49M          | 664.32M                     | 2.90M                       | 0.94x               | 216.39x               |
| Instance Method Invoke (typed) | 632.88M          | 687.55M                     | 1.72M                       | 0.92x               | 368.17x               |
| Static Method Invoke (typed)   | 589.19M          | 687.20M                     | 2.67M                       | 0.86x               | 220.35x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 29.07M           | 6.19M                       | 4.69x                 |
| Instance Field Set (boxed)     | 27.13M           | 4.54M                       | 5.97x                 |
| Static Field Get (boxed)       | 29.16M           | 7.15M                       | 4.08x                 |
| Static Field Set (boxed)       | 28.77M           | 6.05M                       | 4.75x                 |
| Instance Property Get (boxed)  | 24.30M           | 14.03M                      | 1.73x                 |
| Instance Property Set (boxed)  | 12.17M           | 2.02M                       | 6.03x                 |
| Static Property Get (boxed)    | 20.48M           | 27.16M                      | 0.75x                 |
| Static Property Set (boxed)    | 26.51M           | 2.61M                       | 10.15x                |
| Instance Method Invoke (boxed) | 23.56M           | 1.96M                       | 11.99x                |
| Static Method Invoke (boxed)   | 21.84M           | 2.68M                       | 8.16x                 |
| Constructor Invoke (boxed)     | 27.73M           | 2.52M                       | 10.99x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 613.42M          | 678.14M                     | 6.19M                       | 0.90x               | 99.03x                |
| Instance Field Set (typed)     | 637.98M          | 666.62M                     | 4.54M                       | 0.96x               | 140.44x               |
| Static Field Get (typed)       | 640.79M          | 694.02M                     | 7.15M                       | 0.92x               | 89.57x                |
| Static Field Set (typed)       | 632.35M          | 665.19M                     | 6.05M                       | 0.95x               | 104.47x               |
| Instance Property Get (typed)  | 594.07M          | 694.97M                     | 14.03M                      | 0.85x               | 42.33x                |
| Instance Property Set (typed)  | 635.86M          | 705.62M                     | 2.02M                       | 0.90x               | 315.16x               |
| Static Property Get (typed)    | 605.72M          | 698.38M                     | 27.16M                      | 0.87x               | 22.30x                |
| Static Property Set (typed)    | 624.85M          | 664.61M                     | 2.61M                       | 0.94x               | 239.27x               |
| Instance Method Invoke (typed) | 630.44M          | 687.72M                     | 1.96M                       | 0.92x               | 320.92x               |
| Static Method Invoke (typed)   | 609.86M          | 685.47M                     | 2.68M                       | 0.89x               | 227.89x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 6.07M            | 7.42M                       | 0.82x                 |
| Instance Field Set (boxed)     | 5.52M            | 4.56M                       | 1.21x                 |
| Static Field Get (boxed)       | 8.64M            | 7.12M                       | 1.21x                 |
| Static Field Set (boxed)       | 6.18M            | 6.30M                       | 0.98x                 |
| Instance Property Get (boxed)  | 21.49M           | 27.58M                      | 0.78x                 |
| Instance Property Set (boxed)  | 2.06M            | 1.74M                       | 1.18x                 |
| Static Property Get (boxed)    | 22.33M           | 27.32M                      | 0.82x                 |
| Static Property Set (boxed)    | 2.03M            | 2.92M                       | 0.70x                 |
| Instance Method Invoke (boxed) | 2.01M            | 1.98M                       | 1.02x                 |
| Static Method Invoke (boxed)   | 2.68M            | 2.68M                       | 1.00x                 |
| Constructor Invoke (boxed)     | 1.69M            | 1.67M                       | 1.01x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 2.69M            | 677.84M                     | 7.42M                       | 0.00x               | 0.36x                 |
| Instance Field Set (typed)     | 5.61M            | 666.69M                     | 4.56M                       | 0.01x               | 1.23x                 |
| Static Field Get (typed)       | 8.46M            | 694.37M                     | 7.12M                       | 0.01x               | 1.19x                 |
| Static Field Set (typed)       | 5.10M            | 662.97M                     | 6.30M                       | 0.01x               | 0.81x                 |
| Instance Property Get (typed)  | 586.00M          | 696.85M                     | 27.58M                      | 0.84x               | 21.24x                |
| Instance Property Set (typed)  | 635.88M          | 704.08M                     | 1.74M                       | 0.90x               | 365.43x               |
| Static Property Get (typed)    | 604.50M          | 701.68M                     | 27.32M                      | 0.86x               | 22.13x                |
| Static Property Set (typed)    | 617.76M          | 664.25M                     | 2.92M                       | 0.93x               | 211.68x               |
| Instance Method Invoke (typed) | 632.74M          | 688.70M                     | 1.98M                       | 0.92x               | 319.78x               |
| Static Method Invoke (typed)   | 608.62M          | 686.50M                     | 2.68M                       | 0.89x               | 227.10x               |

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
