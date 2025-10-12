#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.VContainer
{
    using System.Collections.Generic;
    using global::VContainer;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Convenience extensions for assigning relational component fields when using VContainer.
    /// </summary>
    public static class ObjectResolverRelationalExtensions
    {
        public static void AssignRelationalComponents(
            this IObjectResolver resolver,
            Component component
        )
        {
            if (component == null)
            {
                return;
            }

            if (TryResolveAssigner(resolver, out IRelationalComponentAssigner assigner))
            {
                assigner.Assign(component);
            }
            else
            {
                component.AssignRelationalComponents();
            }
        }

        public static void AssignRelationalHierarchy(
            this IObjectResolver resolver,
            GameObject root,
            bool includeInactiveChildren = true
        )
        {
            if (root == null)
            {
                return;
            }

            if (TryResolveAssigner(resolver, out IRelationalComponentAssigner assigner))
            {
                assigner.AssignHierarchy(root, includeInactiveChildren);
                return;
            }

            using PooledResource<List<Component>> componentBuffer = Buffers<Component>.List.Get(
                out List<Component> components
            );
            root.GetComponentsInChildren(includeInactiveChildren, components);
            for (int i = 0; i < components.Count; i++)
            {
                components[i].AssignRelationalComponents();
            }
        }

        public static T BuildUpWithRelations<T>(this IObjectResolver resolver, T component)
            where T : Component
        {
            if (component == null)
            {
                return null;
            }

            // Use Inject for compatibility with VContainer 1.16.x
            resolver?.Inject(component);
            resolver.AssignRelationalComponents(component);
            return component;
        }

        private static bool TryResolveAssigner(
            IObjectResolver resolver,
            out IRelationalComponentAssigner assigner
        )
        {
            if (resolver != null && resolver.TryResolve(out assigner))
            {
                return assigner != null;
            }

            assigner = null;
            return false;
        }
    }
}
#endif
