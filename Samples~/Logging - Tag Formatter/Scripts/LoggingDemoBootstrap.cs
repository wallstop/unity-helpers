// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace Samples.UnityHelpers.Logging
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper.Logging;

    /// <summary>
    /// Registers sample-specific tag decorations for the logging demo.
    /// </summary>
    internal static class LoggingDemoBootstrap
    {
        private static bool RegisteredDecorations;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterDemoDecorations()
        {
            if (RegisteredDecorations)
            {
                return;
            }

            RegisteredDecorations = true;
            UnityLogTagFormatter formatter = WallstopStudiosLogger.LogInstance;

            formatter.AddDecoration(
                match: "npc",
                format: value =>
                {
                    string label = value?.ToString() ?? "Unknown";
                    return $"<color=#7AD7FF>[{label}]</color>";
                },
                tag: "DemoNpc",
                priority: -20
            );

            formatter.AddDecoration(
                predicate: tag => tag.StartsWith("status=", StringComparison.OrdinalIgnoreCase),
                format: (tag, value) =>
                {
                    string status = tag.Substring("status=".Length);
                    string upper = status.ToUpperInvariant();
                    return $"<color=#F4B942>{upper}</color> {value}";
                },
                tag: "DemoStatus",
                priority: -5
            );
        }
    }
}
