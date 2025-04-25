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

        private static readonly Dictionary<string, string> ColorNamesToHex = ReflectionHelpers
            .LoadStaticPropertiesForType<Color>()
            .Where(kvp => kvp.Value.PropertyType == typeof(Color))
            .Select(kvp => (kvp.Key, ((Color)kvp.Value.GetValue(null)).ToHex()))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<string> Decorations =>
            _matchingDecorations.Values.SelectMany(x => x).Select(value => value.tag);

        public IReadOnlyCollection<
            IReadOnlyList<(
                string tag,
                bool editorOnly,
                Func<string, bool> predicate,
                Func<string, object, string> formatter
            )>
        > MatchingDecorations => _matchingDecorations.Values;

        private readonly SortedDictionary<
            int,
            List<(
                string tag,
                bool editorOnly,
                Func<string, bool> predicate,
                Func<string, object, string> formatter
            )>
        > _matchingDecorations = new();
        private readonly StringBuilder _cachedStringBuilder = new();
        private readonly List<string> _cachedDecorators = new();
        private readonly HashSet<string> _appliedTags = new(StringComparer.OrdinalIgnoreCase);

        public UnityLogTagFormatter()
            : this(true) { }

        public UnityLogTagFormatter(bool createDefaultDecorators)
        {
            if (!createDefaultDecorators)
            {
                return;
            }

            AddDecoration(
                format =>
                    string.Equals(format, "b", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(format, "bold", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(format, "!", StringComparison.OrdinalIgnoreCase),
                format: (_, value) => $"<b>{value}</b>",
                tag: "Bold",
                editorOnly: true,
                force: true
            );

            AddDecoration(
                format =>
                    string.Equals(format, "i", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(format, "italic", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(format, "_", StringComparison.OrdinalIgnoreCase),
                format: (_, value) => $"<i>{value}</i>",
                tag: "Italic",
                editorOnly: true,
                force: true
            );

            AddDecoration(
                match: "json",
                format: value => value?.ToJson() ?? "{}",
                tag: "JSON",
                editorOnly: false,
                force: true
            );

            const char colorCharCheck = '#';
            const string colorStringCheck = "color=";
            AddDecoration(
                format =>
                    format.StartsWith(colorCharCheck)
                    || format.StartsWith(colorStringCheck, StringComparison.OrdinalIgnoreCase),
                format: (format, value) =>
                {
                    string baseColor = format.StartsWith(
                        colorStringCheck,
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? format.Substring(colorStringCheck.Length)
                        : format;

                    string hexCode = ColorNamesToHex.GetValueOrDefault(
                        format.StartsWith(colorCharCheck) ? format.Substring(1) : baseColor,
                        baseColor
                    );
                    return $"<color={hexCode}>{value}</color>";
                },
                tag: "Color",
                editorOnly: true,
                force: true
            );

            const string sizeCheck = "size=";
            AddDecoration(
                format =>
                    (
                        format.StartsWith(sizeCheck, StringComparison.OrdinalIgnoreCase)
                            && int.TryParse(format.Substring(sizeCheck.Length), out _)
                        || int.TryParse(format, out _)
                    ),
                format: (format, value) =>
                {
                    if (!int.TryParse(format, out int size))
                    {
                        size = int.Parse(format.Substring(sizeCheck.Length));
                    }
                    return $"<size={size}>{value}</size>";
                },
                tag: "Size",
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

            _cachedDecorators.Clear();
            if (!format.Contains(Separator))
            {
                _cachedDecorators.Add(format);
            }
            else
            {
                _cachedStringBuilder.Clear();
                foreach (char element in format)
                {
                    if (element == Separator)
                    {
                        if (0 < _cachedStringBuilder.Length)
                        {
                            _cachedDecorators.Add(_cachedStringBuilder.ToString());
                            _cachedStringBuilder.Clear();
                        }
                    }
                    else
                    {
                        _cachedStringBuilder.Append(element);
                    }
                }
                if (0 < _cachedStringBuilder.Length)
                {
                    _cachedDecorators.Add(_cachedStringBuilder.ToString());
                    _cachedStringBuilder.Clear();
                }
            }

            _appliedTags.Clear();
            object formatted = arg;
            foreach (string key in _cachedDecorators)
            {
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
                            string tag,
                            bool editorOnly,
                            Func<string, bool> predicate,
                            Func<string, object, string> matchingFormatter
                        ) in matchingDecoration
                    )
                    {
                        if (
                            (Application.isEditor || !editorOnly)
                            && predicate(key)
                            && _appliedTags.Add(tag)
                        )
                        {
                            formatted = matchingFormatter(key, formatted);
                        }
                    }
                }
            }

            if (0 < _appliedTags.Count)
            {
                return formatted.ToString();
            }

            if (arg is not string && arg is IFormattable formattable)
            {
                return formattable.ToString(format, this);
            }

            return arg?.ToString() ?? string.Empty;
        }

        [HideInCallstack]
        public void Log(
            FormattableString message,
            Object context = null,
            Exception e = null,
            bool pretty = true
        )
        {
            string rendered = Render(message, context, e, pretty);
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
            FormattableString message,
            Object context = null,
            Exception e = null,
            bool pretty = true
        )
        {
            string rendered = Render(message, context, e, pretty);
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
            FormattableString message,
            Object context = null,
            Exception e = null,
            bool pretty = true
        )
        {
            string rendered = Render(message, context, e, pretty);
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
            string match,
            Func<object, string> format,
            string tag = null,
            bool editorOnly = false,
            bool force = false
        )
        {
            return AddDecoration(
                check => string.Equals(check, match, StringComparison.OrdinalIgnoreCase),
                format: (_, value) => format(value),
                tag: tag ?? match,
                priority: 0,
                editorOnly: editorOnly,
                force: force
            );
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
            foreach (var entry in _matchingDecorations)
            {
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    var existingDecoration = entry.Value[i];
                    if (
                        !string.Equals(
                            existingDecoration.tag,
                            tag,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }

                    if (force)
                    {
                        if (priority != entry.Key)
                        {
                            entry.Value.RemoveAt(i);
                            break;
                        }
                        entry.Value[i] = (tag, editorOnly, predicate, format);
                        return true;
                    }
                    return false;
                }
            }

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
            foreach (var entry in _matchingDecorations)
            {
                for (int i = 0; i < entry.Value.Count; ++i)
                {
                    var existingDecoration = entry.Value[i];
                    if (
                        string.Equals(
                            tag,
                            existingDecoration.tag,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        entry.Value.RemoveAt(i);
                        if (entry.Value.Count == 0)
                        {
                            _matchingDecorations.Remove(entry.Key);
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        [HideInCallstack]
        private string Render(
            FormattableString message,
            Object unityObject,
            Exception e,
            bool pretty
        )
        {
            if (!pretty)
            {
                return e != null
                    ? $"{message.ToString(this)}{NewLine}    {e}"
                    : message.ToString(this);
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

            return e != null
                ? $"{now}|{gameObjectName}[{componentType}]|{message.ToString(this)}{NewLine}    {e}"
                : $"{now}|{gameObjectName}[{componentType}]|{message.ToString(this)}";
        }
    }
}
