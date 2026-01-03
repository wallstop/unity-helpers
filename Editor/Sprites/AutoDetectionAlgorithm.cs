// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;

    /// <summary>
    /// Specifies the algorithm used for automatic sprite grid detection.
    /// </summary>
    public enum AutoDetectionAlgorithm
    {
        /// <summary>
        /// Obsolete value. Use <see cref="AutoBest"/> for intelligent algorithm selection.
        /// </summary>
        [Obsolete("Use AutoBest for intelligent algorithm selection.")]
        None = 0,

        /// <summary>
        /// Automatically selects the best algorithm based on confidence scoring.
        /// Uses lazy evaluation: runs algorithms in order of speed until one exceeds 70% confidence.
        /// </summary>
        AutoBest = 1,

        /// <summary>
        /// Simple uniform grid division based on expected sprite count.
        /// Divides texture evenly into rows and columns. Requires expectedSpriteCount to be set.
        /// </summary>
        UniformGrid = 2,

        /// <summary>
        /// Analyzes transparent pixel boundaries to detect grid lines.
        /// Scores candidate cell sizes by transparency percentage along grid boundaries.
        /// </summary>
        BoundaryScoring = 3,

        /// <summary>
        /// Detects individual sprites via flood-fill, computes centroids,
        /// and infers grid dimensions from spacing patterns.
        /// </summary>
        ClusterCentroid = 4,

        /// <summary>
        /// Computes a distance transform from alpha boundaries and finds local maxima
        /// to identify sprite centers. Infers grid from peak spacing.
        /// </summary>
        DistanceTransform = 5,

        /// <summary>
        /// Seeds from local intensity maxima and grows regions via 4-connected flood-fill
        /// to alpha boundaries. Infers grid from region size uniformity.
        /// </summary>
        RegionGrowing = 6,
    }
#endif
}
