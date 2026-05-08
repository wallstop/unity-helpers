# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2026-05-08 04:08:17 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">23.81M</td><td align="right">4.99M</td><td align="right">4.77x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">23.25M</td><td align="right">5.47M</td><td align="right">4.25x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">23.45M</td><td align="right">8.68M</td><td align="right">2.70x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">23.68M</td><td align="right">5.56M</td><td align="right">4.26x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">23.64M</td><td align="right">22.38M</td><td align="right">1.06x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">20.22M</td><td align="right">1.96M</td><td align="right">10.29x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">18.00M</td><td align="right">19.96M</td><td align="right">0.90x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">20.67M</td><td align="right">2.90M</td><td align="right">7.11x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">14.26M</td><td align="right">2.01M</td><td align="right">7.10x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">19.86M</td><td align="right">2.73M</td><td align="right">7.27x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">18.76M</td><td align="right">2.60M</td><td align="right">7.23x</td></tr>
  </tbody>
</table>

#### Typed Access (no boxing)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">Baseline Delegate (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Delegate</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">682.40M</td><td align="right">672.50M</td><td align="right">4.99M</td><td align="right">1.01x</td><td align="right">136.65x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">655.28M</td><td align="right">666.57M</td><td align="right">5.47M</td><td align="right">0.98x</td><td align="right">119.89x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">662.61M</td><td align="right">704.40M</td><td align="right">8.68M</td><td align="right">0.94x</td><td align="right">76.35x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">666.25M</td><td align="right">673.08M</td><td align="right">5.56M</td><td align="right">0.99x</td><td align="right">119.78x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">682.28M</td><td align="right">691.91M</td><td align="right">22.38M</td><td align="right">0.99x</td><td align="right">30.49x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">618.65M</td><td align="right">699.50M</td><td align="right">1.96M</td><td align="right">0.88x</td><td align="right">315.03x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">670.50M</td><td align="right">691.80M</td><td align="right">19.96M</td><td align="right">0.97x</td><td align="right">33.59x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">653.06M</td><td align="right">663.93M</td><td align="right">2.90M</td><td align="right">0.98x</td><td align="right">224.83x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">641.83M</td><td align="right">687.16M</td><td align="right">2.01M</td><td align="right">0.93x</td><td align="right">319.63x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">660.03M</td><td align="right">687.85M</td><td align="right">2.73M</td><td align="right">0.96x</td><td align="right">241.58x</td></tr>
  </tbody>
</table>

### Strategy: Expressions

#### Boxed Access (object)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">18.84M</td><td align="right">3.12M</td><td align="right">6.04x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">1.90M</td><td align="right">5.46M</td><td align="right">0.35x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">23.16M</td><td align="right">8.68M</td><td align="right">2.67x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">21.85M</td><td align="right">6.17M</td><td align="right">3.54x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">20.84M</td><td align="right">19.82M</td><td align="right">1.05x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">21.13M</td><td align="right">2.05M</td><td align="right">10.31x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">16.07M</td><td align="right">19.55M</td><td align="right">0.82x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">21.60M</td><td align="right">2.90M</td><td align="right">7.46x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">16.21M</td><td align="right">1.97M</td><td align="right">8.21x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">19.85M</td><td align="right">2.73M</td><td align="right">7.27x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">21.50M</td><td align="right">2.61M</td><td align="right">8.23x</td></tr>
  </tbody>
</table>

#### Typed Access (no boxing)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">Baseline Delegate (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Delegate</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">680.40M</td><td align="right">701.15M</td><td align="right">3.12M</td><td align="right">0.97x</td><td align="right">218.25x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">651.97M</td><td align="right">670.03M</td><td align="right">5.46M</td><td align="right">0.97x</td><td align="right">119.51x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">656.65M</td><td align="right">704.16M</td><td align="right">8.68M</td><td align="right">0.93x</td><td align="right">75.65x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">695.48M</td><td align="right">667.70M</td><td align="right">6.17M</td><td align="right">1.04x</td><td align="right">112.79x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">688.73M</td><td align="right">700.55M</td><td align="right">19.82M</td><td align="right">0.98x</td><td align="right">34.76x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">625.16M</td><td align="right">711.76M</td><td align="right">2.05M</td><td align="right">0.88x</td><td align="right">305.00x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">666.70M</td><td align="right">693.34M</td><td align="right">19.55M</td><td align="right">0.96x</td><td align="right">34.11x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">651.20M</td><td align="right">664.29M</td><td align="right">2.90M</td><td align="right">0.98x</td><td align="right">224.90x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">639.52M</td><td align="right">692.53M</td><td align="right">1.97M</td><td align="right">0.92x</td><td align="right">323.96x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">661.92M</td><td align="right">685.72M</td><td align="right">2.73M</td><td align="right">0.97x</td><td align="right">242.33x</td></tr>
  </tbody>
