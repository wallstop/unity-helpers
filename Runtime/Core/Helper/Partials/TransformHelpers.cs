namespace UnityHelpers.Core.Helper
{
    using System.Collections.Generic;
    using UnityEngine;

    public static partial class Helpers
    {
        public static IEnumerable<GameObject> IterateOverChildGameObjects(
            this GameObject gameObject
        )
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                yield return gameObject.transform.GetChild(i).gameObject;
            }
        }

        public static IEnumerable<GameObject> IterateOverChildGameObjectsRecursively(
            this GameObject gameObject
        )
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject child = gameObject.transform.GetChild(i).gameObject;
                yield return child;
                foreach (GameObject go in child.IterateOverChildGameObjectsRecursively())
                {
                    yield return go;
                }
            }
        }

        public static IEnumerable<GameObject> IterateOverChildGameObjectsRecursivelyIncludingSelf(
            this GameObject gameObject
        )
        {
            yield return gameObject;

            for (int i = 0; i < gameObject.transform.childCount; ++i)
            {
                GameObject child = gameObject.transform.GetChild(i).gameObject;
                foreach (
                    GameObject c in child.IterateOverChildGameObjectsRecursivelyIncludingSelf()
                )
                {
                    yield return c;
                }
            }
        }

        public static IEnumerable<GameObject> IterateOverParentGameObjects(
            this GameObject gameObject
        )
        {
            Transform currentTransform = gameObject.transform.parent;
            while (currentTransform != null)
            {
                yield return currentTransform.gameObject;
                currentTransform = currentTransform.parent;
            }
        }

        public static IEnumerable<GameObject> IterateOverParentGameObjectsRecursivelyIncludingSelf(
            this GameObject gameObject
        )
        {
            yield return gameObject;

            foreach (GameObject parent in IterateOverParentGameObjects(gameObject))
            {
                yield return parent;
            }
        }

        public static IEnumerable<T> IterateOverAllChildComponentsRecursively<T>(
            this Component component
        )
        {
            if (component == null)
            {
                yield break;
            }

            foreach (T c in component.gameObject.GetComponents<T>())
            {
                yield return c;
            }

            for (int i = 0; i < component.transform.childCount; ++i)
            {
                Transform child = component.transform.GetChild(i);

                foreach (T c in child.IterateOverAllChildComponentsRecursively<T>())
                {
                    yield return c;
                }
            }
        }

        public static IEnumerable<Transform> IterateOverAllChildren(this Component component)
        {
            if (component == null)
            {
                yield break;
            }

            for (int i = 0; i < component.transform.childCount; ++i)
            {
                yield return component.transform.GetChild(i);
            }
        }

        public static IEnumerable<Transform> IterateOverAllParents(this Component component)
        {
            if (component == null)
            {
                yield break;
            }

            Transform transform = component.transform;
            while (transform.parent != null)
            {
                yield return transform.parent;
                transform = transform.parent;
            }
        }

        public static IEnumerable<Transform> IterateOverAllParentsIncludingSelf(
            this Component component
        )
        {
            if (component == null)
            {
                yield break;
            }

            Transform transform = component.transform;
            while (transform != null)
            {
                yield return transform;
                transform = transform.parent;
            }
        }

        public static IEnumerable<Transform> IterateOverAllChildrenRecursively(
            this Component component
        )
        {
            if (component == null)
            {
                yield break;
            }

            for (int i = 0; i < component.transform.childCount; ++i)
            {
                Transform childTransform = component.transform.GetChild(i);
                yield return childTransform;
                foreach (
                    Transform childChildTransform in childTransform.IterateOverAllChildrenRecursively()
                )
                {
                    yield return childChildTransform;
                }
            }
        }

        public static IEnumerable<Transform> IterateOverAllChildrenRecursivelyBreadthFirst(
            this Component component
        )
        {
            if (component == null)
            {
                yield break;
            }

            Queue<Transform> iteration = new();
            iteration.Enqueue(component.transform);
            while (iteration.TryDequeue(out Transform current))
            {
                for (int i = 0; i < current.childCount; ++i)
                {
                    Transform childTransform = current.GetChild(i);
                    iteration.Enqueue(childTransform);
                    yield return childTransform;
                }
            }
        }
    }
}
