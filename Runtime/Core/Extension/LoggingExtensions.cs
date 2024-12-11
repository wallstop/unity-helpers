#if !ENABLE_UBERLOGGING && (DEVELOPMENT_BUILD || DEBUG || UNITY_EDITOR)
#define ENABLE_UBERLOGGING
#endif

namespace UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using JetBrains.Annotations;
    using UnityEngine;
    using Utils;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;

    public static class LoggingExtensions
    {
        private static readonly Thread UnityMainThread;
        private const int LogsPerCacheClean = 5;

        private static bool LoggingEnabled = true;
        private static long _cacheAccessCount;

        private static readonly HashSet<Object> Disabled = new();
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();
        private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new();

        static LoggingExtensions()
        {
#if ENABLE_UBERLOGGING
            /*
                Unity throws exceptions if you try to log on something that isn't the main thread.
                Sometimes, it's nice to log in async Tasks. Assume that the first initialization of
                this class will be done by the Unity main thread, and then check every time we log.
                If the logging thread is not the unity main thread, then do nothing
                (instead of throwing...)
             */
            UnityMainThread = Thread.CurrentThread;
#endif
        }

        public static void GlobalEnableLogging(this Object component)
        {
            LoggingEnabled = true;
        }

        public static void GlobalDisableLogging(this Object component)
        {
            LoggingEnabled = false;
        }

        public static void EnableLogging(this Object component)
        {
            Disabled.Remove(component);
        }

        public static void DisableLogging(this Object component)
        {
            Disabled.Add(component);
        }

        public static string GenericToString(this Object component)
        {
            if (component == null)
            {
                return "null";
            }

            Dictionary<string, object> structure = new();
            Type type = component.GetType();
            FieldInfo[] fields = FieldCache.GetOrAdd(
                type,
                inType => inType.GetFields(BindingFlags.Public | BindingFlags.Instance)
            );
            PropertyInfo[] properties = PropertyCache.GetOrAdd(
                type,
                inType => inType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            );
            foreach (FieldInfo field in fields)
            {
                structure[field.Name] = ValueFormat(field.GetValue(component));
            }

            foreach (PropertyInfo property in properties)
            {
                structure[property.Name] = ValueFormat(property.GetValue(component));
            }

            return structure.ToJson();

            object ValueFormat(object value)
            {
                if (value is Object obj && obj != null)
                {
                    return obj.name;
                }
                return value?.ToString();
            }
        }

        [StringFormatMethod("message")]
        public static void Log(this Object component, string message, params object[] args)
        {
#if ENABLE_UBERLOGGING
            LogDebug(component, message, args);
#endif
        }

        [StringFormatMethod("message")]
        public static void LogMethod(this Object component, [CallerMemberName] string caller = "")
        {
#if ENABLE_UBERLOGGING
            LogDebug(component, caller);
#endif
        }

        [StringFormatMethod("message")]
        public static void LogDebug(this Object component, string message, params object[] args)
        {
#if ENABLE_UBERLOGGING
            LogDebug(component, message, null, args);
#endif
        }

        [StringFormatMethod("message")]
        public static void LogWarn(this Object component, string message, params object[] args)
        {
#if ENABLE_UBERLOGGING
            LogWarn(component, message, null, args);
#endif
        }

        [StringFormatMethod("message")]
        public static void LogError(this Object component, string message, params object[] args)
        {
#if ENABLE_UBERLOGGING
            LogError(component, message, null, args);
#endif
        }

        [StringFormatMethod("message")]
        public static void Log(
            this Object component,
            string message,
            Exception e,
            params object[] args
        )
        {
#if ENABLE_UBERLOGGING
            LogDebug(component, message, e, args);
#endif
        }

        [StringFormatMethod("message")]
        public static void LogDebug(
            this Object component,
            string message,
            Exception e,
            params object[] args
        )
        {
#if ENABLE_UBERLOGGING
            if (LoggingAllowed(component))
            {
                Debug.Log(
                    Wrap(component, args.Length != 0 ? string.Format(message, args) : message, e),
                    component
                );
            }
#endif
        }

        [StringFormatMethod("message")]
        public static void LogWarn(
            this Object component,
            string message,
            Exception e,
            params object[] args
        )
        {
#if ENABLE_UBERLOGGING
            if (LoggingAllowed(component))
            {
                Debug.LogWarning(
                    Wrap(component, args.Length != 0 ? string.Format(message, args) : message, e),
                    component
                );
            }
#endif
        }

        [StringFormatMethod("message")]
        public static void LogError(
            this Object component,
            string message,
            Exception e,
            params object[] args
        )
        {
#if ENABLE_UBERLOGGING
            if (LoggingAllowed(component))
            {
                Debug.LogError(
                    Wrap(component, args.Length != 0 ? string.Format(message, args) : message, e),
                    component
                );
            }
#endif
        }

        private static bool LoggingAllowed(Object component)
        {
            if (Interlocked.Increment(ref _cacheAccessCount) % LogsPerCacheClean == 0)
            {
                List<Object> buffer = Buffers<Object>.List;
                buffer.Clear();
                buffer.AddRange(Disabled);
                buffer.RemoveAll(element => element != null);
                if (0 < buffer.Count)
                {
                    Disabled.ExceptWith(buffer);
                }
            }

            return LoggingEnabled
                && Equals(Thread.CurrentThread, UnityMainThread)
                && !Disabled.Contains(component);
        }

        private static string Wrap(Object component, string message, Exception e)
        {
#if ENABLE_UBERLOGGING
            float now = Time.time;
            string componentType;
            string gameObjectName;
            if (component != null)
            {
                componentType = component.GetType().Name;
                gameObjectName = component.name;
            }
            else
            {
                componentType = "NO_TYPE";
                gameObjectName = "NO_NAME";
            }

            return e != null
                ? $"{now}|{gameObjectName}[{componentType}]|{message}\n    {e}"
                : $"{now}|{gameObjectName}[{componentType}]|{message}";

#else
            return string.Empty;
#endif
        }
    }
}
