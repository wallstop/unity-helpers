# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-11-01 01:16:36 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 27.89M           | 6.18M                       | 4.51x                 |
| Instance Field Set (boxed)     | 29.76M           | 5.51M                       | 5.40x                 |
| Static Field Get (boxed)       | 22.08M           | 8.75M                       | 2.52x                 |
| Static Field Set (boxed)       | 24.76M           | 6.28M                       | 3.94x                 |
| Instance Property Get (boxed)  | 29.39M           | 21.84M                      | 1.35x                 |
| Instance Property Set (boxed)  | 29.69M           | 1.68M                       | 17.72x                |
| Static Property Get (boxed)    | 4.25M            | 23.97M                      | 0.18x                 |
| Static Property Set (boxed)    | 29.77M           | 2.34M                       | 12.75x                |
| Instance Method Invoke (boxed) | 25.89M           | 1.96M                       | 13.24x                |
| Static Method Invoke (boxed)   | 23.74M           | 2.67M                       | 8.88x                 |
| Constructor Invoke (boxed)     | 28.93M           | 2.56M                       | 11.29x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 586.42M          | 595.59M                     | 6.18M                       | 0.98x               | 94.85x                |
| Instance Field Set (typed)     | 638.30M          | 666.89M                     | 5.51M                       | 0.96x               | 115.78x               |
| Static Field Get (typed)       | 606.26M          | 704.87M                     | 8.75M                       | 0.86x               | 69.31x                |
| Static Field Set (typed)       | 622.13M          | 665.91M                     | 6.28M                       | 0.93x               | 98.99x                |
| Instance Property Get (typed)  | 593.69M          | 693.12M                     | 21.84M                      | 0.86x               | 27.18x                |
| Instance Property Set (typed)  | 627.08M          | 693.14M                     | 1.68M                       | 0.90x               | 374.21x               |
| Static Property Get (typed)    | 627.16M          | 693.29M                     | 23.97M                      | 0.90x               | 26.16x                |
| Static Property Set (typed)    | 619.97M          | 663.26M                     | 2.34M                       | 0.93x               | 265.43x               |
| Instance Method Invoke (typed) | 582.57M          | 685.93M                     | 1.96M                       | 0.85x               | 297.92x               |
| Static Method Invoke (typed)   | 634.87M          | 685.50M                     | 2.67M                       | 0.93x               | 237.39x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 19.70M           | 7.40M                       | 2.66x                 |
| Instance Field Set (boxed)     | 27.91M           | 4.93M                       | 5.66x                 |
| Static Field Get (boxed)       | 28.27M           | 7.08M                       | 3.99x                 |
| Static Field Set (boxed)       | 29.07M           | 5.13M                       | 5.66x                 |
| Instance Property Get (boxed)  | 29.90M           | 22.31M                      | 1.34x                 |
| Instance Property Set (boxed)  | 29.82M           | 2.05M                       | 14.54x                |
| Static Property Get (boxed)    | 5.55M            | 18.79M                      | 0.30x                 |
| Static Property Set (boxed)    | 26.48M           | 2.92M                       | 9.07x                 |
| Instance Method Invoke (boxed) | 20.81M           | 1.95M                       | 10.66x                |
| Static Method Invoke (boxed)   | 29.39M           | 2.68M                       | 10.98x                |
| Constructor Invoke (boxed)     | 23.88M           | 2.57M                       | 9.30x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 600.38M          | 675.15M                     | 7.40M                       | 0.89x               | 81.08x                |
| Instance Field Set (typed)     | 637.47M          | 666.53M                     | 4.93M                       | 0.96x               | 129.23x               |
| Static Field Get (typed)       | 599.66M          | 705.36M                     | 7.08M                       | 0.85x               | 84.64x                |
| Static Field Set (typed)       | 619.50M          | 666.08M                     | 5.13M                       | 0.93x               | 120.69x               |
| Instance Property Get (typed)  | 590.53M          | 695.28M                     | 22.31M                      | 0.85x               | 26.46x                |
| Instance Property Set (typed)  | 624.91M          | 707.07M                     | 2.05M                       | 0.88x               | 304.61x               |
| Static Property Get (typed)    | 624.53M          | 693.66M                     | 18.79M                      | 0.90x               | 33.24x                |
| Static Property Set (typed)    | 619.69M          | 662.30M                     | 2.92M                       | 0.94x               | 212.26x               |
| Instance Method Invoke (typed) | 580.91M          | 688.85M                     | 1.95M                       | 0.84x               | 297.67x               |
| Static Method Invoke (typed)   | 639.78M          | 686.03M                     | 2.68M                       | 0.93x               | 239.14x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 28.60M           | 6.29M                       | 4.55x                 |
| Instance Field Set (boxed)     | 29.48M           | 4.68M                       | 6.30x                 |
| Static Field Get (boxed)       | 29.22M           | 7.46M                       | 3.92x                 |
| Static Field Set (boxed)       | 29.51M           | 5.21M                       | 5.66x                 |
| Instance Property Get (boxed)  | 30.02M           | 22.18M                      | 1.35x                 |
| Instance Property Set (boxed)  | 29.28M           | 2.05M                       | 14.30x                |
| Static Property Get (boxed)    | 6.16M            | 18.44M                      | 0.33x                 |
| Static Property Set (boxed)    | 30.35M           | 2.70M                       | 11.24x                |
| Instance Method Invoke (boxed) | 22.91M           | 1.96M                       | 11.68x                |
| Static Method Invoke (boxed)   | 28.36M           | 2.24M                       | 12.67x                |
| Constructor Invoke (boxed)     | 29.31M           | 2.57M                       | 11.42x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 607.95M          | 671.93M                     | 6.29M                       | 0.90x               | 96.65x                |
| Instance Field Set (typed)     | 638.42M          | 666.75M                     | 4.68M                       | 0.96x               | 136.47x               |
| Static Field Get (typed)       | 619.46M          | 704.28M                     | 7.46M                       | 0.88x               | 83.05x                |
| Static Field Set (typed)       | 637.16M          | 666.30M                     | 5.21M                       | 0.96x               | 122.20x               |
| Instance Property Get (typed)  | 608.53M          | 693.07M                     | 22.18M                      | 0.88x               | 27.44x                |
| Instance Property Set (typed)  | 631.49M          | 698.80M                     | 2.05M                       | 0.90x               | 308.35x               |
| Static Property Get (typed)    | 624.23M          | 693.70M                     | 18.44M                      | 0.90x               | 33.86x                |
| Static Property Set (typed)    | 620.18M          | 663.68M                     | 2.70M                       | 0.93x               | 229.56x               |
| Instance Method Invoke (typed) | 589.79M          | 687.20M                     | 1.96M                       | 0.86x               | 300.83x               |
| Static Method Invoke (typed)   | 639.28M          | 685.98M                     | 2.24M                       | 0.93x               | 285.60x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 6.15M            | 7.45M                       | 0.82x                 |
| Instance Field Set (boxed)     | 5.55M            | 4.63M                       | 1.20x                 |
| Static Field Get (boxed)       | 8.68M            | 7.44M                       | 1.17x                 |
| Static Field Set (boxed)       | 6.24M            | 6.25M                       | 1.00x                 |
| Instance Property Get (boxed)  | 22.47M           | 27.07M                      | 0.83x                 |
| Instance Property Set (boxed)  | 2.04M            | 1.97M                       | 1.03x                 |
| Static Property Get (boxed)    | 20.33M           | 27.58M                      | 0.74x                 |
| Static Property Set (boxed)    | 2.85M            | 2.13M                       | 1.34x                 |
| Instance Method Invoke (boxed) | 1.98M            | 1.94M                       | 1.02x                 |
| Static Method Invoke (boxed)   | 2.65M            | 2.68M                       | 0.99x                 |
| Constructor Invoke (boxed)     | 2.55M            | 2.49M                       | 1.02x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 5.75M            | 671.09M                     | 7.45M                       | 0.01x               | 0.77x                 |
| Instance Field Set (typed)     | 5.59M            | 666.95M                     | 4.63M                       | 0.01x               | 1.21x                 |
| Static Field Get (typed)       | 7.28M            | 705.32M                     | 7.44M                       | 0.01x               | 0.98x                 |
| Static Field Set (typed)       | 6.25M            | 666.22M                     | 6.25M                       | 0.01x               | 1.00x                 |
| Instance Property Get (typed)  | 592.82M          | 694.24M                     | 27.07M                      | 0.85x               | 21.90x                |
| Instance Property Set (typed)  | 632.82M          | 698.16M                     | 1.97M                       | 0.91x               | 321.58x               |
| Static Property Get (typed)    | 624.42M          | 692.15M                     | 27.58M                      | 0.90x               | 22.64x                |
| Static Property Set (typed)    | 620.73M          | 664.87M                     | 2.13M                       | 0.93x               | 291.17x               |
| Instance Method Invoke (typed) | 573.62M          | 687.86M                     | 1.94M                       | 0.83x               | 295.48x               |
| Static Method Invoke (typed)   | 640.22M          | 685.85M                     | 2.68M                       | 0.93x               | 238.87x               |

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
