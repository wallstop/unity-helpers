namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Contract for services that assign relational components (parent/child/sibling) to
    /// decorated fields.
    /// </summary>
    public interface IRelationalComponentAssigner
    {
        /// <summary>
        /// Returns <c>true</c> when the supplied component type has at least one field decorated with a
        /// relational attribute.
        /// </summary>
        bool HasRelationalAssignments(Type componentType);

        /// <summary>
        /// Assigns relational component references for a single component instance if any decorated
        /// fields are present.
        /// </summary>
        void Assign(Component component);

        /// <summary>
        /// Assigns relational component references for a collection of component instances.
        /// </summary>
        void Assign(IEnumerable<Component> components);

        /// <summary>
        /// Recursively assigns relational component references for all components found beneath the
        /// supplied root GameObject.
        /// </summary>
        void AssignHierarchy(GameObject root, bool includeInactiveChildren = true);
    }
}
