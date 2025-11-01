namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class RelationalComponentBenchmarkTests
    {
        private const int NumIterations = 10_000;

        private const string DocumentPath = "Docs/RELATIONAL_COMPONENT_PERFORMANCE.md";
        private const string SectionPrefix = "RELATIONAL_COMPONENTS_";

        private static readonly TimeSpan BenchmarkDuration = TimeSpan.FromSeconds(1);

        private static readonly Func<ScenarioResult>[] ScenarioFactories =
        {
            RunParentSingleScenario,
            RunParentArrayScenario,
            RunParentListScenario,
            RunParentHashSetScenario,
            RunChildSingleScenario,
            RunChildArrayScenario,
            RunChildListScenario,
            RunChildHashSetScenario,
            RunSiblingSingleScenario,
            RunSiblingArrayScenario,
            RunSiblingListScenario,
            RunSiblingHashSetScenario,
        };

        [Test]
        [Timeout(0)]
        public void Benchmark()
        {
            List<ScenarioResult> results = new();
            foreach (Func<ScenarioResult> factory in ScenarioFactories)
            {
                ScenarioResult result = factory();
                results.Add(result);
            }

            string opsHeader =
                "| Scenario | Relational Ops/s | Manual Ops/s | Rel/Manual | Iterations |";
            string opsDivider = "| --- | ---: | ---: | ---: | ---: |";

            UnityEngine.Debug.Log("### Relational Component Assignment Benchmarks");
            UnityEngine.Debug.Log(opsHeader);
            UnityEngine.Debug.Log(opsDivider);

            List<string> sectionLines = new()
            {
                string.Format(
                    CultureInfo.InvariantCulture,
                    "_Last updated {0:yyyy-MM-dd HH:mm} UTC on {1}_",
                    DateTime.UtcNow,
                    SystemInfo.operatingSystem
                ),
                string.Empty,
                "Numbers capture repeated `Assign*Components` calls for one second per scenario.",
                "Higher operations per second are better.",
                string.Empty,
                "### Operations per second (higher is better)",
                opsHeader,
                opsDivider,
            };

            foreach (ScenarioResult result in results)
            {
                string opsRow = FormatOpsRow(result);
                UnityEngine.Debug.Log(opsRow);
                sectionLines.Add(opsRow);
            }

            sectionLines.Add(string.Empty);

            string sectionName = SectionPrefix + GetOperatingSystemToken();
            BenchmarkReadmeUpdater.UpdateSection(sectionName, sectionLines, DocumentPath);
        }

        private static ScenarioResult RunParentSingleScenario()
        {
            GameObject root = CreateGameObject("ParentSingleRoot");
            root.AddComponent<BoxCollider>();

            GameObject child = CreateGameObject("ParentSingleChild");
            child.transform.SetParent(root.transform, false);

            ParentSingleRelational relational = child.AddComponent<ParentSingleRelational>();
            ParentSingleManual manual = child.AddComponent<ParentSingleManual>();

            ScenarioResult result = ExecuteScenario(
                "Parent - Single",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyImmediate(child);
            DestroyImmediate(root);

            return result;
        }

        private static ScenarioResult RunParentArrayScenario()
        {
            GameObject grandParent = CreateGameObject("ParentArrayGrand");
            grandParent.AddComponent<BoxCollider>();

            GameObject parent = CreateGameObject("ParentArrayParent");
            parent.transform.SetParent(grandParent.transform, false);
            parent.AddComponent<BoxCollider>();

            GameObject child = CreateGameObject("ParentArrayChild");
            child.transform.SetParent(parent.transform, false);

            ParentArrayRelational relational = child.AddComponent<ParentArrayRelational>();
            ParentArrayManual manual = child.AddComponent<ParentArrayManual>();

            ScenarioResult result = ExecuteScenario(
                "Parent - Array",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyImmediate(child);
            DestroyImmediate(parent);
            DestroyImmediate(grandParent);

            return result;
        }

        private static ScenarioResult RunParentListScenario()
        {
            GameObject grandParent = CreateGameObject("ParentListGrand");
            grandParent.AddComponent<BoxCollider>();

            GameObject parent = CreateGameObject("ParentListParent");
            parent.transform.SetParent(grandParent.transform, false);
            parent.AddComponent<BoxCollider>();

            GameObject child = CreateGameObject("ParentListChild");
            child.transform.SetParent(parent.transform, false);

            ParentListRelational relational = child.AddComponent<ParentListRelational>();
            ParentListManual manual = child.AddComponent<ParentListManual>();

            ScenarioResult result = ExecuteScenario(
                "Parent - List",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyImmediate(child);
            DestroyImmediate(parent);
            DestroyImmediate(grandParent);

            return result;
        }

        private static ScenarioResult RunParentHashSetScenario()
        {
            GameObject grandParent = CreateGameObject("ParentHashGrand");
            grandParent.AddComponent<BoxCollider>();

            GameObject parent = CreateGameObject("ParentHashParent");
            parent.transform.SetParent(grandParent.transform, false);
            parent.AddComponent<BoxCollider>();

            GameObject child = CreateGameObject("ParentHashChild");
            child.transform.SetParent(parent.transform, false);

            ParentHashSetRelational relational = child.AddComponent<ParentHashSetRelational>();
            ParentHashSetManual manual = child.AddComponent<ParentHashSetManual>();

            ScenarioResult result = ExecuteScenario(
                "Parent - HashSet",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyImmediate(child);
            DestroyImmediate(parent);
            DestroyImmediate(grandParent);

            return result;
        }

        private static ScenarioResult RunChildSingleScenario()
        {
            GameObject parent = CreateGameObject("ChildSingleParent");

            ChildSingleRelational relational = parent.AddComponent<ChildSingleRelational>();
            ChildSingleManual manual = parent.AddComponent<ChildSingleManual>();

            GameObject child = CreateGameObject("ChildSingleChild");
            child.AddComponent<BoxCollider>();
            child.transform.SetParent(parent.transform, false);

            ScenarioResult result = ExecuteScenario(
                "Child - Single",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyImmediate(child);
            DestroyImmediate(parent);

            return result;
        }

        private static ScenarioResult RunChildArrayScenario()
        {
            GameObject parent = CreateGameObject("ChildArrayParent");

            ChildArrayRelational relational = parent.AddComponent<ChildArrayRelational>();
            ChildArrayManual manual = parent.AddComponent<ChildArrayManual>();

            List<GameObject> children = CreateChildColliders(parent, 6);

            ScenarioResult result = ExecuteScenario(
                "Child - Array",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyChildren(children);
            DestroyImmediate(parent);

            return result;
        }

        private static ScenarioResult RunChildListScenario()
        {
            GameObject parent = CreateGameObject("ChildListParent");

            ChildListRelational relational = parent.AddComponent<ChildListRelational>();
            ChildListManual manual = parent.AddComponent<ChildListManual>();

            List<GameObject> children = CreateChildColliders(parent, 8);

            ScenarioResult result = ExecuteScenario(
                "Child - List",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyChildren(children);
            DestroyImmediate(parent);

            return result;
        }

        private static ScenarioResult RunChildHashSetScenario()
        {
            GameObject parent = CreateGameObject("ChildHashParent");

            ChildHashSetRelational relational = parent.AddComponent<ChildHashSetRelational>();
            ChildHashSetManual manual = parent.AddComponent<ChildHashSetManual>();

            List<GameObject> children = CreateChildColliders(parent, 8);

            ScenarioResult result = ExecuteScenario(
                "Child - HashSet",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyChildren(children);
            DestroyImmediate(parent);

            return result;
        }

        private static ScenarioResult RunSiblingSingleScenario()
        {
            GameObject host = CreateGameObject("SiblingSingleHost");

            SiblingSingleRelational relational = host.AddComponent<SiblingSingleRelational>();
            SiblingSingleManual manual = host.AddComponent<SiblingSingleManual>();

            host.AddComponent<BoxCollider>();

            ScenarioResult result = ExecuteScenario(
                "Sibling - Single",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyImmediate(host);

            return result;
        }

        private static ScenarioResult RunSiblingArrayScenario()
        {
            GameObject host = CreateGameObject("SiblingArrayHost");

            SiblingArrayRelational relational = host.AddComponent<SiblingArrayRelational>();
            SiblingArrayManual manual = host.AddComponent<SiblingArrayManual>();

            AddSiblingColliders(host, 6);

            ScenarioResult result = ExecuteScenario(
                "Sibling - Array",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyImmediate(host);

            return result;
        }

        private static ScenarioResult RunSiblingListScenario()
        {
            GameObject host = CreateGameObject("SiblingListHost");

            SiblingListRelational relational = host.AddComponent<SiblingListRelational>();
            SiblingListManual manual = host.AddComponent<SiblingListManual>();

            AddSiblingColliders(host, 8);

            ScenarioResult result = ExecuteScenario(
                "Sibling - List",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyImmediate(host);

            return result;
        }

        private static ScenarioResult RunSiblingHashSetScenario()
        {
            GameObject host = CreateGameObject("SiblingHashHost");

            SiblingHashSetRelational relational = host.AddComponent<SiblingHashSetRelational>();
            SiblingHashSetManual manual = host.AddComponent<SiblingHashSetManual>();

            AddSiblingColliders(host, 8);

            ScenarioResult result = ExecuteScenario(
                "Sibling - HashSet",
                () => relational.Assign(),
                () => manual.Assign()
            );

            DestroyImmediate(host);
            return result;
        }

        private static ScenarioResult ExecuteScenario(
            string label,
            Action relationalAction,
            Action manualAction
        )
        {
            if (relationalAction == null)
            {
                throw new ArgumentNullException(nameof(relationalAction));
            }

            if (manualAction == null)
            {
                throw new ArgumentNullException(nameof(manualAction));
            }

            Prewarm(relationalAction);
            Prewarm(manualAction);

            BenchmarkMetrics relationalMetrics = Measure(relationalAction);
            BenchmarkMetrics manualMetrics = Measure(manualAction);

            return new ScenarioResult(label, relationalMetrics, manualMetrics);
        }

        private static void Prewarm(Action action)
        {
            for (int i = 0; i < 10; ++i)
            {
                action();
            }
        }

        private static BenchmarkMetrics Measure(Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Stopwatch stopwatch = Stopwatch.StartNew();

            int iterations = 0;
            do
            {
                for (int i = 0; i < NumIterations; ++i)
                {
                    action();
                    ++iterations;
                }
            } while (stopwatch.Elapsed < BenchmarkDuration);

            stopwatch.Stop();
            double opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
            return new BenchmarkMetrics(opsPerSecond, iterations, stopwatch.Elapsed);
        }

        private static string FormatOpsRow(ScenarioResult result)
        {
            string ratio =
                result.Manual.OpsPerSecond > 0d
                    ? (result.Relational.OpsPerSecond / result.Manual.OpsPerSecond).ToString(
                        "0.00",
                        CultureInfo.InvariantCulture
                    ) + "x"
                    : "n/a";

            return "| "
                + result.Label
                + " | "
                + FormatOps(result.Relational.OpsPerSecond)
                + " | "
                + FormatOps(result.Manual.OpsPerSecond)
                + " | "
                + ratio
                + " | "
                + result.Relational.Iterations.ToString("N0", CultureInfo.InvariantCulture)
                + " |";
        }

        private static string FormatOps(double value)
        {
            if (value >= 1000d)
            {
                return value.ToString("N0", CultureInfo.InvariantCulture);
            }

            if (value >= 100d)
            {
                return value.ToString("N1", CultureInfo.InvariantCulture);
            }

            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static GameObject CreateGameObject(string name)
        {
            return new GameObject(name);
        }

        private static List<GameObject> CreateChildColliders(GameObject parent, int count)
        {
            List<GameObject> children = new(count);
            for (int i = 0; i < count; ++i)
            {
                GameObject child = CreateGameObject($"Child_{i}");
                child.AddComponent<BoxCollider>();
                child.transform.SetParent(parent.transform, false);
                children.Add(child);
            }

            return children;
        }

        private static void AddSiblingColliders(GameObject host, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                host.AddComponent<BoxCollider>();
            }
        }

        private static void DestroyChildren(IEnumerable<GameObject> children)
        {
            if (children == null)
            {
                return;
            }

            foreach (GameObject child in children)
            {
                DestroyImmediate(child);
            }
        }

        private static void DestroyImmediate(UnityEngine.Object obj)
        {
            if (obj != null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        private static string GetOperatingSystemToken()
        {
            RuntimePlatform platform = Application.platform;
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsServer:
                    return "WINDOWS";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "MACOS";
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxServer:
                    return "LINUX";
                default:
                    return "OTHER";
            }
        }

        private readonly struct ScenarioResult
        {
            public ScenarioResult(
                string label,
                BenchmarkMetrics relational,
                BenchmarkMetrics manual
            )
            {
                Label = label;
                Relational = relational;
                Manual = manual;
            }

            public string Label { get; }

            public BenchmarkMetrics Relational { get; }

            public BenchmarkMetrics Manual { get; }
        }

        private readonly struct BenchmarkMetrics
        {
            public BenchmarkMetrics(double opsPerSecond, int iterations, TimeSpan elapsed)
            {
                OpsPerSecond = opsPerSecond;
                Iterations = iterations;
                Elapsed = elapsed;
            }

            public double OpsPerSecond { get; }

            public int Iterations { get; }

            public TimeSpan Elapsed { get; }
        }

        private sealed class ParentSingleRelational : MonoBehaviour
        {
            [ParentComponent]
            private BoxCollider parentCollider;

            public void Assign()
            {
                this.AssignParentComponents();
            }
        }

        private sealed class ParentSingleManual : MonoBehaviour
        {
            private BoxCollider parentCollider;

            public void Assign()
            {
                parentCollider = GetComponentInParent<BoxCollider>();
            }
        }

        private sealed class ParentArrayRelational : MonoBehaviour
        {
            [ParentComponent]
            private BoxCollider[] parentColliders;

            public void Assign()
            {
                this.AssignParentComponents();
            }
        }

        private sealed class ParentArrayManual : MonoBehaviour
        {
            private BoxCollider[] parentColliders;

            public void Assign()
            {
                parentColliders = GetComponentsInParent<BoxCollider>();
            }
        }

        private sealed class ParentListRelational : MonoBehaviour
        {
            [ParentComponent]
            private List<BoxCollider> parentColliders = new();

            public void Assign()
            {
                this.AssignParentComponents();
            }
        }

        private sealed class ParentListManual : MonoBehaviour
        {
            private readonly List<BoxCollider> parentColliders = new();

            public void Assign()
            {
                GetComponentsInParent(false, parentColliders);
            }
        }

        private sealed class ParentHashSetRelational : MonoBehaviour
        {
            [ParentComponent]
            private HashSet<BoxCollider> parentColliders = new();

            public void Assign()
            {
                this.AssignParentComponents();
            }
        }

        private sealed class ParentHashSetManual : MonoBehaviour
        {
            private readonly HashSet<BoxCollider> parentColliders = new();

            public void Assign()
            {
                BoxCollider[] buffer = GetComponentsInParent<BoxCollider>();
                parentColliders.Clear();
                for (int i = 0; i < buffer.Length; ++i)
                {
                    parentColliders.Add(buffer[i]);
                }
            }
        }

        private sealed class ChildSingleRelational : MonoBehaviour
        {
            [ChildComponent(OnlyDescendants = true)]
            private BoxCollider childCollider;

            public void Assign()
            {
                this.AssignChildComponents();
            }
        }

        private sealed class ChildSingleManual : MonoBehaviour
        {
            private BoxCollider childCollider;

            public void Assign()
            {
                BoxCollider[] buffer = GetComponentsInChildren<BoxCollider>();
                childCollider = buffer.Length > 0 ? buffer[0] : null;
            }
        }

        private sealed class ChildArrayRelational : MonoBehaviour
        {
            [ChildComponent(OnlyDescendants = true)]
            private BoxCollider[] childColliders;

            public void Assign()
            {
                this.AssignChildComponents();
            }
        }

        private sealed class ChildArrayManual : MonoBehaviour
        {
            private BoxCollider[] childColliders;

            public void Assign()
            {
                BoxCollider[] buffer = GetComponentsInChildren<BoxCollider>();
                if (buffer.Length == 0)
                {
                    childColliders = Array.Empty<BoxCollider>();
                }
                else
                {
                    childColliders = buffer;
                }
            }
        }

        private sealed class ChildListRelational : MonoBehaviour
        {
            [ChildComponent(OnlyDescendants = true)]
            private List<BoxCollider> childColliders = new();

            public void Assign()
            {
                this.AssignChildComponents();
            }
        }

        private sealed class ChildListManual : MonoBehaviour
        {
            private readonly List<BoxCollider> childColliders = new();

            public void Assign()
            {
                GetComponentsInChildren(childColliders);
            }
        }

        private sealed class ChildHashSetRelational : MonoBehaviour
        {
            [ChildComponent(OnlyDescendants = true)]
            private HashSet<BoxCollider> childColliders = new();

            public void Assign()
            {
                this.AssignChildComponents();
            }
        }

        private sealed class ChildHashSetManual : MonoBehaviour
        {
            private readonly HashSet<BoxCollider> childColliders = new();

            public void Assign()
            {
                BoxCollider[] buffer = GetComponentsInChildren<BoxCollider>();
                childColliders.Clear();
                for (int i = 0; i < buffer.Length; ++i)
                {
                    childColliders.Add(buffer[i]);
                }
            }
        }

        private sealed class SiblingSingleRelational : MonoBehaviour
        {
            [SiblingComponent]
            private BoxCollider siblingCollider;

            public void Assign()
            {
                this.AssignSiblingComponents();
            }
        }

        private sealed class SiblingSingleManual : MonoBehaviour
        {
            private BoxCollider siblingCollider;

            public void Assign()
            {
                siblingCollider = GetComponent<BoxCollider>();
            }
        }

        private sealed class SiblingArrayRelational : MonoBehaviour
        {
            [SiblingComponent]
            private BoxCollider[] siblingColliders;

            public void Assign()
            {
                this.AssignSiblingComponents();
            }
        }

        private sealed class SiblingArrayManual : MonoBehaviour
        {
            private BoxCollider[] siblingColliders;

            public void Assign()
            {
                siblingColliders = GetComponents<BoxCollider>();
            }
        }

        private sealed class SiblingListRelational : MonoBehaviour
        {
            [SiblingComponent]
            private List<BoxCollider> siblingColliders = new();

            public void Assign()
            {
                this.AssignSiblingComponents();
            }
        }

        private sealed class SiblingListManual : MonoBehaviour
        {
            private readonly List<BoxCollider> siblingColliders = new();

            public void Assign()
            {
                GetComponents(siblingColliders);
            }
        }

        private sealed class SiblingHashSetRelational : MonoBehaviour
        {
            [SiblingComponent]
            private HashSet<BoxCollider> siblingColliders = new();

            public void Assign()
            {
                this.AssignSiblingComponents();
            }
        }

        private sealed class SiblingHashSetManual : MonoBehaviour
        {
            private readonly HashSet<BoxCollider> siblingColliders = new();

            public void Assign()
            {
                BoxCollider[] buffer = GetComponents<BoxCollider>();
                siblingColliders.Clear();
                for (int i = 0; i < buffer.Length; ++i)
                {
                    siblingColliders.Add(buffer[i]);
                }
            }
        }
    }
}
