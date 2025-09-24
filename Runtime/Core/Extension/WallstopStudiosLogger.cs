#if !ENABLE_UBERLOGGING && (DEVELOPMENT_BUILD || DEBUG || UNITY_EDITOR)
#define ENABLE_UBERLOGGING
#endif

namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Helper;
    using Helper.Logging;
    using UnityEngine;
    using Utils;
    using Object = UnityEngine.Object;

    public static class WallstopStudiosLogger
    {
        public static readonly UnityLogTagFormatter LogInstance = new(
            createDefaultDecorators: true
        );

        private static Thread UnityMainThread;
        private const int LogsPerCacheClean = 5;

        private static bool LoggingEnabled = true;
        private static long _cacheAccessCount;

        private static readonly HashSet<Object> Disabled = new();
        private static readonly Dictionary<Type, (string, Func<object, object>)[]> MetadataCache =
            new();

        private static readonly Dictionary<string, object> GenericObject = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeMainThread()
        {
            UnityMainThread = Thread.CurrentThread;
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

        [HideInCallstack]
        public static string GenericToString(this Object component)
        {
            if (component == null)
            {
                return "null";
            }

            (string, Func<object, object>)[] metadataAccess = MetadataCache.GetOrAdd(
                component.GetType(),
                inType =>
                    inType
                        .GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Select(field => (field.Name, ReflectionHelpers.GetFieldGetter(field)))
                        .Concat(
                            inType
                                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Select(property =>
                                    (property.Name, ReflectionHelpers.GetPropertyGetter(property))
                                )
                        )
                        .ToArray()
            );

            GenericObject.Clear();
            foreach ((string name, Func<object, object> access) in metadataAccess)
            {
                try
                {
                    string valueFormat = ValueFormat(access(component));
                    if (valueFormat != null)
                    {
                        GenericObject[name] = valueFormat;
                    }
                }
                catch
                {
                    // Skip
                }
            }

            return GenericObject.ToJson();
        }

        [HideInCallstack]
        private static string ValueFormat(object value)
        {
            if (value is Object obj)
            {
                return obj != null ? obj.name : "null";
            }
            return value?.ToString();
        }

        [HideInCallstack]
        public static void Log(
            this Object component,
            FormattableString message,
            Exception e = null,
            bool pretty = true
        )
        {
#if ENABLE_UBERLOGGING || DEBUG_LOGGING
            LogDebug(component, message, e, pretty);
#endif
        }

        [HideInCallstack]
        public static void LogDebug(
            this Object component,
            FormattableString message,
            Exception e = null,
            bool pretty = true
        )
        {
#if ENABLE_UBERLOGGING || DEBUG_LOGGING
            if (LoggingAllowed(component))
            {
                if (Equals(Thread.CurrentThread, UnityMainThread))
                {
                    LogInstance.Log(message, component, e, pretty);
                }
                else
                {
                    FormattableString localMessage = message;
                    Object localComponent = component;
                    Exception localE = e;
                    bool localPretty = pretty;
                    UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
                        LogInstance.Log(localMessage, localComponent, localE, localPretty)
                    );
                }
            }
#endif
        }

        [HideInCallstack]
        public static void LogWarn(
            this Object component,
            FormattableString message,
            Exception e = null,
            bool pretty = true
        )
        {
#if ENABLE_UBERLOGGING || WARN_LOGGING
            if (LoggingAllowed(component))
            {
                if (Equals(Thread.CurrentThread, UnityMainThread))
                {
                    LogInstance.LogWarn(message, component, e, pretty);
                }
                else
                {
                    FormattableString localMessage = message;
                    Object localComponent = component;
                    Exception localE = e;
                    bool localPretty = pretty;
                    UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
                        LogInstance.LogWarn(localMessage, localComponent, localE, localPretty)
                    );
                }
            }
#endif
        }

        [HideInCallstack]
        public static void LogError(
            this Object component,
            FormattableString message,
            Exception e = null,
            bool pretty = true
        )
        {
#if ENABLE_UBERLOGGING || ERROR_LOGGING
            if (LoggingAllowed(component))
            {
                if (Equals(Thread.CurrentThread, UnityMainThread))
                {
                    LogInstance.LogError(message, component, e, pretty);
                }
                else
                {
                    FormattableString localMessage = message;
                    Object localComponent = component;
                    Exception localE = e;
                    bool localPretty = pretty;
                    UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
                        LogInstance.LogError(localMessage, localComponent, localE, localPretty)
                    );
                }
            }
#endif
        }

        [HideInCallstack]
        private static bool LoggingAllowed(Object component)
        {
            if (Interlocked.Increment(ref _cacheAccessCount) % LogsPerCacheClean == 0)
            {
                using PooledResource<List<Object>> bufferResource = Buffers<Object>.List.Get();
                List<Object> buffer = bufferResource.resource;
                foreach (Object disabled in Disabled)
                {
                    buffer.Add(disabled);
                }

                foreach (Object disabled in buffer)
                {
                    if (disabled == null)
                    {
                        Disabled.Remove(disabled);
                    }
                }
            }

            return LoggingEnabled && !Disabled.Contains(component);
        }
    }
}
