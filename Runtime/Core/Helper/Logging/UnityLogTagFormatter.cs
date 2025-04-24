namespace UnityHelpers.Core.Helper.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Extension;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;

    public sealed class UnityLogTagFormatter : IFormatProvider, ICustomFormatter
    {
        public const char Separator = ',';

        private static readonly string NewLine = Environment.NewLine;
        private static readonly StringBuilder CachedStringBuilder = new();
        private static readonly List<string> CachedDecorators = new();

        private static readonly Dictionary<string, string> ColorNamesToHex = ReflectionHelpers
            .LoadStaticPropertiesForType<Color>()
            .Where(kvp => kvp.Value.PropertyType == typeof(Color))
            .Select(kvp => (kvp.Key, ((Color)kvp.Value.GetValue(null)).ToHex()))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<
            string,
            (bool editorOnly, Func<object, string> formatter)
        > KeyedDecorations => _keyedDecorations;

        public IReadOnlyCollection<
            IReadOnlyList<(
                string tag,
                bool editorOnly,
                Func<string, bool> predicate,
                Func<string, object, string> formatter
            )>
        > MatchingDecorations => _matchingDecorations.Values;

        private readonly Dictionary<
            string,
            (bool editorOnly, Func<object, string> formatter)
        > _keyedDecorations = new(StringComparer.OrdinalIgnoreCase);

        private readonly SortedDictionary<
            int,
            List<(
                string tag,
                bool editorOnly,
                Func<string, bool> predicate,
                Func<string, object, string> formatter
            )>
        > _matchingDecorations = new();

        public UnityLogTagFormatter()
            : this(true) { }

        public UnityLogTagFormatter(bool createDefaultDecorators)
        {
            if (!createDefaultDecorators)
            {
                return;
            }

            const string boldify = "<b>{0}</b>";
            AddDecoration("b", boldify, editorOnly: true, force: true);
            AddDecoration("bold", boldify, editorOnly: true, force: true);
            AddDecoration("!", boldify, editorOnly: true, force: true);

            const string italicify = "<i>{0}</i>";
            AddDecoration("i", italicify, editorOnly: true, force: true);
            AddDecoration("italic", italicify, editorOnly: true, force: true);
            AddDecoration("_", italicify, editorOnly: true, force: true);

            AddDecoration("json", value => value?.ToJson() ?? "{}", editorOnly: false, force: true);

            AddDecoration(
                format => format.StartsWith('#'),
                (format, value) =>
                {
                    string hexCode = ColorNamesToHex.GetValueOrDefault(format.Substring(1), format);
                    return $"<color={hexCode}>{value}</color>";
                },
                "Color",
                editorOnly: true,
                force: true
            );
            AddDecoration(
                format => int.TryParse(format, out _),
                (format, value) =>
                {
                    int size = int.Parse(format);
                    return $"<size={size}>{value}</size>";
                },
                "ImplicitSize",
                editorOnly: true,
                force: true
            );

            const string sizeCheck = "size=";
            AddDecoration(
                format =>
                    format.StartsWith(sizeCheck)
                    && int.TryParse(format.Substring(sizeCheck.Length), out _),
                (format, value) =>
                {
                    int size = int.Parse(format.Substring(sizeCheck.Length));
                    return $"<size={size}>{value}</size>";
                },
                "ExplicitSize",
                editorOnly: true,
                force: true
            );

            const string colorCheck = "color=";
            AddDecoration(
                format => format.StartsWith(colorCheck),
                (format, value) =>
                {
                    string color = format.Substring(colorCheck.Length);
                    color = ColorNamesToHex.GetValueOrDefault(color, color);
                    return $"<color={color}>{value}</color>";
                },
                "ExplicitColor",
                editorOnly: true,
                force: true
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

            // Do not format stuff that we don't have control over!
            if (arg is not string && arg is IFormattable formattable)
            {
                return formattable.ToString(format, this);
            }

            CachedDecorators.Clear();
            if (!format.Contains(Separator))
            {
                CachedDecorators.Add(format);
            }
            else
            {
                CachedStringBuilder.Clear();
                foreach (char element in format)
                {
                    if (element == Separator)
                    {
                        if (0 < CachedStringBuilder.Length)
                        {
                            CachedDecorators.Add(CachedStringBuilder.ToString());
                            CachedStringBuilder.Clear();
                        }
                    }
                    else
                    {
                        CachedStringBuilder.Append(element);
                    }
                }
                if (0 < CachedStringBuilder.Length)
                {
                    CachedDecorators.Add(CachedStringBuilder.ToString());
                    CachedStringBuilder.Clear();
                }
            }

            foreach (string key in CachedDecorators)
            {
                if (
                    _keyedDecorations.TryGetValue(
                        key,
                        out (bool editorOnly, Func<object, string> formatter) decorator
                    ) && (Application.isEditor || !decorator.editorOnly)
                )
                {
                    return decorator.formatter(arg);
                }

                foreach (
                    List<(
                        string tag,
                        bool editorOnly,
                        Func<string, bool> predicate,
                        Func<string, object, string> formatter
                    )> matchingDecoration in _matchingDecorations.Values
                )
                {
                    foreach (
                        (
                            _,
                            bool editorOnly,
                            Func<string, bool> predicate,
                            Func<string, object, string> matchingFormatter
                        ) in matchingDecoration
                    )
                    {
                        if ((Application.isEditor || !editorOnly) && predicate(key))
                        {
                            return matchingFormatter(key, arg);
                        }
                    }
                }
            }

            return arg?.ToString() ?? string.Empty;
        }

        [HideInCallstack]
        public void Log(
            object message,
            Object context = null,
            Exception e = null,
            bool prettify = true
        )
        {
            string rendered = Render(message, context, e, prettify);
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
        public void LogWarn(
            object message,
            Object context = null,
            Exception e = null,
            bool prettify = true
        )
        {
            string rendered = Render(message, context, e, prettify);
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
        public void LogError(
            object message,
            Object context = null,
            Exception e = null,
            bool prettify = true
        )
        {
            string rendered = Render(message, context, e, prettify);
            if (context != null)
            {
                Debug.LogError(rendered, context);
            }
            else
            {
                Debug.LogError(rendered);
            }
        }

        public bool AddDecoration(
            string tag,
            string format,
            bool editorOnly = false,
            bool force = false
        )
        {
            return AddDecoration(tag, content => string.Format(format, content), editorOnly, force);
        }

        public bool AddDecoration(
            string tag,
            Func<object, string> format,
            bool editorOnly = false,
            bool force = false
        )
        {
            if (!force)
            {
                return _keyedDecorations.TryAdd(tag, (editorOnly, format));
            }

            _keyedDecorations[tag] = (editorOnly, format);
            return true;
        }

        public bool AddDecoration(
            Func<string, bool> predicate,
            Func<string, object, string> format,
            string tag,
            int priority = 0,
            bool editorOnly = false,
            bool force = false
        )
        {
            if (
                !_matchingDecorations.TryGetValue(
                    priority,
                    out List<(
                        string tag,
                        bool editorOnly,
                        Func<string, bool> predicate,
                        Func<string, object, string> formatter
                    )> matchingDecorations
                )
            )
            {
                _matchingDecorations[priority] = new List<(
                    string tag,
                    bool editorOnly,
                    Func<string, bool> predicate,
                    Func<string, object, string> formatter
                )>
                {
                    (tag, editorOnly, predicate, format),
                };
                return true;
            }

            int? matchingIndex = null;
            for (int i = 0; i < matchingDecorations.Count; ++i)
            {
                (string matchingTag, bool _, Func<string, bool> _, Func<string, object, string> _) =
                    matchingDecorations[i];
                if (string.Equals(matchingTag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    matchingIndex = i;
                    break;
                }
            }

            if (matchingIndex == null)
            {
                matchingDecorations.Add((tag, editorOnly, predicate, format));
                return true;
            }

            if (!force)
            {
                return false;
            }
            matchingDecorations[matchingIndex.Value] = (tag, editorOnly, predicate, format);
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
                        bool,
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
                (string matchingTag, bool _, Func<string, bool> _, Func<string, object, string> _) =
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
        private string Render(object message, Object unityObject, Exception e, bool prettify)
        {
            if (!prettify)
            {
                return message switch
                {
                    FormattableString formattable => e != null
                        ? $"{formattable.ToString(this)}{NewLine}    {e}"
                        : formattable.ToString(this),
                    string str => e != null ? $"{str}{NewLine}    {e}" : str,
                    _ => e != null ? $"{message}{NewLine}    {e}" : message.ToString(),
                };
            }

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

            return message switch
            {
                FormattableString formattable => e != null
                    ? $"{now}|{gameObjectName}[{componentType}]|{formattable.ToString(this)}{NewLine}    {e}"
                    : $"{now}|{gameObjectName}[{componentType}]|{formattable.ToString(this)}",
                string str => e != null
                    ? $"{now}|{gameObjectName}[{componentType}]|{str}{NewLine}    {e}"
                    : $"{now}|{gameObjectName}[{componentType}]|{str}",
                _ => e != null
                    ? $"{now}|{gameObjectName}[{componentType}]|{message}{NewLine}    {e}"
                    : $"{now}|{gameObjectName}[{componentType}]|{message}",
            };
        }
    }
}
