// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.VContainer
{
    using System;
    using global::VContainer;
    using global::VContainer.Unity;
    using UnityEngine;
    using UnityEngine.Pool;

    /// <summary>
    /// Helpers for creating UnityEngine.Pool object pools plus extensions that hydrate pooled items
    /// through VContainer when you rent them via <see cref="GetWithRelations{T}(ObjectPool{T},IObjectResolver)"/>.
    /// </summary>
    public static class RelationalObjectPools
    {
        /// <summary>
        /// Creates a component pool to be combined with <see cref="GetWithRelations{T}(ObjectPool{T},IObjectResolver)"/>
        /// so items are injected and hydrated on rental time.
        /// </summary>
        public static ObjectPool<T> CreatePoolWithRelations<T>(
            Func<T> createFunc,
            Action<T> actionOnGet = null,
            Action<T> actionOnRelease = null,
            Action<T> actionOnDestroy = null,
            bool collectionCheck = true,
            int defaultCapacity = 10,
            int maxSize = 10000
        )
            where T : Component
        {
            if (createFunc == null)
            {
                throw new ArgumentNullException(nameof(createFunc));
            }

            return new ObjectPool<T>(
                createFunc,
                actionOnGet: actionOnGet,
                actionOnRelease: actionOnRelease,
                actionOnDestroy: actionOnDestroy,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        /// <summary>
        /// Creates a GameObject pool to be combined with
        /// <see cref="GetWithRelations(ObjectPool{GameObject},IObjectResolver)"/> so hierarchies are
        /// injected and hydrated on rental time.
        /// </summary>
        public static ObjectPool<GameObject> CreateGameObjectPoolWithRelations(
            GameObject prefab,
            Transform parent = null,
            Action<GameObject> actionOnGet = null,
            Action<GameObject> actionOnRelease = null,
            Action<GameObject> actionOnDestroy = null,
            bool collectionCheck = true,
            int defaultCapacity = 10,
            int maxSize = 10000
        )
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            Func<GameObject> create = () => UnityEngine.Object.Instantiate(prefab, parent);

            return new ObjectPool<GameObject>(
                create,
                actionOnGet: actionOnGet,
                actionOnRelease: actionOnRelease,
                actionOnDestroy: actionOnDestroy,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        /// <summary>
        /// Rents an item from the pool, injects and assigns relational fields.
        /// </summary>
        public static T GetWithRelations<T>(this ObjectPool<T> pool, IObjectResolver resolver)
            where T : Component
        {
            T item = pool.Get();
            if (item != null)
            {
                resolver?.Inject(item);
                resolver.AssignRelationalComponents(item);
            }
            return item;
        }

        /// <summary>
        /// Rents a GameObject from the pool, injects the hierarchy and assigns relational fields.
        /// </summary>
        public static GameObject GetWithRelations(
            this ObjectPool<GameObject> pool,
            IObjectResolver resolver
        )
        {
            GameObject go = pool.Get();
            if (go != null)
            {
                resolver?.InjectGameObject(go);
                resolver.AssignRelationalHierarchy(go, includeInactiveChildren: true);
            }
            return go;
        }
    }
}
#endif
