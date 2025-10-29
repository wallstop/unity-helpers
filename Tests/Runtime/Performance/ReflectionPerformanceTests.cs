namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public sealed class ReflectionPerformanceTests
    {
        private const int BatchSize = 256;
        private static readonly TimeSpan BenchmarkDuration = TimeSpan.FromMilliseconds(250);
        private static int sink;

        [Test]
        [Timeout(0)]
        public void Benchmark()
        {
            ReflectionPerfTarget instance = new ReflectionPerfTarget
            {
                InstanceField = 5,
                InstanceProperty = 7,
            };
            ReflectionPerfTarget.StaticField = 11;
            ReflectionPerfTarget.StaticProperty = 13;

            List<ScenarioResult> boxedResults = new List<ScenarioResult>();
            foreach (Scenario scenario in CreateBoxedScenarios(instance))
            {
                boxedResults.Add(RunScenario(scenario));
            }

            List<ScenarioResult> typedResults = new List<ScenarioResult>();
            foreach (Scenario scenario in CreateTypedScenarios(instance))
            {
                typedResults.Add(RunScenario(scenario));
            }

            Dictionary<string, double> reflectionBaselineLookup = new Dictionary<string, double>(
                boxedResults.Count,
                StringComparer.Ordinal
            );
            foreach (ScenarioResult result in boxedResults)
            {
                string key = GetScenarioKey(result.Name);
                reflectionBaselineLookup[key] = result.BaselineOpsPerSecond;
            }

            List<string> outputLines = new List<string>
            {
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Generated on {0:yyyy-MM-dd HH:mm:ss} UTC",
                    DateTime.UtcNow
                ),
                string.Empty,
                "### Boxed Access (object)",
                string.Empty,
                "| Scenario | ReflectionHelpers (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |",
                "| -------- | --------------------------- | --------------------------- | --------------------- |",
            };

            foreach (ScenarioResult result in boxedResults)
            {
                outputLines.Add(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "| {0} | {1} | {2} | {3:F2}x |",
                        result.Name,
                        FormatOps(result.HelperOpsPerSecond),
                        FormatOps(result.BaselineOpsPerSecond),
                        result.Speedup
                    )
                );

                UnityEngine.Debug.Log(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "[ReflectionPerf][Boxed] {0}: helpers={1:N0} ops/s, reflection={2:N0} ops/s",
                        result.Name,
                        result.HelperOpsPerSecond,
                        result.BaselineOpsPerSecond
                    )
                );
            }

            outputLines.Add(string.Empty);
            outputLines.Add("### Typed Access (no boxing)");
            outputLines.Add(string.Empty);
            outputLines.Add(
                "| Scenario | ReflectionHelpers (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |"
            );
            outputLines.Add(
                "| -------- | --------------------------- | --------------------------- | --------------------------- | ------------------- | -------------------- |"
            );

            foreach (ScenarioResult result in typedResults)
            {
                string key = GetScenarioKey(result.Name);
                double reflectionOps = reflectionBaselineLookup.TryGetValue(key, out double value)
                    ? value
                    : double.NaN;
                double speedupVsDelegate = result.Speedup;
                double speedupVsReflection =
                    reflectionOps <= 0.0
                        ? double.PositiveInfinity
                        : result.HelperOpsPerSecond / reflectionOps;

                outputLines.Add(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "| {0} | {1} | {2} | {3} | {4:F2}x | {5:F2}x |",
                        result.Name,
                        FormatOps(result.HelperOpsPerSecond),
                        FormatOps(result.BaselineOpsPerSecond),
                        FormatOps(reflectionOps),
                        speedupVsDelegate,
                        speedupVsReflection
                    )
                );

                UnityEngine.Debug.Log(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "[ReflectionPerf][Typed] {0}: helpers={1:N0} ops/s, delegate={2:N0} ops/s, reflection={3:N0} ops/s",
                        result.Name,
                        result.HelperOpsPerSecond,
                        result.BaselineOpsPerSecond,
                        reflectionOps
                    )
                );
            }

            string token = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "REFLECTION_PERFORMANCE_{0}",
                GetOsToken()
            );

            BenchmarkReadmeUpdater.UpdateSection(
                token,
                outputLines,
                "Docs/REFLECTION_PERFORMANCE.md"
            );
        }

        private static IEnumerable<Scenario> CreateBoxedScenarios(ReflectionPerfTarget instance)
        {
            Type targetType = typeof(ReflectionPerfTarget);
            FieldInfo instanceField = targetType.GetField(
                nameof(ReflectionPerfTarget.InstanceField)
            );
            FieldInfo staticField = targetType.GetField(nameof(ReflectionPerfTarget.StaticField));
            PropertyInfo instanceProperty = targetType.GetProperty(
                nameof(ReflectionPerfTarget.InstanceProperty)
            );
            PropertyInfo staticProperty = targetType.GetProperty(
                nameof(ReflectionPerfTarget.StaticProperty)
            );
            MethodInfo instanceMethod = targetType.GetMethod(nameof(ReflectionPerfTarget.Combine));
            MethodInfo staticMethod = targetType.GetMethod(
                nameof(ReflectionPerfTarget.StaticCombine)
            );
            ConstructorInfo constructor = targetType.GetConstructor(new[] { typeof(int) });

            if (
                instanceField == null
                || staticField == null
                || instanceProperty == null
                || staticProperty == null
                || instanceMethod == null
                || staticMethod == null
                || constructor == null
            )
            {
                throw new InvalidOperationException("ReflectionPerfTarget members not found.");
            }

            Func<object, object> instanceFieldGetter = ReflectionHelpers.GetFieldGetter(
                instanceField
            );
            Action<object, object> instanceFieldSetter = ReflectionHelpers.GetFieldSetter(
                instanceField
            );
            Func<object> staticFieldGetter = ReflectionHelpers.GetStaticFieldGetter(staticField);
            Action<object> staticFieldSetter = ReflectionHelpers.GetStaticFieldSetter(staticField);
            Func<object, object> instancePropertyGetter = ReflectionHelpers.GetPropertyGetter(
                instanceProperty
            );
            Action<object, object> instancePropertySetter = ReflectionHelpers.GetPropertySetter(
                instanceProperty
            );
            Func<object, object> staticPropertyGetter = ReflectionHelpers.GetPropertyGetter(
                staticProperty
            );
            Action<object, object> staticPropertySetter = ReflectionHelpers.GetPropertySetter(
                staticProperty
            );
            Func<object, object[], object> instanceMethodInvoker =
                ReflectionHelpers.GetMethodInvoker(instanceMethod);
            Func<object[], object> staticMethodInvoker = ReflectionHelpers.GetStaticMethodInvoker(
                staticMethod
            );
            Func<object[], object> constructorInvoker = ReflectionHelpers.GetConstructor(
                constructor
            );

            yield return Scenario.Create(
                "Instance Field Get (boxed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)instanceField.GetValue(instance);
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)instanceFieldGetter(instance);
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Instance Field Set (boxed)",
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        instanceField.SetValue(instance, value);
                        sink ^= instance.InstanceField;
                        value++;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        instanceFieldSetter(instance, value);
                        sink ^= instance.InstanceField;
                        value++;
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Field Get (boxed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)staticField.GetValue(null);
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)staticFieldGetter();
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Field Set (boxed)",
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        staticField.SetValue(null, value);
                        sink ^= ReflectionPerfTarget.StaticField;
                        value++;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        staticFieldSetter(value);
                        sink ^= ReflectionPerfTarget.StaticField;
                        value++;
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Instance Property Get (boxed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)instanceProperty.GetValue(instance);
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)instancePropertyGetter(instance);
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Instance Property Set (boxed)",
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        instanceProperty.SetValue(instance, value);
                        sink ^= instance.InstanceProperty;
                        value++;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        instancePropertySetter(instance, value);
                        sink ^= instance.InstanceProperty;
                        value++;
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Property Get (boxed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)staticProperty.GetValue(null);
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)staticPropertyGetter(null);
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Property Set (boxed)",
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        staticProperty.SetValue(null, value);
                        sink ^= ReflectionPerfTarget.StaticProperty;
                        value++;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        staticPropertySetter(null, value);
                        sink ^= ReflectionPerfTarget.StaticProperty;
                        value++;
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Instance Method Invoke (boxed)",
                () =>
                {
                    int count = 0;
                    object[] arguments = { 3, 5 };
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)instanceMethod.Invoke(instance, arguments);
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    object[] arguments = { 3, 5 };
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)instanceMethodInvoker(instance, arguments);
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Method Invoke (boxed)",
                () =>
                {
                    int count = 0;
                    object[] arguments = { 3, 5 };
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)staticMethod.Invoke(null, arguments);
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    object[] arguments = { 3, 5 };
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)staticMethodInvoker(arguments);
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Constructor Invoke (boxed)",
                () =>
                {
                    int count = 0;
                    object[] arguments = { 9 };
                    for (int i = 0; i < BatchSize; i++)
                    {
                        ReflectionPerfTarget created = (ReflectionPerfTarget)
                            constructor.Invoke(arguments);
                        sink ^= created.InstanceField;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    object[] arguments = { 9 };
                    for (int i = 0; i < BatchSize; i++)
                    {
                        ReflectionPerfTarget created = (ReflectionPerfTarget)constructorInvoker(
                            arguments
                        );
                        sink ^= created.InstanceField;
                        count++;
                    }

                    return count;
                }
            );
        }

        private static IEnumerable<Scenario> CreateTypedScenarios(ReflectionPerfTarget instance)
        {
            Type targetType = typeof(ReflectionPerfTarget);
            FieldInfo instanceField = targetType.GetField(
                nameof(ReflectionPerfTarget.InstanceField)
            );
            FieldInfo staticField = targetType.GetField(nameof(ReflectionPerfTarget.StaticField));
            PropertyInfo instanceProperty = targetType.GetProperty(
                nameof(ReflectionPerfTarget.InstanceProperty)
            );
            PropertyInfo staticProperty = targetType.GetProperty(
                nameof(ReflectionPerfTarget.StaticProperty)
            );
            MethodInfo instanceMethod = targetType.GetMethod(nameof(ReflectionPerfTarget.Combine));
            MethodInfo staticMethod = targetType.GetMethod(
                nameof(ReflectionPerfTarget.StaticCombine)
            );

            if (
                instanceField == null
                || staticField == null
                || instanceProperty == null
                || staticProperty == null
                || instanceMethod == null
                || staticMethod == null
            )
            {
                throw new InvalidOperationException("ReflectionPerfTarget members not found.");
            }

            Func<ReflectionPerfTarget, int> instanceFieldGetter = ReflectionHelpers.GetFieldGetter<
                ReflectionPerfTarget,
                int
            >(instanceField);
            FieldSetter<ReflectionPerfTarget, int> instanceFieldSetter =
                ReflectionHelpers.GetFieldSetter<ReflectionPerfTarget, int>(instanceField);
            Func<int> staticFieldGetter = ReflectionHelpers.GetStaticFieldGetter<int>(staticField);
            Action<int> staticFieldSetter = ReflectionHelpers.GetStaticFieldSetter<int>(
                staticField
            );
            Func<ReflectionPerfTarget, int> instancePropertyGetter =
                ReflectionHelpers.GetPropertyGetter<ReflectionPerfTarget, int>(instanceProperty);
            Action<ReflectionPerfTarget, int> instancePropertySetter =
                ReflectionHelpers.GetPropertySetter<ReflectionPerfTarget, int>(instanceProperty);
            Func<int> staticPropertyGetter = ReflectionHelpers.GetStaticPropertyGetter<int>(
                staticProperty
            );
            Action<int> staticPropertySetter = ReflectionHelpers.GetStaticPropertySetter<int>(
                staticProperty
            );
            Func<ReflectionPerfTarget, int, int, int> instanceMethodInvoker =
                ReflectionHelpers.GetInstanceMethodInvoker<ReflectionPerfTarget, int, int, int>(
                    instanceMethod
                );
            Func<int, int, int> staticMethodInvoker = ReflectionHelpers.GetStaticMethodInvoker<
                int,
                int,
                int
            >(staticMethod);

            yield return Scenario.Create(
                "Instance Field Get (typed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= instance.InstanceField;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= instanceFieldGetter(instance);
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Instance Field Set (typed)",
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        instance.InstanceField = value;
                        sink ^= instance.InstanceField;
                        value++;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    int value = 0;
                    ReflectionPerfTarget target = instance;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        instanceFieldSetter(ref target, value);
                        sink ^= target.InstanceField;
                        value++;
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Field Get (typed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= ReflectionPerfTarget.StaticField;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= staticFieldGetter();
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Field Set (typed)",
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        ReflectionPerfTarget.StaticField = value;
                        sink ^= ReflectionPerfTarget.StaticField;
                        value++;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        staticFieldSetter(value);
                        sink ^= ReflectionPerfTarget.StaticField;
                        value++;
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Instance Property Get (typed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= instance.InstanceProperty;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= instancePropertyGetter(instance);
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Instance Property Set (typed)",
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        instance.InstanceProperty = value;
                        sink ^= instance.InstanceProperty;
                        value++;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        instancePropertySetter(instance, value);
                        sink ^= instance.InstanceProperty;
                        value++;
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Property Get (typed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= ReflectionPerfTarget.StaticProperty;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= staticPropertyGetter();
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Property Set (typed)",
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        ReflectionPerfTarget.StaticProperty = value;
                        sink ^= ReflectionPerfTarget.StaticProperty;
                        value++;
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        staticPropertySetter(value);
                        sink ^= ReflectionPerfTarget.StaticProperty;
                        value++;
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Instance Method Invoke (typed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= instance.Combine(3, 5);
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= instanceMethodInvoker(instance, 3, 5);
                        count++;
                    }

                    return count;
                }
            );

            yield return Scenario.Create(
                "Static Method Invoke (typed)",
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= ReflectionPerfTarget.StaticCombine(3, 5);
                        count++;
                    }

                    return count;
                },
                () =>
                {
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= staticMethodInvoker(3, 5);
                        count++;
                    }

                    return count;
                }
            );
        }

        private static ScenarioResult RunScenario(Scenario scenario)
        {
            scenario.Warmup();

            double helperOpsPerSecond = MeasureOpsPerSecond(scenario.Helper);
            double baselineOpsPerSecond = MeasureOpsPerSecond(scenario.Baseline);

            double speedup =
                baselineOpsPerSecond <= 0.0
                    ? double.PositiveInfinity
                    : helperOpsPerSecond / baselineOpsPerSecond;

            return new ScenarioResult(
                scenario.Name,
                helperOpsPerSecond,
                baselineOpsPerSecond,
                speedup
            );
        }

        private static double MeasureOpsPerSecond(Func<int> executeBatch)
        {
            if (executeBatch == null)
            {
                return 0.0;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            long operations = 0;

            do
            {
                operations += executeBatch();
            } while (stopwatch.Elapsed < BenchmarkDuration);

            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            if (elapsedSeconds <= 0.0)
            {
                elapsedSeconds = 1.0 / Stopwatch.Frequency;
            }

            return operations / elapsedSeconds;
        }

        private static string FormatOps(double value)
        {
            const double Million = 1_000_000.0;
            const double Thousand = 1_000.0;

            if (value >= Million)
            {
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0:F2}M",
                    value / Million
                );
            }

            if (value >= Thousand)
            {
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0:F1}K",
                    value / Thousand
                );
            }

            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0:F0}",
                value
            );
        }

        private static string GetOsToken()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "WINDOWS";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "MACOS";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "LINUX";
            }

            return "UNKNOWN";
        }

        private static string GetScenarioKey(string scenarioName)
        {
            if (string.IsNullOrEmpty(scenarioName))
            {
                return string.Empty;
            }

            const string BoxedSuffix = " (boxed)";
            const string TypedSuffix = " (typed)";

            if (scenarioName.EndsWith(BoxedSuffix, StringComparison.Ordinal))
            {
                return scenarioName.Substring(0, scenarioName.Length - BoxedSuffix.Length);
            }

            if (scenarioName.EndsWith(TypedSuffix, StringComparison.Ordinal))
            {
                return scenarioName.Substring(0, scenarioName.Length - TypedSuffix.Length);
            }

            return scenarioName;
        }

        private sealed class Scenario
        {
            private Scenario(string name, Func<int> baseline, Func<int> helper)
            {
                Name = name;
                Baseline = baseline;
                Helper = helper;
            }

            public string Name { get; }

            public Func<int> Baseline { get; }

            public Func<int> Helper { get; }

            public static Scenario Create(string name, Func<int> baseline, Func<int> helper)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Scenario name must be provided.", nameof(name));
                }

                if (baseline == null)
                {
                    throw new ArgumentNullException(nameof(baseline));
                }

                if (helper == null)
                {
                    throw new ArgumentNullException(nameof(helper));
                }

                return new Scenario(name, baseline, helper);
            }

            public void Warmup()
            {
                _ = Baseline();
                _ = Helper();
            }
        }

        private readonly struct ScenarioResult
        {
            public ScenarioResult(
                string name,
                double helperOpsPerSecond,
                double baselineOpsPerSecond,
                double speedup
            )
            {
                Name = name;
                HelperOpsPerSecond = helperOpsPerSecond;
                BaselineOpsPerSecond = baselineOpsPerSecond;
                Speedup = speedup;
            }

            public string Name { get; }

            public double HelperOpsPerSecond { get; }

            public double BaselineOpsPerSecond { get; }

            public double Speedup { get; }
        }

        private sealed class ReflectionPerfTarget
        {
            public static int StaticField;

            public static int StaticProperty { get; set; }

            public int InstanceField;

            public int InstanceProperty { get; set; }

            public ReflectionPerfTarget() { }

            public ReflectionPerfTarget(int value)
            {
                InstanceField = value;
                InstanceProperty = value;
            }

            public int Combine(int first, int second)
            {
                return InstanceField + first + second;
            }

            public static int StaticCombine(int first, int second)
            {
                return StaticField + first + second;
            }
        }
    }
}
