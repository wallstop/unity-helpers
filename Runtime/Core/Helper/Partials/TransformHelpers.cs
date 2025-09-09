namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;
    using UnityEngine;
    using Utils;

    public static partial class Helpers
    {
        public static List<T> IterateOverAllParentComponentsRecursively<T>(
            this Component component,
            List<T> buffer,
            bool includeSelf = false
        )
        {
            buffer.Clear();
            if (component == null)
            {
                return buffer;
            }

            List<T> internalBuffer = Buffers<T>.List;
            if (includeSelf)
            {
                component.GetComponents(internalBuffer);
                foreach (T c in internalBuffer)
                {
                    buffer.Add(c);
                }
            }

            Transform parent = component.transform.parent;
            while (parent != null)
            {
                parent.GetComponents(internalBuffer);
                foreach (T c in internalBuffer)
                {
                    buffer.Add(c);
                }
                parent = parent.parent;
            }

            return buffer;
        }

        public static IEnumerable<T> IterateOverAllParentComponentsRecursively<T>(
            this Component component,
            bool includeSelf = false
        )
        {
            if (component == null)
            {
                yield break;
            }

            List<T> buffer = new();
            foreach (Transform parent in IterateOverAllParents(component, includeSelf))
            {
                parent.GetComponents(buffer);
                foreach (T c in buffer)
                {
                    yield return c;
                }
            }
        }

        public static List<T> IterateOverAllChildComponentsRecursively<T>(
            this Component component,
            List<T> buffer,
            bool includeSelf = false
        )
        {
            buffer.Clear();
            if (component == null)
            {
                return buffer;
            }

            List<T> internalBuffer = Buffers<T>.List;
            if (includeSelf)
            {
                component.GetComponents(internalBuffer);
                foreach (T c in internalBuffer)
                {
                    buffer.Add(c);
                }
            }

            Transform transform = component.transform;
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                child.GetComponentsInChildren(true, internalBuffer);
                foreach (T c in internalBuffer)
                {
                    buffer.Add(c);
                }
            }

            return buffer;
        }

        public static IEnumerable<T> IterateOverAllChildComponentsRecursively<T>(
            this Component component,
            bool includeSelf = false
        )
        {
            if (component == null)
            {
                yield break;
            }

            List<T> buffer = new();
            if (includeSelf)
            {
                component.GetComponents(buffer);
                foreach (T c in buffer)
                {
                    yield return c;
                }
            }

            Transform transform = component.transform;
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                child.GetComponentsInChildren(true, buffer);
                foreach (T c in buffer)
                {
                    yield return c;
                }
            }
        }

        public static IEnumerable<Transform> IterateOverAllChildren(
            this Component component,
            bool includeSelf = false
        )
        {
            if (component == null)
            {
                yield break;
            }

            Transform transform = component.transform;
            if (includeSelf)
            {
                yield return transform;
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                yield return transform.GetChild(i);
            }
        }

        public static List<Transform> IterateOverAllChildren(
            this Component component,
            List<Transform> buffer,
            bool includeSelf = false
        )
        {
            buffer.Clear();
            if (component == null)
            {
                return buffer;
            }

            Transform transform = component.transform;
            if (includeSelf)
            {
                buffer.Add(transform);
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                buffer.Add(transform.GetChild(i));
            }

            return buffer;
        }

        public static IEnumerable<Transform> IterateOverAllParents(
            this Component component,
            bool includeSelf = false
        )
        {
            if (component == null)
            {
                yield break;
            }

            Transform transform = component.transform;
            if (includeSelf)
            {
                yield return transform;
            }

            Transform parent = transform.parent;
            while (parent != null)
            {
                yield return parent;
                parent = parent.parent;
            }
        }

        public static List<Transform> IterateOverAllParents(
            this Component component,
            List<Transform> buffer,
            bool includeSelf = false
        )
        {
            buffer.Clear();
            if (component == null)
            {
                return buffer;
            }

            Transform transform = component.transform;
            if (includeSelf)
            {
                buffer.Add(transform);
            }

            Transform parent = transform.parent;
            while (parent != null)
            {
                buffer.Add(parent);
                parent = parent.parent;
            }

            return buffer;
        }

        public static IEnumerable<Transform> IterateOverAllChildrenRecursively(
            this Component component,
            bool includeSelf = false
        )
        {
            if (component == null)
            {
                yield break;
            }

            Transform transform = component.transform;
            if (includeSelf)
            {
                yield return transform;
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                foreach (
                    Transform child in IterateOverAllChildrenRecursively(
                        transform.GetChild(i),
                        includeSelf: true
                    )
                )
                {
                    yield return child;
                }
            }
        }

        public static List<Transform> IterateOverAllChildrenRecursively(
            this Component component,
            List<Transform> buffer,
            bool includeSelf = false
        )
        {
            buffer.Clear();
            if (component == null)
            {
                return buffer;
            }

            Transform transform = component.transform;
            if (includeSelf)
            {
                buffer.Add(transform);
            }

            return InternalIterateOverAllChildrenRecursively(transform, buffer);
        }

        private static List<Transform> InternalIterateOverAllChildrenRecursively(
            this Transform transform,
            List<Transform> buffer
        )
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                buffer.Add(child);
                InternalIterateOverAllChildrenRecursively(child, buffer);
            }

            return buffer;
        }

        public static IEnumerable<Transform> IterateOverAllChildrenRecursivelyBreadthFirst(
            this Component component,
            bool includeSelf = false
        )
        {
            if (component == null)
            {
                yield break;
            }

            Transform transform = component.transform;
            if (includeSelf)
            {
                yield return transform;
            }

            Queue<Transform> iteration = new();
            iteration.Enqueue(transform);
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

        public static List<Transform> IterateOverAllChildrenRecursivelyBreadthFirst(
            this Component component,
            List<Transform> buffer,
            bool includeSelf = false
        )
        {
            buffer.Clear();
            if (component == null)
            {
                return buffer;
            }

            Transform transform = component.transform;
            if (includeSelf)
            {
                buffer.Add(transform);
            }

            Queue<Transform> iteration = Buffers<Transform>.Queue;
            iteration.Clear();
            iteration.Enqueue(transform);
            while (iteration.TryDequeue(out Transform current))
            {
                for (int i = 0; i < current.childCount; ++i)
                {
                    Transform childTransform = current.GetChild(i);
                    iteration.Enqueue(childTransform);
                    buffer.Add(childTransform);
                }
            }
            return buffer;
        }
    }
}
