# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-12-09 03:53:06 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 18.11M           | 5.18M                       | 3.50x                 |
| Instance Field Set (boxed)     | 25.08M           | 5.46M                       | 4.59x                 |
| Static Field Get (boxed)       | 24.74M           | 6.44M                       | 3.84x                 |
| Static Field Set (boxed)       | 24.99M           | 6.25M                       | 4.00x                 |
| Instance Property Get (boxed)  | 10.53M           | 2.71M                       | 3.89x                 |
| Instance Property Set (boxed)  | 19.26M           | 2.03M                       | 9.47x                 |
| Static Property Get (boxed)    | 21.67M           | 24.86M                      | 0.87x                 |
| Static Property Set (boxed)    | 26.26M           | 1.89M                       | 13.89x                |
| Instance Method Invoke (boxed) | 25.22M           | 1.99M                       | 12.66x                |
| Static Method Invoke (boxed)   | 26.27M           | 2.71M                       | 9.70x                 |
| Constructor Invoke (boxed)     | 23.25M           | 2.18M                       | 10.64x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 676.69M          | 672.59M                     | 5.18M                       | 1.01x               | 130.76x               |
| Instance Field Set (typed)     | 656.82M          | 667.07M                     | 5.46M                       | 0.98x               | 120.32x               |
| Static Field Get (typed)       | 659.33M          | 690.85M                     | 6.44M                       | 0.95x               | 102.41x               |
| Static Field Set (typed)       | 659.01M          | 661.11M                     | 6.25M                       | 1.00x               | 105.40x               |
| Instance Property Get (typed)  | 648.75M          | 695.30M                     | 2.71M                       | 0.93x               | 239.35x               |
| Instance Property Set (typed)  | 646.19M          | 698.55M                     | 2.03M                       | 0.93x               | 317.82x               |
| Static Property Get (typed)    | 661.12M          | 692.27M                     | 24.86M                      | 0.96x               | 26.59x                |
| Static Property Set (typed)    | 675.04M          | 661.70M                     | 1.89M                       | 1.02x               | 357.12x               |
| Instance Method Invoke (typed) | 613.02M          | 690.12M                     | 1.99M                       | 0.89x               | 307.61x               |
| Static Method Invoke (typed)   | 619.88M          | 685.52M                     | 2.71M                       | 0.90x               | 228.98x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 24.68M           | 7.30M                       | 3.38x                 |
| Instance Field Set (boxed)     | 24.05M           | 4.04M                       | 5.95x                 |
| Static Field Get (boxed)       | 25.50M           | 8.65M                       | 2.95x                 |
| Static Field Set (boxed)       | 24.27M           | 4.91M                       | 4.94x                 |
| Instance Property Get (boxed)  | 27.23M           | 25.06M                      | 1.09x                 |
| Instance Property Set (boxed)  | 23.39M           | 1.77M                       | 13.22x                |
| Static Property Get (boxed)    | 17.56M           | 3.00M                       | 5.85x                 |
| Static Property Set (boxed)    | 20.46M           | 2.32M                       | 8.83x                 |
| Instance Method Invoke (boxed) | 22.38M           | 2.01M                       | 11.16x                |
| Static Method Invoke (boxed)   | 26.09M           | 2.70M                       | 9.67x                 |
| Constructor Invoke (boxed)     | 22.17M           | 2.24M                       | 9.90x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 676.41M          | 662.47M                     | 7.30M                       | 1.02x               | 92.60x                |
| Instance Field Set (typed)     | 641.40M          | 676.29M                     | 4.04M                       | 0.95x               | 158.58x               |
| Static Field Get (typed)       | 650.93M          | 692.95M                     | 8.65M                       | 0.94x               | 75.27x                |
| Static Field Set (typed)       | 665.15M          | 663.54M                     | 4.91M                       | 1.00x               | 135.46x               |
| Instance Property Get (typed)  | 646.14M          | 694.09M                     | 25.06M                      | 0.93x               | 25.79x                |
| Instance Property Set (typed)  | 646.77M          | 704.42M                     | 1.77M                       | 0.92x               | 365.65x               |
| Static Property Get (typed)    | 663.59M          | 696.80M                     | 3.00M                       | 0.95x               | 221.24x               |
| Static Property Set (typed)    | 667.51M          | 666.70M                     | 2.32M                       | 1.00x               | 287.90x               |
| Instance Method Invoke (typed) | 631.50M          | 691.10M                     | 2.01M                       | 0.91x               | 314.84x               |
| Static Method Invoke (typed)   | 618.17M          | 685.03M                     | 2.70M                       | 0.90x               | 229.13x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 24.23M           | 7.37M                       | 3.29x                 |
| Instance Field Set (boxed)     | 25.88M           | 4.10M                       | 6.31x                 |
| Static Field Get (boxed)       | 25.58M           | 8.54M                       | 3.00x                 |
| Static Field Set (boxed)       | 22.79M           | 5.36M                       | 4.25x                 |
| Instance Property Get (boxed)  | 26.72M           | 25.12M                      | 1.06x                 |
| Instance Property Set (boxed)  | 23.40M           | 1.79M                       | 13.10x                |
| Static Property Get (boxed)    | 23.49M           | 25.66M                      | 0.92x                 |
| Static Property Set (boxed)    | 26.80M           | 2.05M                       | 13.07x                |
| Instance Method Invoke (boxed) | 17.91M           | 1.43M                       | 12.52x                |
| Static Method Invoke (boxed)   | 2.34M            | 2.73M                       | 0.86x                 |
| Constructor Invoke (boxed)     | 25.89M           | 2.57M                       | 10.06x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 673.27M          | 673.00M                     | 7.37M                       | 1.00x               | 91.39x                |
| Instance Field Set (typed)     | 647.27M          | 666.89M                     | 4.10M                       | 0.97x               | 157.81x               |
| Static Field Get (typed)       | 652.80M          | 693.33M                     | 8.54M                       | 0.94x               | 76.45x                |
| Static Field Set (typed)       | 668.14M          | 663.81M                     | 5.36M                       | 1.01x               | 124.72x               |
| Instance Property Get (typed)  | 664.33M          | 696.31M                     | 25.12M                      | 0.95x               | 26.44x                |
| Instance Property Set (typed)  | 655.35M          | 702.29M                     | 1.79M                       | 0.93x               | 367.04x               |
| Static Property Get (typed)    | 655.00M          | 693.05M                     | 25.66M                      | 0.95x               | 25.53x                |
| Static Property Set (typed)    | 664.40M          | 667.26M                     | 2.05M                       | 1.00x               | 324.04x               |
| Instance Method Invoke (typed) | 624.57M          | 688.12M                     | 1.43M                       | 0.91x               | 436.69x               |
| Static Method Invoke (typed)   | 619.30M          | 685.57M                     | 2.73M                       | 0.90x               | 226.73x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 7.49M            | 5.41M                       | 1.38x                 |
| Instance Field Set (boxed)     | 5.52M            | 5.55M                       | 0.99x                 |
| Static Field Get (boxed)       | 6.68M            | 8.72M                       | 0.77x                 |
| Static Field Set (boxed)       | 6.28M            | 4.76M                       | 1.32x                 |
| Instance Property Get (boxed)  | 24.48M           | 25.47M                      | 0.96x                 |
| Instance Property Set (boxed)  | 2.07M            | 1.88M                       | 1.10x                 |
| Static Property Get (boxed)    | 20.73M           | 21.41M                      | 0.97x                 |
| Static Property Set (boxed)    | 2.90M            | 2.97M                       | 0.97x                 |
| Instance Method Invoke (boxed) | 2.00M            | 1.99M                       | 1.00x                 |
| Static Method Invoke (boxed)   | 1.85M            | 2.73M                       | 0.68x                 |
| Constructor Invoke (boxed)     | 2.60M            | 2.58M                       | 1.01x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 7.50M            | 672.51M                     | 5.41M                       | 0.01x               | 1.39x                 |
| Instance Field Set (typed)     | 4.19M            | 666.16M                     | 5.55M                       | 0.01x               | 0.75x                 |
| Static Field Get (typed)       | 8.82M            | 693.19M                     | 8.72M                       | 0.01x               | 1.01x                 |
| Static Field Set (typed)       | 6.32M            | 663.96M                     | 4.76M                       | 0.01x               | 1.33x                 |
| Instance Property Get (typed)  | 651.45M          | 693.62M                     | 25.47M                      | 0.94x               | 25.57x                |
| Instance Property Set (typed)  | 648.19M          | 691.96M                     | 1.88M                       | 0.94x               | 344.41x               |
| Static Property Get (typed)    | 657.72M          | 693.39M                     | 21.41M                      | 0.95x               | 30.72x                |
| Static Property Set (typed)    | 669.21M          | 666.71M                     | 2.97M                       | 1.00x               | 225.06x               |
| Instance Method Invoke (typed) | 625.14M          | 687.86M                     | 1.99M                       | 0.91x               | 313.44x               |
| Static Method Invoke (typed)   | 618.34M          | 684.71M                     | 2.73M                       | 0.90x               | 226.74x               |

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
