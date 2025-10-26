namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class IListSortingPerformanceTests
    {
        private const string DocumentPath = "Docs/ILIST_SORTING_PERFORMANCE.md";
        private const string SectionPrefix = "ILIST_SORT_";
        private const int NearlySortedSwapPercentage = 50;

        private static readonly DatasetSizeSpec[] DatasetSizeSpecs =
        {
            new DatasetSizeSpec("100", 100),
            new DatasetSizeSpec("1,000", 1_000),
            new DatasetSizeSpec("10,000", 10_000),
            new DatasetSizeSpec("100,000", 100_000),
            new DatasetSizeSpec("1,000,000", 1_000_000),
        };

        private static readonly DatasetState[] DatasetStates =
        {
            new DatasetState("Sorted", BuildSortedData),
            new DatasetState("Nearly Sorted (2% swaps)", BuildNearlySortedData),
            new DatasetState("Shuffled (deterministic)", BuildShuffledData),
        };

        private static readonly SortImplementation[] SortImplementations =
        {
            new SortImplementation("Ghost", SortAlgorithm.Ghost, false, int.MaxValue),
            new SortImplementation("Meteor", SortAlgorithm.Meteor, false, int.MaxValue),
            new SortImplementation(
                "Pattern-Defeating QuickSort",
                SortAlgorithm.PatternDefeatingQuickSort,
                false,
                int.MaxValue
            ),
            new SortImplementation("Grail", SortAlgorithm.Grail, true, int.MaxValue),
            new SortImplementation("Power", SortAlgorithm.Power, true, int.MaxValue),
            new SortImplementation("Insertion", SortAlgorithm.Insertion, true, 10_000),
        };

        [Test]
        [Timeout(0)]
        public void Benchmark()
        {
            string operatingSystemToken = GetOperatingSystemToken();
            string sectionName = SectionPrefix + operatingSystemToken;

            List<string> readmeLines = new List<string>
            {
                string.Format(
                    CultureInfo.InvariantCulture,
                    "_Last updated {0:yyyy-MM-dd HH:mm} UTC on {1}_",
                    DateTime.UtcNow,
                    SystemInfo.operatingSystem
                ),
                string.Empty,
                "Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.",
                string.Empty,
            };

            IComparer<int> comparer = Comparer<int>.Default;
            string headerLine = BuildHeaderLine();
            string dividerLine = BuildDividerLine();

            foreach (DatasetState datasetState in DatasetStates)
            {
                UnityEngine.Debug.Log($"IList Sorting Benchmarks - {datasetState.Label}");
                UnityEngine.Debug.Log(headerLine);
                UnityEngine.Debug.Log(dividerLine);

                readmeLines.Add($"### {datasetState.Label}");
                readmeLines.Add(headerLine);
                readmeLines.Add(dividerLine);

                foreach (DatasetSizeSpec sizeSpec in DatasetSizeSpecs)
                {
                    int[] baseData = datasetState.CreateData(sizeSpec.Count);
                    string rowLine = BuildRowLine(sizeSpec.Label, baseData, comparer);
                    UnityEngine.Debug.Log(rowLine);
                    readmeLines.Add(rowLine);
                }

                readmeLines.Add(string.Empty);
                UnityEngine.Debug.Log(string.Empty);
            }

            BenchmarkReadmeUpdater.UpdateSection(sectionName, readmeLines, DocumentPath);
        }

        private static string BuildHeaderLine()
        {
            StringBuilder headerBuilder = new StringBuilder();
            headerBuilder.Append("| List Size |");
            foreach (SortImplementation implementation in SortImplementations)
            {
                headerBuilder.Append(' ');
                headerBuilder.Append(implementation.Label);
                headerBuilder.Append(" |");
            }

            return headerBuilder.ToString();
        }

        private static string BuildDividerLine()
        {
            StringBuilder dividerBuilder = new StringBuilder();
            dividerBuilder.Append("| --- |");
            foreach (SortImplementation implementation in SortImplementations)
            {
                dividerBuilder.Append(" --- |");
            }

            return dividerBuilder.ToString();
        }

        private static string BuildRowLine(
            string sizeLabel,
            int[] baseData,
            IComparer<int> comparer
        )
        {
            StringBuilder rowBuilder = new StringBuilder();
            rowBuilder.Append("| ");
            rowBuilder.Append(sizeLabel);
            rowBuilder.Append(" |");

            foreach (SortImplementation implementation in SortImplementations)
            {
                string result = BenchmarkImplementation(implementation, baseData, comparer);
                rowBuilder.Append(' ');
                rowBuilder.Append(result);
                rowBuilder.Append(" |");
            }

            return rowBuilder.ToString();
        }

        private static string BenchmarkImplementation(
            SortImplementation implementation,
            int[] baseData,
            IComparer<int> comparer
        )
        {
            if (baseData.Length > implementation.MaxSupportedCount)
            {
                return "n/a";
            }

            int[] workingData = new int[baseData.Length];
            Array.Copy(baseData, workingData, baseData.Length);

            IList<int> workingList = workingData;
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                implementation.Execute(workingList, comparer);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError(
                    $"Sorting benchmark failed for {implementation.Label}: {exception.Message}"
                );
                return "error";
            }
            finally
            {
                stopwatch.Stop();
            }

            double milliseconds = stopwatch.Elapsed.TotalMilliseconds;
            return FormatDuration(milliseconds);
        }

        private static string FormatDuration(double milliseconds)
        {
            if (milliseconds >= 1000d)
            {
                double seconds = milliseconds / 1000d;
                return string.Format(CultureInfo.InvariantCulture, "{0:0.00} s", seconds);
            }

            if (milliseconds >= 100d)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:0} ms", milliseconds);
            }

            if (milliseconds >= 10d)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:0.0} ms", milliseconds);
            }

            if (milliseconds >= 1d)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:0.00} ms", milliseconds);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:0.000} ms", milliseconds);
        }

        private static int[] BuildSortedData(int count)
        {
            int[] data = new int[count];
            for (int i = 0; i < count; ++i)
            {
                data[i] = i;
            }

            return data;
        }

        private static int[] BuildNearlySortedData(int count)
        {
            int[] data = BuildSortedData(count);
            if (count <= 1)
            {
                return data;
            }

            int swaps = Math.Max(1, count / NearlySortedSwapPercentage);
            IRandom random = new PcgRandom(unchecked(count * 37));

            for (int i = 0; i < swaps; ++i)
            {
                int index = random.Next(0, count - 1);
                int neighbor = Math.Min(count - 1, index + 1);
                int temp = data[index];
                data[index] = data[neighbor];
                data[neighbor] = temp;
            }

            return data;
        }

        private static int[] BuildShuffledData(int count)
        {
            int[] data = BuildSortedData(count);
            if (count <= 1)
            {
                return data;
            }

            IRandom random = new PcgRandom(unchecked(count * 7919));

            for (int i = count - 1; i > 0; --i)
            {
                int swapIndex = random.Next(0, i + 1);
                int temp = data[i];
                data[i] = data[swapIndex];
                data[swapIndex] = temp;
            }

            return data;
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

        private readonly struct DatasetSizeSpec
        {
            public DatasetSizeSpec(string label, int count)
            {
                Label = label;
                Count = count;
            }

            public string Label { get; }

            public int Count { get; }
        }

        private readonly struct DatasetState
        {
            private readonly Func<int, int[]> generator;

            public DatasetState(string label, Func<int, int[]> generator)
            {
                Label = label;
                this.generator = generator;
            }

            public string Label { get; }

            public int[] CreateData(int count)
            {
                return generator(count);
            }
        }

        private readonly struct SortImplementation
        {
            public SortImplementation(
                string label,
                SortAlgorithm algorithm,
                bool isStable,
                int maxSupportedCount
            )
            {
                Label = label;
                Algorithm = algorithm;
                IsStable = isStable;
                MaxSupportedCount = maxSupportedCount;
            }

            public string Label { get; }

            public SortAlgorithm Algorithm { get; }

            public bool IsStable { get; }

            public int MaxSupportedCount { get; }

            public void Execute(IList<int> list, IComparer<int> comparer)
            {
                list.Sort(comparer, Algorithm);
            }
        }
    }
}
