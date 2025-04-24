namespace UnityHelpers.Core.Helper.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extension;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;

    public sealed class UnityLogTagFormatter : IFormatProvider, ICustomFormatter
    {
        public static readonly UnityLogTagFormatter Instance = new();

        private static readonly Dictionary<string, string> ColorNamesToHex = ReflectionHelpers
            .LoadStaticPropertiesForType<Color>()
            .Where(kvp => kvp.Value.PropertyType == typeof(Color))
            .Select(kvp => ($"#{kvp.Key}", ((Color)kvp.Value.GetValue(null)).ToHex()))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, Func<object, string>> KeyedDecorations =>
            _keyedDecorations;

        public IReadOnlyCollection<
            IReadOnlyList<(
                string tag,
                Func<string, bool> predicate,
                Func<string, object, string> formatter
            )>
        > MatchingDecorations => _matchingDecorations.Values;

        private readonly Dictionary<string, Func<object, string>> _keyedDecorations = new(
            StringComparer.OrdinalIgnoreCase
        );

        private readonly SortedDictionary<
            int,
            List<(string tag, Func<string, bool> predicate, Func<string, object, string> formatter)>
        > _matchingDecorations = new();

        private UnityLogTagFormatter()
        {
            const string boldify = "<b>{0}</b>";
            AddDecoration("b", boldify, true);
            AddDecoration("bold", boldify, true);
            AddDecoration("!", boldify, true);

            const string italicify = "<i>{0}</i>";
            AddDecoration("i", italicify, true);
            AddDecoration("italic", italicify, true);
            AddDecoration("_", italicify, true);

            const string underlineify = "<u>{0}</u>";
            AddDecoration("u", underlineify, true);
            AddDecoration("underline", underlineify, true);

            AddDecoration("json", value => value?.ToJson() ?? "{}", true);

            AddDecoration(
                format => format.StartsWith('#'),
                (format, value) =>
                {
                    string hexCode = ColorNamesToHex.GetValueOrDefault(format, format);
                    return $"<color={hexCode}>{value}</color>";
                },
                "Color"
            );
            AddDecoration(
                format => int.TryParse(format, out _),
                (format, value) =>
                {
                    int size = int.Parse(format);
                    return $"<size={size}>{value}</size>";
                },
                "SpecificSize"
            );
        }

        [HideInCallstack]
        public object GetFormat(Type formatType)
        {
            return formatType.IsAssignableFrom(typeof(ICustomFormatter)) ? this : null;
        }

        [HideInCallstack]
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return arg?.ToString() ?? string.Empty;
            }

            if (arg is not string && arg is IFormattable formattable)
            {
                return formattable.ToString(format, this);
            }

            if (Application.isEditor)
            {
                if (_keyedDecorations.TryGetValue(format, out Func<object, string> formatter))
                {
                    return formatter(arg);
                }

                foreach (
                    List<(
                        string tag,
                        Func<string, bool> predicate,
                        Func<string, object, string> formatter
                    )> matchingDecoration in _matchingDecorations.Values
                )
                {
                    foreach (
                        (
                            _,
                            Func<string, bool> predicate,
                            Func<string, object, string> matchingFormatter
                        ) in matchingDecoration
                    )
                    {
                        if (predicate(format))
                        {
                            return matchingFormatter(format, arg);
                        }
                    }
                }
            }

            return arg?.ToString() ?? string.Empty;
        }

        [HideInCallstack]
        //[System.Diagnostics]
        public void Log(FormattableString message, Object context = null, Exception e = null)
        {
            string rendered = Render(message, context, e);
            if (context != null)
            {
                Debug.Log(rendered, context);
            }
            else
            {
                Debug.Log(rendered);
            }
        }

        [HideInCallstack]
        public void Log(string message, Object context = null)
        {
            if (context != null)
            {
                Debug.Log(message, context);
            }
            else
            {
                Debug.Log(message);
            }
        }

        [HideInCallstack]
        public void LogWarn(FormattableString message, Object context = null, Exception e = null)
        {
            string rendered = Render(message, context, e);
            if (context != null)
            {
                Debug.LogWarning(rendered, context);
            }
            else
            {
                Debug.LogWarning(rendered);
            }
        }

        [HideInCallstack]
        public void LogWarn(string message, Object context = null)
        {
            if (context != null)
            {
                Debug.LogWarning(message, context);
            }
            else
            {
                Debug.LogWarning(message);
            }
        }

        [HideInCallstack]
        public void LogError(FormattableString message, Object context = null, Exception e = null)
        {
            string rendered = Render(message, context, e);
            if (context != null)
            {
                Debug.LogError(rendered, context);
            }
            else
            {
                Debug.LogError(rendered);
            }
        }

        [HideInCallstack]
        public void LogError(string message, Object context = null)
        {
            if (context != null)
            {
                Debug.LogError(message, context);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public bool AddDecoration(string tag, string format, bool force = false)
        {
            return AddDecoration(tag, content => string.Format(format, content), force);
        }

        public bool AddDecoration(string tag, Func<object, string> format, bool force = false)
        {
            if (!force)
            {
                return _keyedDecorations.TryAdd(tag, format);
            }

            _keyedDecorations[tag] = format;
            return true;
        }

        public bool AddDecoration(
            Func<string, bool> predicate,
            Func<string, object, string> format,
            string tag,
            int priority = 0,
            bool force = false
        )
        {
            if (
                !_matchingDecorations.TryGetValue(
                    priority,
                    out List<(
                        string tag,
                        Func<string, bool> predicate,
                        Func<string, object, string> formatter
                    )> matchingDecorations
                )
            )
            {
                _matchingDecorations[priority] = new List<(
                    string tag,
                    Func<string, bool> predicate,
                    Func<string, object, string> formatter
                )>
                {
                    (tag, predicate, format),
                };
                return true;
            }

            int? matchingIndex = null;
            for (int i = 0; i < matchingDecorations.Count; ++i)
            {
                (string matchingTag, Func<string, bool> _, Func<string, object, string> _) =
                    matchingDecorations[i];
                if (string.Equals(matchingTag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    matchingIndex = i;
                    break;
                }
            }

            if (matchingIndex == null)
            {
                matchingDecorations.Add((tag, predicate, format));
                return true;
            }

            if (!force)
            {
                return false;
            }
            matchingDecorations[matchingIndex.Value] = (tag, predicate, format);
            return true;
        }

        public bool RemoveDecoration(string tag)
        {
            return _keyedDecorations.Remove(tag);
        }

        public bool RemoveDecoration(int priority, string tag)
        {
            if (
                !_matchingDecorations.TryGetValue(
                    priority,
                    out List<(
                        string,
                        Func<string, bool>,
                        Func<string, object, string>
                    )> matchingDecorations
                )
            )
            {
                return false;
            }

            int? matchingIndex = null;
            for (int i = 0; i < matchingDecorations.Count; ++i)
            {
                (string matchingTag, Func<string, bool> _, Func<string, object, string> _) =
                    matchingDecorations[i];
                if (string.Equals(matchingTag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    matchingIndex = i;
                    break;
                }
            }

            if (matchingIndex == null)
            {
                return false;
            }

            matchingDecorations.RemoveAt(matchingIndex.Value);
            if (matchingDecorations.Count == 0)
            {
                _matchingDecorations.Remove(priority);
            }
            return true;
        }

        [HideInCallstack]
        private string Render(FormattableString message, Object unityObject, Exception e)
        {
            float now = Time.time;
            string componentType;
            string gameObjectName;
            if (unityObject != null)
            {
                componentType = unityObject.GetType().Name;
                gameObjectName = unityObject.name;
            }
            else
            {
                componentType = "NO_TYPE";
                gameObjectName = "NO_NAME";
            }

            return e != null
                ? $"{now}|{gameObjectName}[{componentType}]|{message.ToString(this)}\n    {e}"
                : $"{now}|{gameObjectName}[{componentType}]|{message.ToString(this)}";
        }
    }
}
