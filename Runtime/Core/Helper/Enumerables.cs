namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides utility methods for creating IEnumerable instances from elements.
    /// </summary>
    /// <remarks>
    /// Thread Safety: All methods are thread-safe.
    /// Performance: O(1) operations that wrap arrays.
    /// Allocations: Allocates array for single element overload, returns params array for multi-element overload.
    /// </remarks>
    public static class Enumerables
    {
        /// <summary>
        /// Creates an IEnumerable containing a single element.
        /// </summary>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <param name="element">The single element to wrap in an enumerable.</param>
        /// <returns>An IEnumerable containing only the specified element.</returns>
        /// <remarks>
        /// Null handling: Element can be null for reference types.
        /// Thread-safe: Yes.
        /// Performance: O(1).
        /// Allocations: Allocates a single-element array.
        /// Edge cases: Works with null elements for reference types.
        /// </remarks>
        public static IEnumerable<T> Of<T>(T element)
        {
            return new[] { element };
        }

        /// <summary>
        /// Creates an IEnumerable containing multiple elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="elements">The elements to wrap in an enumerable.</param>
        /// <returns>An IEnumerable containing all the specified elements.</returns>
        /// <remarks>
        /// Null handling: Elements can contain nulls for reference types. If elements itself is null, returns null.
        /// Thread-safe: Yes.
        /// Performance: O(1) - directly returns the params array.
        /// Allocations: Params array is allocated by the compiler if not already an array.
        /// Edge cases: Empty params returns empty array. Can be called with explicit array to avoid allocation.
        /// </remarks>
        public static IEnumerable<T> Of<T>(params T[] elements)
        {
            return elements;
        }
    }
}
