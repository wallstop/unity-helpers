// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestUtils
{
#if UNITY_EDITOR
    using System;
    using System.Threading;
    using UnityEditor;

    /// <summary>
    ///     A disposable scope that batches AssetDatabase operations for improved performance.
    ///     Calls <see cref="AssetDatabase.StartAssetEditing"/> and <see cref="AssetDatabase.DisallowAutoRefresh"/>
    ///     on construction, and <see cref="AssetDatabase.AllowAutoRefresh"/>, <see cref="AssetDatabase.StopAssetEditing"/>,
    ///     and optionally <see cref="AssetDatabase.Refresh"/> on disposal.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     Use this struct with a <c>using</c> statement to automatically batch asset operations:
    ///     </para>
    ///     <code>
    ///     using (AssetDatabaseBatchHelper.BeginBatch())
    ///     {
    ///         // Multiple asset operations batched together
    ///         AssetDatabase.CreateAsset(obj, path);
    ///         AssetDatabase.DeleteAsset(oldPath);
    ///     }
    ///     </code>
    ///     <para>
    ///     Nested scopes are supported. The actual AssetDatabase calls are only made at the outermost scope.
    ///     </para>
    /// </remarks>
    public readonly struct AssetDatabaseBatchScope : IDisposable
    {
        private readonly bool _shouldRefreshOnDispose;
        private readonly bool _isOutermostScope;

        /// <summary>
        ///     Creates a new batch scope. Use <see cref="AssetDatabaseBatchHelper.BeginBatch"/> instead of calling this directly.
        /// </summary>
        /// <param name="refreshOnDispose">Whether to call <see cref="AssetDatabase.Refresh"/> when disposing.</param>
        internal AssetDatabaseBatchScope(bool refreshOnDispose)
        {
            _shouldRefreshOnDispose = refreshOnDispose;
            _isOutermostScope = AssetDatabaseBatchHelper.IncrementBatchDepth();

            if (_isOutermostScope)
            {
                AssetDatabase.StartAssetEditing();
                AssetDatabase.DisallowAutoRefresh();
            }
        }

        /// <summary>
        ///     Ends the batch scope. If this is the outermost scope, calls
        ///     <see cref="AssetDatabase.AllowAutoRefresh"/>, <see cref="AssetDatabase.StopAssetEditing"/>,
        ///     and optionally <see cref="AssetDatabase.Refresh"/>.
        /// </summary>
        public void Dispose()
        {
            bool wasOutermost = AssetDatabaseBatchHelper.DecrementBatchDepth();

            if (wasOutermost && _isOutermostScope)
            {
                AssetDatabase.AllowAutoRefresh();
                AssetDatabase.StopAssetEditing();

                if (_shouldRefreshOnDispose)
                {
                    AssetDatabase.Refresh();
                }
            }
        }
    }

    /// <summary>
    ///     Provides static helper methods for managing AssetDatabase batch operations.
    /// </summary>
    public static class AssetDatabaseBatchHelper
    {
        private static int _batchDepth;

        /// <summary>
        ///     Gets a value indicating whether AssetDatabase operations are currently being batched.
        /// </summary>
        public static bool IsCurrentlyBatching => Volatile.Read(ref _batchDepth) > 0;

        /// <summary>
        ///     Gets the current nesting depth of batch scopes.
        /// </summary>
        public static int CurrentBatchDepth => Volatile.Read(ref _batchDepth);

        /// <summary>
        ///     Begins a new AssetDatabase batch scope. All asset operations within the scope
        ///     are batched together for improved performance.
        /// </summary>
        /// <param name="refreshOnDispose">
        ///     Whether to call <see cref="AssetDatabase.Refresh"/> when the scope is disposed.
        ///     Defaults to <c>true</c>.
        /// </param>
        /// <returns>A disposable scope that ends the batch when disposed.</returns>
        /// <example>
        ///     <code>
        ///     using (AssetDatabaseBatchHelper.BeginBatch())
        ///     {
        ///         AssetDatabase.CreateAsset(obj1, path1);
        ///         AssetDatabase.CreateAsset(obj2, path2);
        ///         AssetDatabase.DeleteAsset(oldPath);
        ///     }
        ///     // Assets are now committed and database is refreshed
        ///     </code>
        /// </example>
        public static AssetDatabaseBatchScope BeginBatch(bool refreshOnDispose = true)
        {
            return new AssetDatabaseBatchScope(refreshOnDispose);
        }

        /// <summary>
        ///     Calls <see cref="AssetDatabase.Refresh"/> only if no batch scope is currently active.
        ///     Use this when you need to ensure assets are refreshed but want to respect active batch scopes.
        /// </summary>
        /// <remarks>
        ///     This method is useful for code that may be called both inside and outside of batch scopes.
        ///     When inside a batch scope, the refresh will be skipped (and handled by the scope disposal).
        ///     When outside a batch scope, the refresh will be performed immediately.
        /// </remarks>
        public static void RefreshIfNotBatching()
        {
            if (!IsCurrentlyBatching)
            {
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        ///     Calls <see cref="AssetDatabase.Refresh"/> only if no batch scope is currently active.
        ///     Use this when you need to ensure assets are refreshed but want to respect active batch scopes.
        /// </summary>
        /// <param name="options">The import options to use if refresh is performed.</param>
        /// <remarks>
        ///     This method is useful for code that may be called both inside and outside of batch scopes.
        ///     When inside a batch scope, the refresh will be skipped (and handled by the scope disposal).
        ///     When outside a batch scope, the refresh will be performed immediately.
        /// </remarks>
        public static void RefreshIfNotBatching(ImportAssetOptions options)
        {
            if (!IsCurrentlyBatching)
            {
                AssetDatabase.Refresh(options);
            }
        }

        /// <summary>
        ///     Increments the batch depth counter and returns whether this is the outermost scope.
        /// </summary>
        /// <returns><c>true</c> if this is the outermost (first) scope; otherwise, <c>false</c>.</returns>
        internal static bool IncrementBatchDepth()
        {
            int previousDepth = Interlocked.Increment(ref _batchDepth) - 1;
            return previousDepth == 0;
        }

        /// <summary>
        ///     Decrements the batch depth counter and returns whether this was the outermost scope.
        /// </summary>
        /// <returns><c>true</c> if this was the outermost scope (depth is now 0); otherwise, <c>false</c>.</returns>
        internal static bool DecrementBatchDepth()
        {
            int newDepth = Interlocked.Decrement(ref _batchDepth);

            if (newDepth < 0)
            {
                Interlocked.Exchange(ref _batchDepth, 0);
                return false;
            }

            return newDepth == 0;
        }

        /// <summary>
        ///     Resets the batch depth counter to zero. Use only in test cleanup scenarios.
        /// </summary>
        /// <remarks>
        ///     This method should only be used in test teardown to ensure clean state between tests.
        ///     Using this in production code may leave the AssetDatabase in an inconsistent state.
        /// </remarks>
        internal static void ResetBatchDepth()
        {
            Interlocked.Exchange(ref _batchDepth, 0);
        }
    }
#endif
}
