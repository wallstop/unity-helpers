namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using UnityEngine;

    /// <summary>
    /// Extension methods for automatically assigning components based on relational attributes.
    /// </summary>
    public static class RelationalComponentExtensions
    {
        /// <summary>
        /// Assigns all relational components (parent, sibling, and child components) to fields marked with the corresponding attributes.
        /// </summary>
        /// <param name="component">The component on which to perform the assignment.</param>
        /// <remarks>
        /// This is a convenience method that calls AssignParentComponents, AssignSiblingComponents, and AssignChildComponents in sequence.
        /// Fields must be marked with ParentComponentAttribute, SiblingComponentAttribute, or ChildComponentAttribute for automatic assignment.
        /// Null handling: If the component is null, this will cause a NullReferenceException.
        /// Thread-safe: No. Must be called from the main Unity thread.
        /// Performance: O(n*m) where n is the number of attributed fields and m is the search space for each component.
        /// </remarks>
        /// <seealso cref="ParentComponentAttribute"/>
        /// <seealso cref="SiblingComponentAttribute"/>
        /// <seealso cref="ChildComponentAttribute"/>
        public static void AssignRelationalComponents(this Component component)
        {
            component.AssignParentComponents();
            component.AssignSiblingComponents();
            component.AssignChildComponents();
        }
    }
}
