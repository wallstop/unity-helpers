#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.Zenject
{
    using System;

    /// <summary>
    /// Controls how the Zenject integration scans the scene for relational components.
    /// </summary>
    /// <example>
    /// <code>
    /// // In a custom installer, if you want to bind your own options instance:
    /// Container.BindInstance(new RelationalSceneAssignmentOptions(includeInactive: false));
    /// </code>
    /// </example>
    public readonly struct RelationalSceneAssignmentOptions
        : IEquatable<RelationalSceneAssignmentOptions>
    {
        /// <summary>
        /// Initializes a new set of options.
        /// </summary>
        /// <param name="includeInactive">
        /// When true the initializer will scan inactive scene objects so that relational fields are
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
