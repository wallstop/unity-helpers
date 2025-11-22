namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Marks a RuntimeSingleton or ScriptableObjectSingleton to be auto-loaded during startup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AutoLoadSingletonAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoLoadSingletonAttribute"/> class.
        /// </summary>
        /// <param name="loadType">
        /// Unity load phase that should trigger instantiation. Defaults to <see cref="RuntimeInitializeLoadType.BeforeSplashScreen"/>.
        /// </param>
        public AutoLoadSingletonAttribute(
            RuntimeInitializeLoadType loadType = RuntimeInitializeLoadType.BeforeSplashScreen
        )
        {
            LoadType = loadType;
        }

        /// <summary>
        /// Gets the Unity load phase that should trigger instantiation.
        /// </summary>
        public RuntimeInitializeLoadType LoadType { get; }
    }
}
