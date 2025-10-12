#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.VContainer
{
    using System;

    /// <summary>
    /// Controls how the VContainer integration applies relational component assignment.
    /// </summary>
    /// <example>
    /// <code>
    /// // Register integration and scan only active objects
    /// builder.RegisterRelationalComponents(
    ///     new RelationalSceneAssignmentOptions(includeInactive: false)
    /// );
    /// </code>
    /// </example>
    public readonly struct RelationalSceneAssignmentOptions
        : IEquatable<RelationalSceneAssignmentOptions>
    {
        /// <summary>
        /// Initializes a new set of options.
        /// </summary>
        /// <param name="includeInactive">
        /// When true the entry point will scan inactive scene objects so that relational fields are
        /// populated even for disabled hierarchies. Defaults to <c>true</c>.
        /// </param>
        public RelationalSceneAssignmentOptions(bool includeInactive)
        {
            IncludeInactive = includeInactive;
        }

        /// <summary>
        /// Options used when no explicit configuration is supplied.
        /// </summary>
        public static RelationalSceneAssignmentOptions Default => new(includeInactive: true);

        /// <summary>
        /// Gets whether inactive GameObjects should be included when scanning the scene.
        /// </summary>
        public bool IncludeInactive { get; }

        /// <inheritdoc />
        public bool Equals(RelationalSceneAssignmentOptions other)
        {
            return IncludeInactive == other.IncludeInactive;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is RelationalSceneAssignmentOptions other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return IncludeInactive.GetHashCode();
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(
            RelationalSceneAssignmentOptions left,
            RelationalSceneAssignmentOptions right
        )
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(
            RelationalSceneAssignmentOptions left,
            RelationalSceneAssignmentOptions right
        )
        {
            return !left.Equals(right);
        }
    }
}
#endif
