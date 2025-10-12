#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.Zenject
{
    using System;
    using System.Collections.Generic;
    using global::Zenject;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Helper extensions that bridge Zenject container operations with relational component
    /// assignment.
    /// </summary>
    /// <remarks>
    /// These helpers prefer a container-registered <see cref="IRelationalComponentAssigner"/> when
    /// available (via <see cref="RelationalComponentsInstaller"/> or a manual binding). If no
    /// registration exists, they safely fall back to the non-DI assignment path so fields still get
    /// populated.
    /// </remarks>
    /// <example>
    /// <code>
    /// // SceneContext: add RelationalComponentsInstaller to bind assigner and run scene initialization
    /// public sealed class GameInstaller : MonoInstaller
    /// {
    ///     public override void InstallBindings()
    ///     {
    ///         // Your bindings...
    ///     }
    /// }
    ///
    /// // Prefab instantiation with DI + relational assignment
    /// public sealed class Spawner
    /// {
    ///     readonly DiContainer _container;
    ///     readonly Enemy _enemyPrefab;
    ///
    ///     public Spawner(DiContainer container, Enemy enemyPrefab)
    ///     {
    ///         _container = container;
    ///         _enemyPrefab = enemyPrefab;
    ///     }
    ///
    ///     public Enemy Spawn(Transform parent)
    ///     {
    ///         return _container.InstantiateComponentWithRelations(_enemyPrefab, parent);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static class DiContainerRelationalExtensions
    {
        /// <summary>
        /// Assigns all relational fields on a component using the container-registered
        /// <see cref="IRelationalComponentAssigner"/>, with a safe fallback to the non-DI path.
        /// </summary>
        /// <param name="container">The Zenject container.</param>
        /// <param name="component">The component to hydrate.</param>
        /// <remarks>
        /// Call this after <c>container.Inject(component)</c> if the instance was not created by
        /// Zenject, or use <see cref="InstantiateComponentWithRelations{T}(DiContainer,T,UnityEngine.Transform)"/>
        /// to combine instantiation and assignment.
        /// </remarks>
        /// <example>
        /// <code>
        /// container.Inject(controller);
        /// container.AssignRelationalComponents(controller);
        /// </code>
        /// </example>
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
        /// Assigns relational fields for all components in a hierarchy.
        /// </summary>
        /// <param name="container">The Zenject container.</param>
        /// <param name="root">Root GameObject to scan.</param>
        /// <param name="includeInactiveChildren">Whether to include inactive children (default true).</param>
        /// <remarks>
        /// If an assigner binding exists it is used to recursively process the hierarchy; otherwise
        /// the method iterates components and calls the non-DI assignment path for each.
        /// </remarks>
        /// <example>
        /// <code>
        /// container.AssignRelationalHierarchy(root, includeInactiveChildren: false);
        /// </code>
        /// </example>
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

            using PooledResource<List<Component>> componentBuffer = Buffers<Component>.List.Get(
                out List<Component> components
            );
            root.GetComponentsInChildren(includeInactiveChildren, components);
            for (int i = 0; i < components.Count; i++)
            {
                components[i].AssignRelationalComponents();
            }
        }

        /// <summary>
        /// Instantiates a prefab, runs Zenject injection, and assigns relational component fields on
        /// the returned component.
        /// </summary>
        /// <typeparam name="T">The component type present on the prefab root.</typeparam>
        /// <param name="container">The Zenject container.</param>
        /// <param name="prefab">Prefab that contains the component of type <typeparamref name="T"/>.</param>
        /// <param name="parent">Optional parent transform.</param>
        /// <returns>The instantiated component with DI and relational fields populated.</returns>
        /// <example>
        /// <code>
        /// var enemy = container.InstantiateComponentWithRelations(enemyPrefab, parent);
        /// </code>
        /// </example>
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

            T instance = container.InstantiatePrefabForComponent<T>(prefab, parent);
            container.AssignRelationalComponents(instance);
            return instance;
        }

        private static IRelationalComponentAssigner ResolveAssigner(DiContainer container)
        {
            if (container == null)
            {
                return null;
            }

            if (container.HasBinding(typeof(IRelationalComponentAssigner)))
            {
                return container.Resolve<IRelationalComponentAssigner>();
            }

            return null;
        }
    }
}
#endif
