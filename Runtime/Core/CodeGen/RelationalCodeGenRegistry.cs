namespace WallstopStudios.UnityHelpers.Core.CodeGen
{
    using System;
    using System.Collections.Concurrent;
    using UnityEngine;

    /// <summary>
    /// Holds generated handlers for relational component assignments.
    /// </summary>
    public readonly struct RelationalGeneratedHandlers
    {
        public RelationalGeneratedHandlers(
            Func<Component, bool>? sibling,
            Func<Component, bool>? parent,
            Func<Component, bool>? child,
            string[]? siblingFields,
            string[]? parentFields,
            string[]? childFields
        )
        {
            Sibling = sibling;
            Parent = parent;
            Child = child;
            SiblingFields = siblingFields;
            ParentFields = parentFields;
            ChildFields = childFields;
        }

        public Func<Component, bool>? Sibling { get; }

        public Func<Component, bool>? Parent { get; }

        public Func<Component, bool>? Child { get; }

        public string[]? SiblingFields { get; }

        public string[]? ParentFields { get; }

        public string[]? ChildFields { get; }
    }

    /// <summary>
    /// Registry populated by generated code to provide fast-path relational assignments.
    /// </summary>
    public static class RelationalCodeGenRegistry
    {
        private static readonly ConcurrentDictionary<Type, RelationalGeneratedHandlers> Handlers =
            new();

        public static void Register(Type componentType, RelationalGeneratedHandlers handlers)
        {
            Handlers[componentType] = handlers;
        }

        public static bool TryAssignSibling(Component component)
        {
            if (component == null)
            {
                return false;
            }

            if (!TryGetHandlers(component.GetType(), out RelationalGeneratedHandlers handlers))
            {
                return false;
            }

            Func<Component, bool>? handler = handlers.Sibling;
            if (handler == null)
            {
                return false;
            }

            return handler(component);
        }

        public static bool TryGetHandlers(
            Type componentType,
            out RelationalGeneratedHandlers handlers
        )
        {
            if (componentType == null)
            {
                handlers = default;
                return false;
            }

            Type? current = componentType;
            while (current != null)
            {
                if (Handlers.TryGetValue(current, out handlers))
                {
                    return true;
                }

                current = current.BaseType;
            }

            handlers = default;
            return false;
        }
    }
}