</table>

### Strategy: Dynamic IL

#### Boxed Access (object)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">22.39M</td><td align="right">7.23M</td><td align="right">3.09x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">22.97M</td><td align="right">5.44M</td><td align="right">4.22x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">23.13M</td><td align="right">8.62M</td><td align="right">2.68x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">21.14M</td><td align="right">3.73M</td><td align="right">5.66x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">21.67M</td><td align="right">21.01M</td><td align="right">1.03x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">21.50M</td><td align="right">2.04M</td><td align="right">10.52x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">19.17M</td><td align="right">4.00M</td><td align="right">4.79x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">1.46M</td><td align="right">2.14M</td><td align="right">0.68x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">17.48M</td><td align="right">1.99M</td><td align="right">8.76x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">22.05M</td><td align="right">2.76M</td><td align="right">8.00x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">21.97M</td><td align="right">2.60M</td><td align="right">8.45x</td></tr>
  </tbody>
</table>

#### Typed Access (no boxing)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">Baseline Delegate (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Delegate</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">674.63M</td><td align="right">671.86M</td><td align="right">7.23M</td><td align="right">1.00x</td><td align="right">93.25x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">650.59M</td><td align="right">666.95M</td><td align="right">5.44M</td><td align="right">0.98x</td><td align="right">119.54x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">664.67M</td><td align="right">705.04M</td><td align="right">8.62M</td><td align="right">0.94x</td><td align="right">77.15x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">668.36M</td><td align="right">666.33M</td><td align="right">3.73M</td><td align="right">1.00x</td><td align="right">179.07x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">685.34M</td><td align="right">696.24M</td><td align="right">21.01M</td><td align="right">0.98x</td><td align="right">32.62x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">624.44M</td><td align="right">703.63M</td><td align="right">2.04M</td><td align="right">0.89x</td><td align="right">305.67x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">666.45M</td><td align="right">693.56M</td><td align="right">4.00M</td><td align="right">0.96x</td><td align="right">166.47x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">649.92M</td><td align="right">663.82M</td><td align="right">2.14M</td><td align="right">0.98x</td><td align="right">303.92x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">628.36M</td><td align="right">687.84M</td><td align="right">1.99M</td><td align="right">0.91x</td><td align="right">315.12x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">661.83M</td><td align="right">685.32M</td><td align="right">2.76M</td><td align="right">0.97x</td><td align="right">240.04x</td></tr>
  </tbody>
</table>

### Strategy: Reflection Fallback

#### Boxed Access (object)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">7.47M</td><td align="right">7.34M</td><td align="right">1.02x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">3.57M</td><td align="right">5.40M</td><td align="right">0.66x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">8.75M</td><td align="right">8.61M</td><td align="right">1.02x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">3.65M</td><td align="right">6.16M</td><td align="right">0.59x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">18.75M</td><td align="right">19.68M</td><td align="right">0.95x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">2.07M</td><td align="right">2.06M</td><td align="right">1.00x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">13.46M</td><td align="right">19.11M</td><td align="right">0.70x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">2.80M</td><td align="right">2.89M</td><td align="right">0.97x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">1.96M</td><td align="right">2.00M</td><td align="right">0.98x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">2.72M</td><td align="right">2.72M</td><td align="right">1.00x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">1.80M</td><td align="right">1.97M</td><td align="right">0.91x</td></tr>
  </tbody>
</table>

#### Typed Access (no boxing)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">Baseline Delegate (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Delegate</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">7.47M</td><td align="right">671.96M</td><td align="right">7.34M</td><td align="right">0.01x</td><td align="right">1.02x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">5.40M</td><td align="right">666.41M</td><td align="right">5.40M</td><td align="right">0.01x</td><td align="right">1.00x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">8.74M</td><td align="right">704.42M</td><td align="right">8.61M</td><td align="right">0.01x</td><td align="right">1.02x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">4.23M</td><td align="right">668.03M</td><td align="right">6.16M</td><td align="right">0.01x</td><td align="right">0.69x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">683.71M</td><td align="right">697.33M</td><td align="right">19.68M</td><td align="right">0.98x</td><td align="right">34.73x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">625.56M</td><td align="right">704.49M</td><td align="right">2.06M</td><td align="right">0.89x</td><td align="right">303.34x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">653.67M</td><td align="right">693.08M</td><td align="right">19.11M</td><td align="right">0.94x</td><td align="right">34.20x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">672.77M</td><td align="right">663.41M</td><td align="right">2.89M</td><td align="right">1.01x</td><td align="right">233.07x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">640.56M</td><td align="right">688.12M</td><td align="right">2.00M</td><td align="right">0.93x</td><td align="right">320.73x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">663.79M</td><td align="right">685.82M</td><td align="right">2.72M</td><td align="right">0.97x</td><td align="right">244.25x</td></tr>
  </tbody>
</table>

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
