// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Provides multiple algorithms for automatic sprite grid detection.
    /// Each algorithm analyzes texture pixel data to determine optimal cell dimensions.
    /// </summary>
    public static class SpriteSheetAlgorithms
    {
        /// <summary>
        /// Minimum confidence threshold for AutoBest algorithm to accept a result early.
        /// </summary>
        public const float AutoBestEarlyStopConfidence = 0.70f;

        /// <summary>
        /// Minimum cell size in pixels for valid grid detection.
        /// </summary>
        public const int MinimumCellSize = 4;

        /// <summary>
        /// Common sprite cell sizes for grid detection candidate generation.
        /// </summary>
        private static readonly int[] CommonCellSizes =
        {
            8,
            16,
            24,
            32,
            48,
            64,
            96,
            128,
            256,
            512,
        };

        /// <summary>
        /// Result of a grid detection algorithm.
        /// </summary>
        public readonly struct AlgorithmResult
        {
            /// <summary>
            /// The detected cell width in pixels.
            /// </summary>
            public readonly int CellWidth;

            /// <summary>
            /// The detected cell height in pixels.
            /// </summary>
            public readonly int CellHeight;

            /// <summary>
            /// Confidence score in the range [0, 1] where 1 is highest confidence.
            /// </summary>
            public readonly float Confidence;

            /// <summary>
            /// The algorithm that produced this result.
            /// </summary>
            public readonly AutoDetectionAlgorithm Algorithm;

            /// <summary>
            /// Whether this result represents a valid grid detection.
            /// </summary>
            public bool IsValid => CellWidth >= MinimumCellSize && CellHeight >= MinimumCellSize;

            public AlgorithmResult(
                int cellWidth,
                int cellHeight,
                float confidence,
                AutoDetectionAlgorithm algorithm
            )
            {
                CellWidth = cellWidth;
                CellHeight = cellHeight;
                Confidence = Mathf.Clamp01(confidence);
                Algorithm = algorithm;
            }

            public static AlgorithmResult Invalid(AutoDetectionAlgorithm algorithm)
            {
                return new AlgorithmResult(0, 0, 0f, algorithm);
            }
        }

        /// <summary>
        /// Detects optimal grid dimensions using the specified algorithm.
        /// </summary>
        /// <param name="pixels">The texture pixel data in Color32 format.</param>
        /// <param name="textureWidth">Width of the texture in pixels.</param>
        /// <param name="textureHeight">Height of the texture in pixels.</param>
        /// <param name="alphaThreshold">Alpha value (0-1) below which a pixel is considered transparent.</param>
        /// <param name="algorithm">The detection algorithm to use.</param>
        /// <param name="expectedSpriteCount">Optional expected sprite count (required for UniformGrid).</param>
        /// <param name="cancellationToken">Optional cancellation token for long-running operations.</param>
        /// <returns>The algorithm result containing detected cell dimensions and confidence.</returns>
        public static AlgorithmResult DetectGrid(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            float alphaThreshold,
            AutoDetectionAlgorithm algorithm,
            int expectedSpriteCount = -1,
            CancellationToken cancellationToken = default
        )
        {
            if (pixels == null || pixels.Length == 0)
            {
                return AlgorithmResult.Invalid(algorithm);
            }

            if (textureWidth < MinimumCellSize || textureHeight < MinimumCellSize)
            {
                return AlgorithmResult.Invalid(algorithm);
            }

            if (pixels.Length != textureWidth * textureHeight)
            {
                return AlgorithmResult.Invalid(algorithm);
            }

            if (alphaThreshold < 0f || alphaThreshold >= 1f)
            {
                return AlgorithmResult.Invalid(algorithm);
            }

            switch (algorithm)
            {
                case AutoDetectionAlgorithm.AutoBest:
                    return DetectGridAutoBest(
                        pixels,
                        textureWidth,
                        textureHeight,
                        alphaThreshold,
                        expectedSpriteCount,
                        cancellationToken
                    );
                case AutoDetectionAlgorithm.UniformGrid:
                    return DetectGridUniformGrid(textureWidth, textureHeight, expectedSpriteCount);
                case AutoDetectionAlgorithm.BoundaryScoring:
                    return DetectGridBoundaryScoring(
                        pixels,
                        textureWidth,
                        textureHeight,
                        alphaThreshold
                    );
                case AutoDetectionAlgorithm.ClusterCentroid:
                    return DetectGridClusterCentroid(
                        pixels,
                        textureWidth,
                        textureHeight,
                        alphaThreshold,
                        cancellationToken
                    );
                case AutoDetectionAlgorithm.DistanceTransform:
                    return DetectGridDistanceTransform(
                        pixels,
                        textureWidth,
                        textureHeight,
                        alphaThreshold,
                        cancellationToken
                    );
                case AutoDetectionAlgorithm.RegionGrowing:
                    return DetectGridRegionGrowing(
                        pixels,
                        textureWidth,
                        textureHeight,
                        alphaThreshold,
                        cancellationToken
                    );
                default:
                    return AlgorithmResult.Invalid(algorithm);
            }
        }

        /// <summary>
        /// AutoBest algorithm: runs algorithms in order of speed until one exceeds 70% confidence.
        /// Order: BoundaryScoring (fastest) -> ClusterCentroid -> DistanceTransform -> RegionGrowing
        /// </summary>
        private static AlgorithmResult DetectGridAutoBest(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            float alphaThreshold,
            int expectedSpriteCount,
            CancellationToken cancellationToken
        )
        {
            AlgorithmResult bestResult = AlgorithmResult.Invalid(AutoDetectionAlgorithm.AutoBest);

            // Try BoundaryScoring first (fastest)
            if (cancellationToken.IsCancellationRequested)
            {
                return bestResult;
            }

            AlgorithmResult boundaryResult = DetectGridBoundaryScoring(
                pixels,
                textureWidth,
                textureHeight,
                alphaThreshold
            );

            if (boundaryResult.IsValid && boundaryResult.Confidence >= AutoBestEarlyStopConfidence)
            {
                return new AlgorithmResult(
                    boundaryResult.CellWidth,
                    boundaryResult.CellHeight,
                    boundaryResult.Confidence,
                    AutoDetectionAlgorithm.AutoBest
                );
            }

            if (boundaryResult.Confidence > bestResult.Confidence)
            {
                bestResult = boundaryResult;
            }

            // Try ClusterCentroid
            if (cancellationToken.IsCancellationRequested)
            {
                return ConvertToAutoBest(bestResult);
            }

            AlgorithmResult clusterResult = DetectGridClusterCentroid(
                pixels,
                textureWidth,
                textureHeight,
                alphaThreshold,
                cancellationToken
            );

            if (clusterResult.IsValid && clusterResult.Confidence >= AutoBestEarlyStopConfidence)
            {
                return new AlgorithmResult(
                    clusterResult.CellWidth,
                    clusterResult.CellHeight,
                    clusterResult.Confidence,
                    AutoDetectionAlgorithm.AutoBest
                );
            }

            if (clusterResult.Confidence > bestResult.Confidence)
            {
                bestResult = clusterResult;
            }

            // Try DistanceTransform
            if (cancellationToken.IsCancellationRequested)
            {
                return ConvertToAutoBest(bestResult);
            }

            AlgorithmResult distanceResult = DetectGridDistanceTransform(
                pixels,
                textureWidth,
                textureHeight,
                alphaThreshold,
                cancellationToken
            );

            if (distanceResult.IsValid && distanceResult.Confidence >= AutoBestEarlyStopConfidence)
            {
                return new AlgorithmResult(
                    distanceResult.CellWidth,
                    distanceResult.CellHeight,
                    distanceResult.Confidence,
                    AutoDetectionAlgorithm.AutoBest
                );
            }

            if (distanceResult.Confidence > bestResult.Confidence)
            {
                bestResult = distanceResult;
            }

            // Try RegionGrowing
            if (cancellationToken.IsCancellationRequested)
            {
                return ConvertToAutoBest(bestResult);
            }

            AlgorithmResult regionResult = DetectGridRegionGrowing(
                pixels,
                textureWidth,
                textureHeight,
                alphaThreshold,
                cancellationToken
            );

            if (regionResult.Confidence > bestResult.Confidence)
            {
                bestResult = regionResult;
            }

            // Try UniformGrid as last resort if expected count is provided
            if (expectedSpriteCount > 0)
            {
                AlgorithmResult uniformResult = DetectGridUniformGrid(
                    textureWidth,
                    textureHeight,
                    expectedSpriteCount
                );

                if (uniformResult.Confidence > bestResult.Confidence)
                {
                    bestResult = uniformResult;
                }
            }

            return ConvertToAutoBest(bestResult);
        }

        private static AlgorithmResult ConvertToAutoBest(AlgorithmResult result)
        {
            if (!result.IsValid)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.AutoBest);
            }

            return new AlgorithmResult(
                result.CellWidth,
                result.CellHeight,
                result.Confidence,
                AutoDetectionAlgorithm.AutoBest
            );
        }

        /// <summary>
        /// UniformGrid algorithm: simple division based on expected sprite count.
        /// Tries to find optimal rows/columns that evenly divide the texture.
        /// </summary>
        private static AlgorithmResult DetectGridUniformGrid(
            int textureWidth,
            int textureHeight,
            int expectedSpriteCount
        )
        {
            if (expectedSpriteCount <= 0)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.UniformGrid);
            }

            // Find factor pairs of expectedSpriteCount
            int bestColumns = 1;
            int bestRows = expectedSpriteCount;
            float bestAspectRatio = float.MaxValue;
            float textureAspect = (float)textureWidth / textureHeight;

            for (int cols = 1; cols <= expectedSpriteCount; ++cols)
            {
                if (expectedSpriteCount % cols != 0)
                {
                    continue;
                }

                int rows = expectedSpriteCount / cols;
                int cellWidth = textureWidth / cols;
                int cellHeight = textureHeight / rows;

                // Skip if cells don't divide evenly
                if (textureWidth % cols != 0 || textureHeight % rows != 0)
                {
                    continue;
                }

                // Skip if cells are too small
                if (cellWidth < MinimumCellSize || cellHeight < MinimumCellSize)
                {
                    continue;
                }

                float cellAspect = (float)cellWidth / cellHeight;
                float aspectDiff = Mathf.Abs(cellAspect - 1f);

                // Prefer square cells, but also consider texture aspect ratio
                float gridAspect = (float)cols / rows;
                float textureAspectDiff = Mathf.Abs(gridAspect - textureAspect);
                float combinedScore = aspectDiff + textureAspectDiff * 0.5f;

                if (combinedScore < bestAspectRatio)
                {
                    bestAspectRatio = combinedScore;
                    bestColumns = cols;
                    bestRows = rows;
                }
            }

            int finalCellWidth = textureWidth / bestColumns;
            int finalCellHeight = textureHeight / bestRows;

            // Confidence is high only if cells divide evenly
            bool perfectDivision = textureWidth % bestColumns == 0 && textureHeight % bestRows == 0;
            float confidence = perfectDivision ? 1.0f : 0.5f;

            // Reduce confidence for extreme aspect ratios
            float cellAspectRatio = (float)finalCellWidth / finalCellHeight;
            if (cellAspectRatio < 0.25f || cellAspectRatio > 4.0f)
            {
                confidence *= 0.5f;
            }

            return new AlgorithmResult(
                finalCellWidth,
                finalCellHeight,
                confidence,
                AutoDetectionAlgorithm.UniformGrid
            );
        }

        /// <summary>
        /// BoundaryScoring algorithm: scores grid lines by transparent pixel percentage.
        /// This is the existing algorithm from SpriteSheetExtractor, refactored for reuse.
        /// </summary>
        private static AlgorithmResult DetectGridBoundaryScoring(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            float alphaThreshold
        )
        {
            byte alphaThresholdByte = (byte)(alphaThreshold * 255f);

            using PooledArray<int> columnTransparencyLease = SystemArrayPool<int>.Get(
                textureWidth,
                out int[] columnTransparencyCount
            );
            using PooledArray<int> rowTransparencyLease = SystemArrayPool<int>.Get(
                textureHeight,
                out int[] rowTransparencyCount
            );

            Array.Clear(columnTransparencyCount, 0, textureWidth);
            Array.Clear(rowTransparencyCount, 0, textureHeight);

            for (int y = 0; y < textureHeight; ++y)
            {
                int rowOffset = y * textureWidth;
                for (int x = 0; x < textureWidth; ++x)
                {
                    if (pixels[rowOffset + x].a <= alphaThresholdByte)
                    {
                        ++columnTransparencyCount[x];
                        ++rowTransparencyCount[y];
                    }
                }
            }

            using PooledResource<List<int>> widthCandidatesLease = Buffers<int>.List.Get(
                out List<int> widthCandidates
            );
            using PooledResource<List<int>> heightCandidatesLease = Buffers<int>.List.Get(
                out List<int> heightCandidates
            );

            GenerateCandidateCellSizes(textureWidth, widthCandidates);
            GenerateCandidateCellSizes(textureHeight, heightCandidates);

            int bestWidth = 0;
            int bestHeight = 0;
            float bestScore = -1f;

            for (int wi = 0; wi < widthCandidates.Count; ++wi)
            {
                int candidateWidth = widthCandidates[wi];
                float widthScore = ScoreCellSizeForDimension(
                    columnTransparencyCount,
                    textureWidth,
                    textureHeight,
                    candidateWidth
                );

                if (widthScore < 0.15f)
                {
                    continue;
                }

                for (int hi = 0; hi < heightCandidates.Count; ++hi)
                {
                    int candidateHeight = heightCandidates[hi];
                    float heightScore = ScoreCellSizeForDimension(
                        rowTransparencyCount,
                        textureHeight,
                        textureWidth,
                        candidateHeight
                    );

                    if (heightScore < 0.15f)
                    {
                        continue;
                    }

                    float combinedScore = (widthScore + heightScore) * 0.5f;

                    int columns = textureWidth / candidateWidth;
                    int rows = textureHeight / candidateHeight;

                    // Bonus for producing multiple cells
                    if (columns >= 2 && rows >= 2)
                    {
                        combinedScore += 0.15f;
                    }
                    else if (columns >= 2 || rows >= 2)
                    {
                        combinedScore += 0.08f;
                    }

                    // Bonus for power of two sizes
                    if (IsPowerOfTwo(candidateWidth) && IsPowerOfTwo(candidateHeight))
                    {
                        combinedScore += 0.05f;
                    }

                    // Bonus for square cells
                    if (candidateWidth == candidateHeight)
                    {
                        combinedScore += 0.02f;
                    }

                    // Bonus for reasonable cell counts
                    int cellCount = columns * rows;
                    if (cellCount >= 4 && cellCount <= 64)
                    {
                        combinedScore += 0.03f;
                    }

                    if (combinedScore > bestScore)
                    {
                        bestScore = combinedScore;
                        bestWidth = candidateWidth;
                        bestHeight = candidateHeight;
                    }
                }
            }

            if (bestScore >= 0.15f && bestWidth > 0 && bestHeight > 0)
            {
                // Normalize score to [0, 1] range
                float confidence = Mathf.Clamp01(bestScore);
                return new AlgorithmResult(
                    bestWidth,
                    bestHeight,
                    confidence,
                    AutoDetectionAlgorithm.BoundaryScoring
                );
            }

            return AlgorithmResult.Invalid(AutoDetectionAlgorithm.BoundaryScoring);
        }

        /// <summary>
        /// ClusterCentroid algorithm: detects sprites via flood-fill, computes centroids,
        /// and infers grid from spacing patterns.
        /// </summary>
        private static AlgorithmResult DetectGridClusterCentroid(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            float alphaThreshold,
            CancellationToken cancellationToken
        )
        {
            byte alphaThresholdByte = (byte)(alphaThreshold * 255f);

            using PooledResource<List<Rect>> spriteBoundsLease = Buffers<Rect>.List.Get(
                out List<Rect> spriteBounds
            );

            DetectSpriteBoundsByAlpha(
                pixels,
                textureWidth,
                textureHeight,
                alphaThresholdByte,
                spriteBounds,
                cancellationToken
            );

            if (spriteBounds.Count < 2)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.ClusterCentroid);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.ClusterCentroid);
            }

            // Compute centroids
            using PooledResource<List<Vector2>> centroidsLease = Buffers<Vector2>.List.Get(
                out List<Vector2> centroids
            );

            for (int i = 0; i < spriteBounds.Count; ++i)
            {
                Rect bounds = spriteBounds[i];
                centroids.Add(bounds.center);
            }

            // Analyze horizontal spacing
            using PooledResource<List<float>> xPositionsLease = Buffers<float>.List.Get(
                out List<float> xPositions
            );
            using PooledResource<List<float>> yPositionsLease = Buffers<float>.List.Get(
                out List<float> yPositions
            );

            for (int i = 0; i < centroids.Count; ++i)
            {
                xPositions.Add(centroids[i].x);
                yPositions.Add(centroids[i].y);
            }

            xPositions.Sort();
            yPositions.Sort();

            // Find consistent spacing
            float xSpacing = FindConsistentSpacing(xPositions);
            float ySpacing = FindConsistentSpacing(yPositions);

            if (xSpacing < MinimumCellSize && ySpacing < MinimumCellSize)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.ClusterCentroid);
            }

            // Use average sprite size as a fallback
            float avgWidth = 0f;
            float avgHeight = 0f;
            for (int i = 0; i < spriteBounds.Count; ++i)
            {
                avgWidth += spriteBounds[i].width;
                avgHeight += spriteBounds[i].height;
            }
            avgWidth /= spriteBounds.Count;
            avgHeight /= spriteBounds.Count;

            int cellWidth =
                xSpacing >= MinimumCellSize
                    ? Mathf.RoundToInt(xSpacing)
                    : Mathf.RoundToInt(avgWidth);
            int cellHeight =
                ySpacing >= MinimumCellSize
                    ? Mathf.RoundToInt(ySpacing)
                    : Mathf.RoundToInt(avgHeight);

            // Snap to common sizes if close
            cellWidth = SnapToCommonSize(cellWidth);
            cellHeight = SnapToCommonSize(cellHeight);

            // Ensure cells divide texture
            cellWidth = FindNearestDivisor(textureWidth, cellWidth);
            cellHeight = FindNearestDivisor(textureHeight, cellHeight);

            if (cellWidth < MinimumCellSize || cellHeight < MinimumCellSize)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.ClusterCentroid);
            }

            // Calculate confidence based on spacing consistency
            float xConsistency = CalculateSpacingConsistency(xPositions, cellWidth);
            float yConsistency = CalculateSpacingConsistency(yPositions, cellHeight);
            float confidence = (xConsistency + yConsistency) * 0.5f;

            return new AlgorithmResult(
                cellWidth,
                cellHeight,
                confidence,
                AutoDetectionAlgorithm.ClusterCentroid
            );
        }

        /// <summary>
        /// DistanceTransform algorithm: computes chamfer distance from alpha boundaries
        /// and finds local maxima to identify sprite centers.
        /// </summary>
        private static AlgorithmResult DetectGridDistanceTransform(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            float alphaThreshold,
            CancellationToken cancellationToken
        )
        {
            byte alphaThresholdByte = (byte)(alphaThreshold * 255f);

            using PooledArray<int> distanceLease = SystemArrayPool<int>.Get(
                pixels.Length,
                out int[] distance
            );

            // Initialize: 0 for transparent, large value for opaque
            const int maxDistance = int.MaxValue / 2;
            for (int i = 0; i < pixels.Length; ++i)
            {
                distance[i] = pixels[i].a <= alphaThresholdByte ? 0 : maxDistance;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.DistanceTransform);
            }

            // Forward pass (3-4 chamfer distance)
            for (int y = 1; y < textureHeight; ++y)
            {
                for (int x = 0; x < textureWidth; ++x)
                {
                    int idx = y * textureWidth + x;
                    if (distance[idx] == 0)
                    {
                        continue;
                    }

                    int top = distance[idx - textureWidth];
                    int current = distance[idx];

                    if (x > 0)
                    {
                        int topLeft = distance[idx - textureWidth - 1];
                        int left = distance[idx - 1];
                        current = Math.Min(current, Math.Min(topLeft + 4, left + 3));
                    }

                    current = Math.Min(current, top + 3);

                    if (x < textureWidth - 1)
                    {
                        int topRight = distance[idx - textureWidth + 1];
                        current = Math.Min(current, topRight + 4);
                    }

                    distance[idx] = current;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.DistanceTransform);
            }

            // Backward pass
            for (int y = textureHeight - 2; y >= 0; --y)
            {
                for (int x = textureWidth - 1; x >= 0; --x)
                {
                    int idx = y * textureWidth + x;
                    if (distance[idx] == 0)
                    {
                        continue;
                    }

                    int bottom = distance[idx + textureWidth];
                    int current = distance[idx];

                    if (x < textureWidth - 1)
                    {
                        int bottomRight = distance[idx + textureWidth + 1];
                        int right = distance[idx + 1];
                        current = Math.Min(current, Math.Min(bottomRight + 4, right + 3));
                    }

                    current = Math.Min(current, bottom + 3);

                    if (x > 0)
                    {
                        int bottomLeft = distance[idx + textureWidth - 1];
                        current = Math.Min(current, bottomLeft + 4);
                    }

                    distance[idx] = current;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.DistanceTransform);
            }

            // Find local maxima
            using PooledResource<List<Vector2Int>> maximaLease = Buffers<Vector2Int>.List.Get(
                out List<Vector2Int> localMaxima
            );

            int minPeakDistance = MinimumCellSize / 2;
            for (int y = minPeakDistance; y < textureHeight - minPeakDistance; ++y)
            {
                for (int x = minPeakDistance; x < textureWidth - minPeakDistance; ++x)
                {
                    int idx = y * textureWidth + x;
                    int val = distance[idx];

                    if (val < minPeakDistance)
                    {
                        continue;
                    }

                    bool isMaximum = true;
                    for (int dy = -1; dy <= 1 && isMaximum; ++dy)
                    {
                        for (int dx = -1; dx <= 1 && isMaximum; ++dx)
                        {
                            if (dx == 0 && dy == 0)
                            {
                                continue;
                            }
                            int neighborIdx = (y + dy) * textureWidth + (x + dx);
                            if (distance[neighborIdx] > val)
                            {
                                isMaximum = false;
                            }
                        }
                    }

                    if (isMaximum)
                    {
                        localMaxima.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (localMaxima.Count < 2)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.DistanceTransform);
            }

            // Analyze peak spacing
            using PooledResource<List<float>> xPositionsLease = Buffers<float>.List.Get(
                out List<float> xPositions
            );
            using PooledResource<List<float>> yPositionsLease = Buffers<float>.List.Get(
                out List<float> yPositions
            );

            for (int i = 0; i < localMaxima.Count; ++i)
            {
                xPositions.Add(localMaxima[i].x);
                yPositions.Add(localMaxima[i].y);
            }

            xPositions.Sort();
            yPositions.Sort();

            float xSpacing = FindConsistentSpacing(xPositions);
            float ySpacing = FindConsistentSpacing(yPositions);

            if (xSpacing < MinimumCellSize && ySpacing < MinimumCellSize)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.DistanceTransform);
            }

            int cellWidth = xSpacing >= MinimumCellSize ? Mathf.RoundToInt(xSpacing) : textureWidth;
            int cellHeight =
                ySpacing >= MinimumCellSize ? Mathf.RoundToInt(ySpacing) : textureHeight;

            cellWidth = SnapToCommonSize(cellWidth);
            cellHeight = SnapToCommonSize(cellHeight);
            cellWidth = FindNearestDivisor(textureWidth, cellWidth);
            cellHeight = FindNearestDivisor(textureHeight, cellHeight);

            if (cellWidth < MinimumCellSize || cellHeight < MinimumCellSize)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.DistanceTransform);
            }

            float xConsistency = CalculateSpacingConsistency(xPositions, cellWidth);
            float yConsistency = CalculateSpacingConsistency(yPositions, cellHeight);
            float confidence = (xConsistency + yConsistency) * 0.5f;

            return new AlgorithmResult(
                cellWidth,
                cellHeight,
                confidence,
                AutoDetectionAlgorithm.DistanceTransform
            );
        }

        /// <summary>
        /// RegionGrowing algorithm: seeds from local intensity maxima and grows regions
        /// via 4-connected flood-fill to alpha boundaries.
        /// </summary>
        private static AlgorithmResult DetectGridRegionGrowing(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            float alphaThreshold,
            CancellationToken cancellationToken
        )
        {
            byte alphaThresholdByte = (byte)(alphaThreshold * 255f);

            // Find seed points based on intensity (sum of RGB)
            using PooledArray<int> intensityLease = SystemArrayPool<int>.Get(
                pixels.Length,
                out int[] intensity
            );

            for (int i = 0; i < pixels.Length; ++i)
            {
                Color32 c = pixels[i];
                intensity[i] = c.a > alphaThresholdByte ? c.r + c.g + c.b : 0;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.RegionGrowing);
            }

            // Find local intensity maxima as seeds
            using PooledResource<List<Vector2Int>> seedsLease = Buffers<Vector2Int>.List.Get(
                out List<Vector2Int> seeds
            );

            int windowSize = Math.Max(8, Math.Min(textureWidth, textureHeight) / 16);
            for (int y = windowSize; y < textureHeight - windowSize; y += windowSize / 2)
            {
                for (int x = windowSize; x < textureWidth - windowSize; x += windowSize / 2)
                {
                    int idx = y * textureWidth + x;
                    int val = intensity[idx];

                    if (val <= 0)
                    {
                        continue;
                    }

                    bool isMaximum = true;
                    for (int dy = -windowSize / 4; dy <= windowSize / 4 && isMaximum; dy += 2)
                    {
                        for (int dx = -windowSize / 4; dx <= windowSize / 4 && isMaximum; dx += 2)
                        {
                            if (dx == 0 && dy == 0)
                            {
                                continue;
                            }
                            int neighborIdx = (y + dy) * textureWidth + (x + dx);
                            if (
                                neighborIdx >= 0
                                && neighborIdx < pixels.Length
                                && intensity[neighborIdx] > val
                            )
                            {
                                isMaximum = false;
                            }
                        }
                    }

                    if (isMaximum)
                    {
                        seeds.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.RegionGrowing);
            }

            if (seeds.Count < 2)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.RegionGrowing);
            }

            // Grow regions from seeds and measure sizes
            using PooledArray<int> regionLease = SystemArrayPool<int>.Get(
                pixels.Length,
                out int[] regionId
            );
            Array.Clear(regionId, 0, regionId.Length);

            using PooledResource<List<int>> regionSizesLease = Buffers<int>.List.Get(
                out List<int> regionSizes
            );
            using PooledResource<List<Rect>> regionBoundsLease = Buffers<Rect>.List.Get(
                out List<Rect> regionBounds
            );

            int currentRegion = 0;
            using PooledResource<List<int>> stackLease = Buffers<int>.List.Get(out List<int> stack);

            for (int seedIdx = 0; seedIdx < seeds.Count && currentRegion < 256; ++seedIdx)
            {
                Vector2Int seed = seeds[seedIdx];
                int startIdx = seed.y * textureWidth + seed.x;

                if (regionId[startIdx] != 0 || pixels[startIdx].a <= alphaThresholdByte)
                {
                    continue;
                }

                ++currentRegion;
                stack.Clear();
                stack.Add(startIdx);
                regionId[startIdx] = currentRegion;

                int size = 0;
                int minX = seed.x;
                int maxX = seed.x;
                int minY = seed.y;
                int maxY = seed.y;

                while (stack.Count > 0)
                {
                    int lastIndex = stack.Count - 1;
                    int idx = stack[lastIndex];
                    stack.RemoveAt(lastIndex);

                    ++size;
                    int px = idx % textureWidth;
                    int py = idx / textureWidth;

                    if (px < minX)
                    {
                        minX = px;
                    }
                    if (px > maxX)
                    {
                        maxX = px;
                    }
                    if (py < minY)
                    {
                        minY = py;
                    }
                    if (py > maxY)
                    {
                        maxY = py;
                    }

                    // 4-connected neighbors
                    if (px > 0)
                    {
                        int left = idx - 1;
                        if (regionId[left] == 0 && pixels[left].a > alphaThresholdByte)
                        {
                            regionId[left] = currentRegion;
                            stack.Add(left);
                        }
                    }
                    if (px < textureWidth - 1)
                    {
                        int right = idx + 1;
                        if (regionId[right] == 0 && pixels[right].a > alphaThresholdByte)
                        {
                            regionId[right] = currentRegion;
                            stack.Add(right);
                        }
                    }
                    if (py > 0)
                    {
                        int bottom = idx - textureWidth;
                        if (regionId[bottom] == 0 && pixels[bottom].a > alphaThresholdByte)
                        {
                            regionId[bottom] = currentRegion;
                            stack.Add(bottom);
                        }
                    }
                    if (py < textureHeight - 1)
                    {
                        int top = idx + textureWidth;
                        if (regionId[top] == 0 && pixels[top].a > alphaThresholdByte)
                        {
                            regionId[top] = currentRegion;
                            stack.Add(top);
                        }
                    }
                }

                if (size > MinimumCellSize * MinimumCellSize / 4)
                {
                    regionSizes.Add(size);
                    regionBounds.Add(new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1));
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.RegionGrowing);
            }

            if (regionBounds.Count < 2)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.RegionGrowing);
            }

            // Compute average region size
            float avgWidth = 0f;
            float avgHeight = 0f;
            for (int i = 0; i < regionBounds.Count; ++i)
            {
                avgWidth += regionBounds[i].width;
                avgHeight += regionBounds[i].height;
            }
            avgWidth /= regionBounds.Count;
            avgHeight /= regionBounds.Count;

            int cellWidth = Mathf.RoundToInt(avgWidth);
            int cellHeight = Mathf.RoundToInt(avgHeight);

            cellWidth = SnapToCommonSize(cellWidth);
            cellHeight = SnapToCommonSize(cellHeight);
            cellWidth = FindNearestDivisor(textureWidth, cellWidth);
            cellHeight = FindNearestDivisor(textureHeight, cellHeight);

            if (cellWidth < MinimumCellSize || cellHeight < MinimumCellSize)
            {
                return AlgorithmResult.Invalid(AutoDetectionAlgorithm.RegionGrowing);
            }

            // Calculate confidence based on region size uniformity
            float widthVariance = 0f;
            float heightVariance = 0f;
            for (int i = 0; i < regionBounds.Count; ++i)
            {
                float wDiff = regionBounds[i].width - avgWidth;
                float hDiff = regionBounds[i].height - avgHeight;
                widthVariance += wDiff * wDiff;
                heightVariance += hDiff * hDiff;
            }
            widthVariance /= regionBounds.Count;
            heightVariance /= regionBounds.Count;

            float widthStdDev = Mathf.Sqrt(widthVariance);
            float heightStdDev = Mathf.Sqrt(heightVariance);

            float widthCoeffVar = avgWidth > 0 ? widthStdDev / avgWidth : 1f;
            float heightCoeffVar = avgHeight > 0 ? heightStdDev / avgHeight : 1f;

            float confidence = Mathf.Clamp01(1f - (widthCoeffVar + heightCoeffVar) * 0.5f);

            return new AlgorithmResult(
                cellWidth,
                cellHeight,
                confidence,
                AutoDetectionAlgorithm.RegionGrowing
            );
        }

        /// <summary>
        /// Detects individual sprite bounds by flood-filling opaque regions.
        /// </summary>
        private static void DetectSpriteBoundsByAlpha(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            byte alphaThresholdByte,
            List<Rect> result,
            CancellationToken cancellationToken
        )
        {
            result.Clear();

            using PooledArray<bool> visitedLease = SystemArrayPool<bool>.Get(
                pixels.Length,
                out bool[] visited
            );
            Array.Clear(visited, 0, visited.Length);

            using PooledResource<List<int>> stackLease = Buffers<int>.List.Get(out List<int> stack);

            for (int y = 0; y < textureHeight; ++y)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                for (int x = 0; x < textureWidth; ++x)
                {
                    int index = y * textureWidth + x;
                    if (visited[index])
                    {
                        continue;
                    }

                    if (pixels[index].a <= alphaThresholdByte)
                    {
                        visited[index] = true;
                        continue;
                    }

                    int minX = x;
                    int maxX = x;
                    int minY = y;
                    int maxY = y;

                    stack.Clear();
                    stack.Add(index);
                    visited[index] = true;

                    while (stack.Count > 0)
                    {
                        int lastIndex = stack.Count - 1;
                        int current = stack[lastIndex];
                        stack.RemoveAt(lastIndex);

                        int currentX = current % textureWidth;
                        int currentY = current / textureWidth;

                        if (currentX < minX)
                        {
                            minX = currentX;
                        }
                        if (currentX > maxX)
                        {
                            maxX = currentX;
                        }
                        if (currentY < minY)
                        {
                            minY = currentY;
                        }
                        if (currentY > maxY)
                        {
                            maxY = currentY;
                        }

                        // 4-connected neighbors
                        if (currentX > 0)
                        {
                            int leftIndex = current - 1;
                            if (!visited[leftIndex] && pixels[leftIndex].a > alphaThresholdByte)
                            {
                                visited[leftIndex] = true;
                                stack.Add(leftIndex);
                            }
                        }
                        if (currentX < textureWidth - 1)
                        {
                            int rightIndex = current + 1;
                            if (!visited[rightIndex] && pixels[rightIndex].a > alphaThresholdByte)
                            {
                                visited[rightIndex] = true;
                                stack.Add(rightIndex);
                            }
                        }
                        if (currentY > 0)
                        {
                            int bottomIndex = current - textureWidth;
                            if (!visited[bottomIndex] && pixels[bottomIndex].a > alphaThresholdByte)
                            {
                                visited[bottomIndex] = true;
                                stack.Add(bottomIndex);
                            }
                        }
                        if (currentY < textureHeight - 1)
                        {
                            int topIndex = current + textureWidth;
                            if (!visited[topIndex] && pixels[topIndex].a > alphaThresholdByte)
                            {
                                visited[topIndex] = true;
                                stack.Add(topIndex);
                            }
                        }
                    }

                    int width = maxX - minX + 1;
                    int height = maxY - minY + 1;

                    if (width >= 2 && height >= 2)
                    {
                        result.Add(new Rect(minX, minY, width, height));
                    }
                }
            }
        }

        /// <summary>
        /// Generates candidate cell sizes for a given dimension.
        /// </summary>
        private static void GenerateCandidateCellSizes(int dimension, List<int> candidates)
        {
            candidates.Clear();

            // Add common sizes that divide evenly
            for (int i = 0; i < CommonCellSizes.Length; ++i)
            {
                int size = CommonCellSizes[i];
                if (size >= MinimumCellSize && size <= dimension && dimension % size == 0)
                {
                    candidates.Add(size);
                }
            }

            // Add all divisors >= MinimumCellSize
            for (int div = MinimumCellSize; div <= dimension / 2; ++div)
            {
                if (dimension % div == 0 && !candidates.Contains(div))
                {
                    candidates.Add(div);
                }
            }

            // Add the full dimension itself
            if (!candidates.Contains(dimension))
            {
                candidates.Add(dimension);
            }
        }

        /// <summary>
        /// Scores a cell size by measuring boundary transparency.
        /// </summary>
        private static float ScoreCellSizeForDimension(
            int[] transparencyCount,
            int dimension,
            int orthogonalDimension,
            int cellSize
        )
        {
            if (cellSize <= 0 || dimension % cellSize != 0)
            {
                return 0f;
            }

            int numBoundaries = dimension / cellSize - 1;
            if (numBoundaries <= 0)
            {
                return 0f;
            }

            float totalScore = 0f;
            for (int i = 1; i <= numBoundaries; ++i)
            {
                int boundaryPos = i * cellSize;
                if (boundaryPos < dimension)
                {
                    float transparency =
                        (float)transparencyCount[boundaryPos] / orthogonalDimension;
                    totalScore += transparency;
                }
            }

            return totalScore / numBoundaries;
        }

        /// <summary>
        /// Finds consistent spacing from a sorted list of positions.
        /// </summary>
        private static float FindConsistentSpacing(List<float> sortedPositions)
        {
            if (sortedPositions.Count < 2)
            {
                return 0f;
            }

            using PooledResource<List<float>> spacingsLease = Buffers<float>.List.Get(
                out List<float> spacings
            );

            for (int i = 1; i < sortedPositions.Count; ++i)
            {
                float spacing = sortedPositions[i] - sortedPositions[i - 1];
                if (spacing >= MinimumCellSize)
                {
                    spacings.Add(spacing);
                }
            }

            if (spacings.Count == 0)
            {
                return 0f;
            }

            // Find the mode (most common spacing)
            spacings.Sort();
            float modeSpacing = spacings[0];
            int modeCount = 1;
            int currentCount = 1;
            float currentSpacing = spacings[0];

            for (int i = 1; i < spacings.Count; ++i)
            {
                if (Mathf.Abs(spacings[i] - currentSpacing) < 2f)
                {
                    ++currentCount;
                }
                else
                {
                    if (currentCount > modeCount)
                    {
                        modeCount = currentCount;
                        modeSpacing = currentSpacing;
                    }
                    currentSpacing = spacings[i];
                    currentCount = 1;
                }
            }

            if (currentCount > modeCount)
            {
                modeSpacing = currentSpacing;
            }

            return modeSpacing;
        }

        /// <summary>
        /// Calculates spacing consistency as confidence metric.
        /// </summary>
        private static float CalculateSpacingConsistency(List<float> sortedPositions, int cellSize)
        {
            if (sortedPositions.Count < 2 || cellSize <= 0)
            {
                return 0f;
            }

            int matchCount = 0;
            for (int i = 1; i < sortedPositions.Count; ++i)
            {
                float spacing = sortedPositions[i] - sortedPositions[i - 1];
                float expectedMultiple = Mathf.Round(spacing / cellSize);
                if (expectedMultiple >= 1f)
                {
                    float expectedSpacing = expectedMultiple * cellSize;
                    if (Mathf.Abs(spacing - expectedSpacing) < cellSize * 0.2f)
                    {
                        ++matchCount;
                    }
                }
            }

            return (float)matchCount / (sortedPositions.Count - 1);
        }

        /// <summary>
        /// Snaps a value to the nearest common cell size if within tolerance.
        /// </summary>
        private static int SnapToCommonSize(int value)
        {
            const int tolerance = 4;
            for (int i = 0; i < CommonCellSizes.Length; ++i)
            {
                if (Mathf.Abs(value - CommonCellSizes[i]) <= tolerance)
                {
                    return CommonCellSizes[i];
                }
            }
            return value;
        }

        /// <summary>
        /// Finds the nearest divisor of dimension to the target value.
        /// </summary>
        private static int FindNearestDivisor(int dimension, int target)
        {
            if (dimension % target == 0)
            {
                return target;
            }

            int bestDivisor = target;
            int bestDiff = int.MaxValue;

            for (int div = MinimumCellSize; div <= dimension; ++div)
            {
                if (dimension % div == 0)
                {
                    int diff = Mathf.Abs(div - target);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        bestDivisor = div;
                    }
                }
            }

            return bestDivisor;
        }

        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
#endif
}
