namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;

    internal static class TexturePlatformNameHelper
    {
        private static string[] _cached;

        public static string[] GetKnownPlatformNames()
        {
            if (_cached != null)
            {
                return _cached;
            }

            List<string> names = new(16) { "DefaultTexturePlatform" };

            // Map BuildTargetGroup to importer platform names
            Dictionary<BuildTargetGroup, string> map = new()
            {
                { BuildTargetGroup.Standalone, "Standalone" },
#if UNITY_IOS || UNITY_TVOS || true
                { BuildTargetGroup.iOS, "iPhone" },
                { BuildTargetGroup.tvOS, "tvOS" },
#endif
                { BuildTargetGroup.Android, "Android" },
                { BuildTargetGroup.WebGL, "WebGL" },
#if UNITY_PS4 || true
                { BuildTargetGroup.PS4, "PS4" },
#endif
#if UNITY_PS5 || true
                { BuildTargetGroup.PS5, "PS5" },
#endif
#if UNITY_XBOXONE || true
                { BuildTargetGroup.XboxOne, "XboxOne" },
#endif
#if UNITY_SWITCH || true
                { BuildTargetGroup.Switch, "Switch" },
#endif
            };

            Array values = Enum.GetValues(typeof(BuildTargetGroup));
            for (int i = 0; i < values.Length; i++)
            {
                BuildTargetGroup g = (BuildTargetGroup)values.GetValue(i);
                if (g == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                // Old/obsolete groups may exist; include them as ToString fallback
                if (map.TryGetValue(g, out string name))
                {
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }
                else
                {
                    string n = g.ToString();
                    if (!string.IsNullOrEmpty(n) && !names.Contains(n))
                    {
                        names.Add(n);
                    }
                }
            }

            names.Sort(StringComparer.Ordinal);
            _cached = names.ToArray();
            return _cached;
        }
    }
#endif
}
