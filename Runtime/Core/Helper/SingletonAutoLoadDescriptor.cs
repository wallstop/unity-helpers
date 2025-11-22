namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    internal enum SingletonAutoLoadKind : byte
    {
        Runtime = 0,
        ScriptableObject = 1,
    }

    internal readonly struct SingletonAutoLoadDescriptor
    {
        private readonly Action _loader;

        private SingletonAutoLoadDescriptor(
            SingletonAutoLoadKind kind,
            Action loader,
            string typeName,
            RuntimeInitializeLoadType loadType
        )
        {
            Kind = kind;
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            TypeName = typeName ?? "UnknownSingleton";
            LoadType = loadType;
        }

        internal SingletonAutoLoadKind Kind { get; }

        internal string TypeName { get; }

        internal RuntimeInitializeLoadType LoadType { get; }

        internal void Invoke()
        {
            _loader();
        }

        internal static SingletonAutoLoadDescriptor Runtime<T>(RuntimeInitializeLoadType loadType)
            where T : RuntimeSingleton<T>
        {
            return new SingletonAutoLoadDescriptor(
                SingletonAutoLoadKind.Runtime,
                () =>
                {
                    _ = RuntimeSingleton<T>.Instance;
                },
                typeof(T).FullName,
                loadType
            );
        }

        internal static SingletonAutoLoadDescriptor ScriptableObject<T>(
            RuntimeInitializeLoadType loadType
        )
            where T : ScriptableObjectSingleton<T>
        {
            return new SingletonAutoLoadDescriptor(
                SingletonAutoLoadKind.ScriptableObject,
                () =>
                {
                    _ = ScriptableObjectSingleton<T>.Instance;
                },
                typeof(T).FullName,
                loadType
            );
        }
    }
}
