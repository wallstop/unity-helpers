// MIT License - Copyright (c) 2023 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Helper
{
    /// <summary>
    /// Provides utility methods for atomic value exchange operations.
    /// </summary>
    /// <remarks>
    /// Thread Safety: Methods are not inherently thread-safe. For thread-safe exchanges, use Interlocked.Exchange.
    /// Performance: O(1) operations with minimal overhead.
    /// </remarks>
    public static class AssignUtilities
    {
        /// <summary>
        /// Atomically exchanges a variable's value and returns the old value.
        /// </summary>
        /// <typeparam name="T">The type of the values being exchanged.</typeparam>
        /// <param name="assignTo">Reference to the variable to assign to.</param>
        /// <param name="assignFrom">The new value to assign.</param>
        /// <returns>The old value of assignTo before the exchange.</returns>
        /// <remarks>
        /// Null handling: Works with null values for reference types.
        /// Thread-safe: No. Use Interlocked.Exchange for thread-safe operations.
        /// Performance: O(1) - single assignment and return.
        /// Allocations: None.
        /// Edge cases: For reference types, both old and new values can be null.
        /// </remarks>
        public static T Exchange<T>(ref T assignTo, T assignFrom)
        {
            T oldValue = assignTo;
            assignTo = assignFrom;
            return oldValue;
        }
    }
}
