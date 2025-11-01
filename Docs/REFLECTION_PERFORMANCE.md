# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-11-01 02:10:30 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 26.90M           | 6.43M                       | 4.18x                 |
| Instance Field Set (boxed)     | 28.65M           | 4.50M                       | 6.36x                 |
| Static Field Get (boxed)       | 28.95M           | 7.23M                       | 4.00x                 |
| Static Field Set (boxed)       | 28.12M           | 6.28M                       | 4.48x                 |
| Instance Property Get (boxed)  | 24.12M           | 26.34M                      | 0.92x                 |
| Instance Property Set (boxed)  | 27.55M           | 1.51M                       | 18.25x                |
| Static Property Get (boxed)    | 26.47M           | 27.45M                      | 0.96x                 |
| Static Property Set (boxed)    | 22.39M           | 2.93M                       | 7.65x                 |
| Instance Method Invoke (boxed) | 23.55M           | 1.97M                       | 11.98x                |
| Static Method Invoke (boxed)   | 22.89M           | 2.67M                       | 8.58x                 |
| Constructor Invoke (boxed)     | 18.01M           | 1.70M                       | 10.58x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 544.26M          | 614.88M                     | 6.43M                       | 0.89x               | 84.68x                |
| Instance Field Set (typed)     | 636.55M          | 669.32M                     | 4.50M                       | 0.95x               | 141.37x               |
| Static Field Get (typed)       | 608.91M          | 694.14M                     | 7.23M                       | 0.88x               | 84.20x                |
| Static Field Set (typed)       | 623.04M          | 664.87M                     | 6.28M                       | 0.94x               | 99.27x                |
| Instance Property Get (typed)  | 594.54M          | 695.13M                     | 26.34M                      | 0.86x               | 22.57x                |
| Instance Property Set (typed)  | 637.69M          | 700.01M                     | 1.51M                       | 0.91x               | 422.38x               |
| Static Property Get (typed)    | 626.49M          | 692.86M                     | 27.45M                      | 0.90x               | 22.82x                |
| Static Property Set (typed)    | 624.91M          | 662.31M                     | 2.93M                       | 0.94x               | 213.60x               |
| Instance Method Invoke (typed) | 592.71M          | 688.29M                     | 1.97M                       | 0.86x               | 301.58x               |
| Static Method Invoke (typed)   | 596.62M          | 685.29M                     | 2.67M                       | 0.87x               | 223.62x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 28.61M           | 7.44M                       | 3.84x                 |
| Instance Field Set (boxed)     | 20.64M           | 5.46M                       | 3.78x                 |
| Static Field Get (boxed)       | 28.66M           | 7.03M                       | 4.08x                 |
| Static Field Set (boxed)       | 27.93M           | 6.28M                       | 4.45x                 |
| Instance Property Get (boxed)  | 21.52M           | 25.54M                      | 0.84x                 |
| Instance Property Set (boxed)  | 26.48M           | 1.71M                       | 15.50x                |
| Static Property Get (boxed)    | 22.02M           | 25.15M                      | 0.88x                 |
| Static Property Set (boxed)    | 27.33M           | 2.14M                       | 12.79x                |
| Instance Method Invoke (boxed) | 23.92M           | 1.95M                       | 12.28x                |
| Static Method Invoke (boxed)   | 27.02M           | 2.09M                       | 12.95x                |
| Constructor Invoke (boxed)     | 28.46M           | 2.60M                       | 10.96x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 608.49M          | 669.38M                     | 7.44M                       | 0.91x               | 81.76x                |
| Instance Field Set (typed)     | 636.85M          | 666.03M                     | 5.46M                       | 0.96x               | 116.70x               |
| Static Field Get (typed)       | 600.54M          | 692.53M                     | 7.03M                       | 0.87x               | 85.40x                |
| Static Field Set (typed)       | 621.80M          | 665.52M                     | 6.28M                       | 0.93x               | 99.05x                |
| Instance Property Get (typed)  | 586.55M          | 696.86M                     | 25.54M                      | 0.84x               | 22.96x                |
| Instance Property Set (typed)  | 637.48M          | 700.05M                     | 1.71M                       | 0.91x               | 373.18x               |
| Static Property Get (typed)    | 626.64M          | 692.26M                     | 25.15M                      | 0.91x               | 24.92x                |
| Static Property Set (typed)    | 617.44M          | 652.06M                     | 2.14M                       | 0.95x               | 288.83x               |
| Instance Method Invoke (typed) | 588.93M          | 680.02M                     | 1.95M                       | 0.87x               | 302.26x               |
| Static Method Invoke (typed)   | 604.99M          | 677.46M                     | 2.09M                       | 0.89x               | 289.97x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 9.63M            | 3.55M                       | 2.71x                 |
| Instance Field Set (boxed)     | 28.14M           | 5.53M                       | 5.09x                 |
| Static Field Get (boxed)       | 21.41M           | 8.79M                       | 2.43x                 |
| Static Field Set (boxed)       | 28.71M           | 5.03M                       | 5.71x                 |
| Instance Property Get (boxed)  | 28.48M           | 24.35M                      | 1.17x                 |
| Instance Property Set (boxed)  | 23.24M           | 2.07M                       | 11.20x                |
| Static Property Get (boxed)    | 25.03M           | 22.55M                      | 1.11x                 |
| Static Property Set (boxed)    | 28.12M           | 2.86M                       | 9.84x                 |
| Instance Method Invoke (boxed) | 21.85M           | 1.60M                       | 13.65x                |
| Static Method Invoke (boxed)   | 26.94M           | 2.66M                       | 10.14x                |
| Constructor Invoke (boxed)     | 26.74M           | 2.27M                       | 11.79x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 596.97M          | 665.13M                     | 3.55M                       | 0.90x               | 168.16x               |
| Instance Field Set (typed)     | 628.92M          | 658.63M                     | 5.53M                       | 0.95x               | 113.81x               |
| Static Field Get (typed)       | 610.06M          | 693.76M                     | 8.79M                       | 0.88x               | 69.40x                |
| Static Field Set (typed)       | 629.61M          | 658.60M                     | 5.03M                       | 0.96x               | 125.18x               |
| Instance Property Get (typed)  | 595.85M          | 687.51M                     | 24.35M                      | 0.87x               | 24.47x                |
| Instance Property Set (typed)  | 631.56M          | 692.86M                     | 2.07M                       | 0.91x               | 304.37x               |
| Static Property Get (typed)    | 620.22M          | 684.44M                     | 22.55M                      | 0.91x               | 27.51x                |
| Static Property Set (typed)    | 611.44M          | 655.34M                     | 2.86M                       | 0.93x               | 213.92x               |
| Instance Method Invoke (typed) | 588.65M          | 680.04M                     | 1.60M                       | 0.87x               | 367.87x               |
| Static Method Invoke (typed)   | 600.46M          | 677.57M                     | 2.66M                       | 0.89x               | 226.04x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 7.29M            | 7.32M                       | 0.99x                 |
| Instance Field Set (boxed)     | 4.40M            | 5.47M                       | 0.80x                 |
| Static Field Get (boxed)       | 8.55M            | 4.84M                       | 1.77x                 |
| Static Field Set (boxed)       | 2.72M            | 4.55M                       | 0.60x                 |
| Instance Property Get (boxed)  | 26.15M           | 26.84M                      | 0.97x                 |
| Instance Property Set (boxed)  | 1.81M            | 2.10M                       | 0.86x                 |
| Static Property Get (boxed)    | 22.27M           | 26.45M                      | 0.84x                 |
| Static Property Set (boxed)    | 2.55M            | 2.96M                       | 0.86x                 |
| Instance Method Invoke (boxed) | 2.00M            | 1.97M                       | 1.02x                 |
| Static Method Invoke (boxed)   | 2.47M            | 1.91M                       | 1.30x                 |
| Constructor Invoke (boxed)     | 2.55M            | 2.57M                       | 0.99x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 7.28M            | 625.84M                     | 7.32M                       | 0.01x               | 0.99x                 |
| Instance Field Set (typed)     | 5.29M            | 654.42M                     | 5.47M                       | 0.01x               | 0.97x                 |
| Static Field Get (typed)       | 7.07M            | 691.26M                     | 4.84M                       | 0.01x               | 1.46x                 |
| Static Field Set (typed)       | 6.19M            | 658.10M                     | 4.55M                       | 0.01x               | 1.36x                 |
| Instance Property Get (typed)  | 590.82M          | 688.57M                     | 26.84M                      | 0.86x               | 22.01x                |
| Instance Property Set (typed)  | 631.83M          | 696.20M                     | 2.10M                       | 0.91x               | 300.24x               |
| Static Property Get (typed)    | 626.19M          | 690.57M                     | 26.45M                      | 0.91x               | 23.67x                |
| Static Property Set (typed)    | 621.38M          | 662.49M                     | 2.96M                       | 0.94x               | 210.10x               |
| Instance Method Invoke (typed) | 587.96M          | 689.37M                     | 1.97M                       | 0.85x               | 298.85x               |
| Static Method Invoke (typed)   | 604.95M          | 685.12M                     | 1.91M                       | 0.88x               | 317.35x               |

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
