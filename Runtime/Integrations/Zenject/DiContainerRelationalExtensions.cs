#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.Zenject
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using Zenject;

    /// <summary>
    /// Helper extensions that bridge Zenject container operations with relational component
    /// assignment.
    /// </summary>
    public static class DiContainerRelationalExtensions
    {
        /// <summary>
        /// Injects the supplied component (and optionally assigns relational fields) using the
        /// container.
        /// </summary>
        public static void AssignRelationalComponents(
            this DiContainer container,
            Component component
        )
        {
            if (component == null)
            {
                return;
            }

            IRelationalComponentAssigner assigner = ResolveAssigner(container);
            if (assigner != null)
            {
                assigner.Assign(component);
                return;
            }

            component.AssignRelationalComponents();
        }

        /// <summary>
        /// Assigns relational fields for all components in the provided hierarchy after Zenject has
        /// injected them.
        /// </summary>
        public static void AssignRelationalHierarchy(
            this DiContainer container,
            GameObject root,
            bool includeInactiveChildren = true
        )
        {
            if (root == null)
            {
                return;
            }

            IRelationalComponentAssigner assigner = ResolveAssigner(container);
            if (assigner != null)
            {
                assigner.AssignHierarchy(root, includeInactiveChildren);
                return;
            }

            Component[] components = root.GetComponentsInChildren<Component>(
                includeInactiveChildren
            );
            for (int i = 0; i < components.Length; i++)
            {
                components[i]?.AssignRelationalComponents();
            }
        }

        /// <summary>
        /// Instantiates a prefab, runs Zenject injection, and assigns relational component fields on
        /// the returned component.
        /// </summary>
        public static T InstantiateComponentWithRelations<T>(
            this DiContainer container,
            T prefab,
            Transform parent = null
        )
            where T : Component
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            T instance = container.InstantiatePrefabForComponent(prefab, parent);
            container.AssignRelationalComponents(instance);
            return instance;
        }

        private static IRelationalComponentAssigner ResolveAssigner(DiContainer container)
        {
            if (container == null)
            {
                return null;
            }

            return container.ResolveOptional<IRelationalComponentAssigner>();
        }
    }
}
#endif
