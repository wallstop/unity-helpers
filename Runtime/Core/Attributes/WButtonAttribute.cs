namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    /// <summary>
    /// Exposes a parameterless method as a clickable inspector button for rapid authoring tools, debug hooks, or content workflows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Apply <see cref="WButtonAttribute"/> to any method on a <see cref="UnityEngine.Object"/> derived type to surface it in the inspector.
    /// You can override the label, group related buttons, and control the ordering so that the editor layout remains predictable.
    /// </para>
    /// <para>
    /// The attribute never runs in player builds—methods execute only inside the Unity Editor when the button is pressed—making it safe for destructive
    /// utilities such as spawning test content, clearing caches, or invoking validation passes.
    /// </para>
    /// </remarks>
    /// <example>
    /// Simple editor utility buttons:
    /// <code>
    /// public sealed class EnemySpawner : MonoBehaviour
    /// {
    ///     [WButton(\"Rebuild Spawn Points\")]
    ///     private void Rebuild()
    ///     {
    ///         // expensive setup logic omitted
    ///     }
    ///
    ///     [WButton(drawOrder: 1, groupName: \"Debug\")]
    ///     private void PrintSpawnCount()
    ///     {
    ///         Debug.Log(spawnPoints.Count);
    ///     }
    /// }
    /// </code>
    /// Grouped buttons with history overrides:
    /// <code>
    /// public sealed class QuestAuthoringTool : ScriptableObject
    /// {
    ///     [WButton(\"Generate IDs\", drawOrder: -1, historyCapacity: 5, groupName: \"Authoring\")]
    ///     private void GenerateIds() { }
    ///
    ///     [WButton(\"Submit\", drawOrder: -1, historyCapacity: 1, priority: UnityHelpersSettings.HighlightColorKey)]
    ///     private void SubmitToServer() { }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class WButtonAttribute : Attribute
    {
        public const int UseGlobalHistory = -1;

        public WButtonAttribute(
            string displayName = null,
            int drawOrder = 0,
            int historyCapacity = UseGlobalHistory,
            string priority = null,
            string groupName = null
        )
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
            DrawOrder = drawOrder;
            HistoryCapacity = historyCapacity < 0 ? UseGlobalHistory : historyCapacity;
            ColorKey = string.IsNullOrWhiteSpace(priority) ? null : priority.Trim();
            GroupName = string.IsNullOrWhiteSpace(groupName) ? null : groupName.Trim();
        }

        /// <summary>
        /// Explicit label override for the button. Falls back to the method name when null.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Controls grouping and sorting. Values of -1 or higher render above the default inspector,
        /// values below -1 (i.e., -2, -3, etc.) render below. Lower values render first within their placement section.
        /// Buttons with the same draw order but different group names render as separate groups.
        /// </summary>
        public int DrawOrder { get; }

        /// <summary>
        /// Optional override for the number of results retained. Negative values defer to the global setting.
        /// </summary>
        public int HistoryCapacity { get; }

        /// <summary>
        /// Optional custom color key used to resolve palette-based styling.
        /// </summary>
        public string ColorKey { get; }

        /// <summary>
        /// Optional group name for organizing buttons. Buttons with the same draw order but different
        /// group names will render in separate groups. Groups render in the order they are first encountered.
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// Legacy alias for <see cref="ColorKey"/> to maintain backwards compatibility.
        /// </summary>
        [Obsolete("Use ColorKey instead.")]
        public string Priority => ColorKey;
    }
}
