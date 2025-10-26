namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
                "| Scenario | ReflectionHelpers (ops/sec) | System.Reflection (ops/sec) | Speedup |",
                "| -------- | --------------------------- | --------------------------- | ------- |",
            };

            foreach (ScenarioResult result in boxedResults)
            {
                outputLines.Add(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "| {0} | {1} | {2} | {3:F2}x |",
                        result.Name,
                        FormatOps(result.HelperOpsPerSecond),
                        FormatOps(result.ReflectionOpsPerSecond),
                        result.Speedup
                    )
                );

                UnityEngine.Debug.Log(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "[ReflectionPerf][Boxed] {0}: helpers={1:N0} ops/s, reflection={2:N0} ops/s",
                        result.Name,
                        result.HelperOpsPerSecond,
                        result.ReflectionOpsPerSecond
                    )
                );
            }

            outputLines.Add(string.Empty);
            outputLines.Add("### Typed Access (no boxing)");
            outputLines.Add(string.Empty);
            outputLines.Add(
                "| Scenario | ReflectionHelpers (ops/sec) | Baseline Delegate (ops/sec) | Speedup |"
            );
            outputLines.Add(
                "| -------- | --------------------------- | --------------------------- | ------- |"
            );

            foreach (ScenarioResult result in typedResults)
            {
                outputLines.Add(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "| {0} | {1} | {2} | {3:F2}x |",
                        result.Name,
                        FormatOps(result.HelperOpsPerSecond),
                        FormatOps(result.ReflectionOpsPerSecond),
                        result.Speedup
                    )
                );

                UnityEngine.Debug.Log(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "[ReflectionPerf][Typed] {0}: helpers={1:N0} ops/s, baseline={2:N0} ops/s",
                        result.Name,
                        result.HelperOpsPerSecond,
                        result.ReflectionOpsPerSecond
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
            var instanceField = targetType.GetField(nameof(ReflectionPerfTarget.InstanceField));
            var staticField = targetType.GetField(nameof(ReflectionPerfTarget.StaticField));
            var instanceProperty = targetType.GetProperty(
                nameof(ReflectionPerfTarget.InstanceProperty)
            );
            var staticProperty = targetType.GetProperty(
                nameof(ReflectionPerfTarget.StaticProperty)
            );
            var instanceMethod = targetType.GetMethod(nameof(ReflectionPerfTarget.Combine));
            var staticMethod = targetType.GetMethod(nameof(ReflectionPerfTarget.StaticCombine));
            var constructor = targetType.GetConstructor(new[] { typeof(int) });

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
                    Func<object, object> helper = ReflectionHelpers.GetFieldGetter(instanceField);
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)helper(instance);
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
                    Action<object, object> helper = ReflectionHelpers.GetFieldSetter(instanceField);
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        helper(instance, value);
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
                    Func<object> helper = ReflectionHelpers.GetStaticFieldGetter(staticField);
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)helper();
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
                    Action<object> helper = ReflectionHelpers.GetStaticFieldSetter(staticField);
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        helper(value);
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
                    Func<object, object> helper = ReflectionHelpers.GetPropertyGetter(
                        instanceProperty
                    );
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)helper(instance);
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
                    Action<object, object> helper = ReflectionHelpers.GetPropertySetter(
                        instanceProperty
                    );
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        helper(instance, value);
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
                    Func<object, object> helper = ReflectionHelpers.GetPropertyGetter(
                        staticProperty
                    );
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)helper(null);
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
                    Action<object, object> helper = ReflectionHelpers.GetPropertySetter(
                        staticProperty
                    );
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        helper(null, value);
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
                    Func<object, object[], object> helper = ReflectionHelpers.GetMethodInvoker(
                        instanceMethod
                    );
                    int count = 0;
                    object[] arguments = { 3, 5 };
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)helper(instance, arguments);
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
                    Func<object, object[], object> helper = ReflectionHelpers.GetMethodInvoker(
                        staticMethod
                    );
                    int count = 0;
                    object[] arguments = { 3, 5 };
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= (int)helper(null, arguments);
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
                    Func<object[], object> helper = ReflectionHelpers.GetConstructorInvoker(
                        constructor
                    );
                    int count = 0;
                    object[] arguments = { 9 };
                    for (int i = 0; i < BatchSize; i++)
                    {
                        ReflectionPerfTarget created = (ReflectionPerfTarget)helper(arguments);
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
            var instanceField = targetType.GetField(nameof(ReflectionPerfTarget.InstanceField));
            var staticField = targetType.GetField(nameof(ReflectionPerfTarget.StaticField));
            var instanceProperty = targetType.GetProperty(
                nameof(ReflectionPerfTarget.InstanceProperty)
            );
            var staticProperty = targetType.GetProperty(
                nameof(ReflectionPerfTarget.StaticProperty)
            );
            var instanceMethod = targetType.GetMethod(nameof(ReflectionPerfTarget.Combine));
            var staticMethod = targetType.GetMethod(nameof(ReflectionPerfTarget.StaticCombine));

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
                    Func<ReflectionPerfTarget, int> helper = ReflectionHelpers.GetFieldGetter<
                        ReflectionPerfTarget,
                        int
                    >(instanceField);
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= helper(instance);
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
                    Action<ReflectionPerfTarget, int> helper = ReflectionHelpers.GetFieldSetter<
                        ReflectionPerfTarget,
                        int
                    >(instanceField);
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        helper(instance, value);
                        sink ^= instance.InstanceField;
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
                    Func<int> helper = ReflectionHelpers.GetStaticFieldGetter<int>(staticField);
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= helper();
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
                    Action<int> helper = ReflectionHelpers.GetStaticFieldSetter<int>(staticField);
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        helper(value);
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
                    Func<ReflectionPerfTarget, int> helper = ReflectionHelpers.GetPropertyGetter<
                        ReflectionPerfTarget,
                        int
                    >(instanceProperty);
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= helper(instance);
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
                    Action<ReflectionPerfTarget, int> helper = ReflectionHelpers.GetPropertySetter<
                        ReflectionPerfTarget,
                        int
                    >(instanceProperty);
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        helper(instance, value);
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
                    Func<int> helper = ReflectionHelpers.GetStaticPropertyGetter<int>(
                        staticProperty
                    );
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= helper();
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
                    Action<int> helper = ReflectionHelpers.GetStaticPropertySetter<int>(
                        staticProperty
                    );
                    int count = 0;
                    int value = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        helper(value);
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
                    Func<ReflectionPerfTarget, int, int, int> helper =
                        ReflectionHelpers.GetInstanceMethodInvoker<
                            ReflectionPerfTarget,
                            int,
                            int,
                            int
                        >(instanceMethod);
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= helper(instance, 3, 5);
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
                    Func<int, int, int> helper = ReflectionHelpers.GetStaticMethodInvoker<
                        int,
                        int,
                        int
                    >(staticMethod);
                    int count = 0;
                    for (int i = 0; i < BatchSize; i++)
                    {
                        sink ^= helper(3, 5);
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
            double reflectionOpsPerSecond = MeasureOpsPerSecond(scenario.Reflection);

            double speedup =
                reflectionOpsPerSecond <= 0.0
                    ? double.PositiveInfinity
                    : helperOpsPerSecond / reflectionOpsPerSecond;

            return new ScenarioResult(
                scenario.Name,
                helperOpsPerSecond,
                reflectionOpsPerSecond,
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

        private sealed class Scenario
        {
            private Scenario(string name, Func<int> reflection, Func<int> helper)
            {
                Name = name;
                Reflection = reflection;
                Helper = helper;
            }

            public string Name { get; }

            public Func<int> Reflection { get; }

            public Func<int> Helper { get; }

            public static Scenario Create(string name, Func<int> reflection, Func<int> helper)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Scenario name must be provided.", nameof(name));
                }

                if (reflection == null)
                {
                    throw new ArgumentNullException(nameof(reflection));
                }

                if (helper == null)
                {
                    throw new ArgumentNullException(nameof(helper));
                }

                return new Scenario(name, reflection, helper);
            }

            public void Warmup()
            {
                _ = Reflection();
                _ = Helper();
            }
        }

        private readonly struct ScenarioResult
        {
            public ScenarioResult(
                string name,
                double helperOpsPerSecond,
                double reflectionOpsPerSecond,
                double speedup
            )
            {
                Name = name;
                HelperOpsPerSecond = helperOpsPerSecond;
                ReflectionOpsPerSecond = reflectionOpsPerSecond;
                Speedup = speedup;
            }

            public string Name { get; }

            public double HelperOpsPerSecond { get; }

            public double ReflectionOpsPerSecond { get; }

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
