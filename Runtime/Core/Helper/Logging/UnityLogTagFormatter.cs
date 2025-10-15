namespace WallstopStudios.UnityHelpers.Core.Helper.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Extension;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Default supported formats:
    ///     b -> Bold text
    ///     bold -> Bold text
    ///     ! -> Bold text
    ///     i -> Italic text
    ///     italic -> Italic text
    ///     _ -> Italic text
    ///     json -> format as JSON
    ///     #color-hex -> Colored text
    ///     #color-name -> Colored text
    ///     color=value -> Colored text
    ///     1-100 -> Sized text
    ///     size=1-100 -> Sized text
    /// </summary>
    public sealed class UnityLogTagFormatter : IFormatProvider, ICustomFormatter
    {
        public const char Separator = ',';

        private static readonly string NewLine = Environment.NewLine;

        private static readonly Dictionary<string, string> ColorNamesToHex = ReflectionHelpers
            .LoadStaticPropertiesForType<Color>()
            .Where(kvp => kvp.Value.PropertyType == typeof(Color))
            .Select(kvp => (kvp.Key, ((Color)kvp.Value.GetValue(null)).ToHex()))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// All currently registered decorations by tag.
        /// </summary>
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
        private readonly Dictionary<string, (int priority, int index)> _decorationLookup = new(
            StringComparer.OrdinalIgnoreCase
        );
        private readonly StringBuilder _cachedStringBuilder = new();
        private readonly List<string> _cachedDecorators = new();
        private readonly HashSet<string> _appliedTags = new(StringComparer.OrdinalIgnoreCase);

        public UnityLogTagFormatter()
            : this(true) { }

        /// <summary>
        /// Creates a new UnityLogTagFormatter.
        /// </summary>
        /// <param name="createDefaultDecorators">If true, applies default decorators (bold, italic, color, size, and json).</param>
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
                    format.StartsWith(sizeCheck, StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(format.Substring(sizeCheck.Length), out _)
                    || int.TryParse(format, out _),
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
        private static string ToSafeString(object arg)
        {
            if (arg is Object unityObj)
            {
                return unityObj != null ? unityObj.ToString() : string.Empty;
            }
            return arg?.ToString() ?? string.Empty;
        }

        [HideInCallstack]
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return ToSafeString(arg);
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

            return ToSafeString(arg);
        }

        [HideInCallstack]
        public string Log(
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

            return rendered;
        }

        [HideInCallstack]
        public string LogWarn(
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

            return rendered;
        }

        [HideInCallstack]
        public string LogError(
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

            return rendered;
        }

        /// <summary>
        /// Attempts to add a decoration.
        /// </summary>
        /// <param name="match">An exact match for tag ("a" would correspond to ${value:a})</param>
        /// <param name="format">A formatter to apply to the matched object (typically something like <c>value =&gt; $"&lt;newFormat&gt;{value}&lt;/newFormat&gt;"</c>).</param>
        /// <param name="tag">A descriptive, unique identifier for the decoration (for example, "Bold", or "Color")</param>
        /// <param name="priority">The priority to register the decoration at. Lower values will be evaluated first.</param>
        /// <param name="editorOnly">If true, will only be applied when the game is running in the Unity Editor.</param>
        /// <param name="force">
        ///     If true, will override any existing decorations for the same tag, regardless of priority.
        ///     If false, decorations with the same tag (compared OrdinalIgnoreCase) will cause the registration to fail.
        /// </param>
        /// <returns>True if the decoration was added, false if the decoration was not added.</returns>
        public bool AddDecoration(
            string match,
            Func<object, string> format,
            string tag = null,
            int priority = 0,
            bool editorOnly = false,
            bool force = false
        )
        {
            return AddDecoration(
                check => string.Equals(check, match, StringComparison.OrdinalIgnoreCase),
                format: (_, value) => format(value),
                tag: tag ?? match,
                priority: priority,
                editorOnly: editorOnly,
                force: force
            );
        }

        /// <summary>
        /// Attempts to add a decoration.
        /// </summary>
        /// <param name="predicate">
        ///     Tag matcher. Can be as complex as you want. For example, the default color matcher
        ///     is implemented something like tag => tag.StartsWith('#') || tag.StartsWith("color=")
        /// </param>
        /// <param name="format">
        ///     Custom formatting function. Takes in both the matched tag as well the current object to format. In
        ///     the same case of color matching, the implementation needs to be smart enough to handle the case where
        ///     the tag is "#red" or "color=red" or "color=#FF0000".
        /// </param>
        /// <param name="tag">A descriptive, unique identifier for the decoration (for example, "Bold", or "Color")</param>
        /// <param name="priority">The priority to register the decoration at. Lower values will be evaluated first.</param>
        /// <param name="editorOnly">If true, will only be applied when the game is running in the Unity Editor.</param>
        /// <param name="force">
        ///     If true, will override any existing decorations for the same tag, regardless of priority.
        ///     If false, decorations with the same tag (compared OrdinalIgnoreCase) will cause the registration to fail.
        /// </param>
        /// <returns>True if the decoration was added, false if the decoration was not added.</returns>
        public bool AddDecoration(
            Func<string, bool> predicate,
            Func<string, object, string> format,
            string tag,
            int priority = 0,
            bool editorOnly = false,
            bool force = false
        )
        {
            if (_decorationLookup.TryGetValue(tag, out (int priority, int index) existing))
            {
                if (!force)
                {
                    return false;
                }

                if (existing.priority == priority)
                {
                    List<(
                        string tag,
                        bool editorOnly,
                        Func<string, bool> predicate,
                        Func<string, object, string> formatter
                    )> decorationsAtPriority = _matchingDecorations[priority];

                    decorationsAtPriority[existing.index] = (tag, editorOnly, predicate, format);
                    _decorationLookup[tag] = (priority, existing.index);
                    return true;
                }

                RemoveDecorationInternal(existing.priority, existing.index);
            }

            List<(
                string tag,
                bool editorOnly,
                Func<string, bool> predicate,
                Func<string, object, string> formatter
            )> matchingDecorations;
            if (!_matchingDecorations.TryGetValue(priority, out matchingDecorations))
            {
                matchingDecorations = new List<(
                    string tag,
                    bool editorOnly,
                    Func<string, bool> predicate,
                    Func<string, object, string> formatter
                )>(1);
                _matchingDecorations[priority] = matchingDecorations;
            }

            int indexToInsert = matchingDecorations.Count;
            matchingDecorations.Add((tag, editorOnly, predicate, format));
            _decorationLookup[tag] = (priority, indexToInsert);
            return true;
        }

        /// <summary>
        /// Attempts to remove a decoration by its tag.
        /// </summary>
        /// <param name="tag">Tag for the decoration ("Bold", "Color", etc.)</param>
        /// <param name="decoration">The removed decoration, if one was found.</param>
        /// <returns>True if a decoration was found for that tag and removed, false otherwise.</returns>
        public bool RemoveDecoration(
            string tag,
            out (
                string tag,
                bool editorOnly,
                Func<string, bool> predicate,
                Func<string, object, string> formatter
            ) decoration
        )
        {
            if (!_decorationLookup.TryGetValue(tag, out (int priority, int index) existing))
            {
                decoration = default;
                return false;
            }

            decoration = RemoveDecorationInternal(existing.priority, existing.index);
            return true;
        }

        private (
            string tag,
            bool editorOnly,
            Func<string, bool> predicate,
            Func<string, object, string> formatter
        ) RemoveDecorationInternal(int priority, int index)
        {
            List<(
                string tag,
                bool editorOnly,
                Func<string, bool> predicate,
                Func<string, object, string> formatter
            )> decorationsAtPriority = _matchingDecorations[priority];

            (
                string tag,
                bool editorOnly,
                Func<string, bool> predicate,
                Func<string, object, string> formatter
            ) removed = decorationsAtPriority[index];

            decorationsAtPriority.RemoveAt(index);
            _decorationLookup.Remove(removed.tag);

            for (int i = index; i < decorationsAtPriority.Count; ++i)
            {
                (
                    string tag,
                    bool editorOnly,
                    Func<string, bool> predicate,
                    Func<string, object, string> formatter
                ) entry = decorationsAtPriority[i];
                _decorationLookup[entry.tag] = (priority, i);
            }

            if (decorationsAtPriority.Count == 0)
            {
                _matchingDecorations.Remove(priority);
            }

            return removed;
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
