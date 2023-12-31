#if !ENABLE_UBERLOGGING && (DEVELOPMENT_BUILD || DEBUG || UNITY_EDITOR)
#define ENABLE_UBERLOGGING
#endif

namespace UnityHelpers.Core.Extension
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Helper;
    using JetBrains.Annotations;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public static class LoggingExtensions
    {
        private static readonly Thread UnityMainThread;

        private static bool LoggingEnabled = true;

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

        public static void EnableLogging(this UnityEngine.Object component)
        {
            LoggingEnabled = true;
        }

        public static void DisableLogging(this UnityEngine.Object component)
        {
            LoggingEnabled = false;
        }

        
        [StringFormatMethod("message")]
        public static void Log(this UnityEngine.Object component, string message, params object[] args)
        {
#if ENABLE_UBERLOGGING
            if (!LoggingEnabled)
            {
                return;
            }
            LogDebug(component, message, args);
#endif
        }

        
        [StringFormatMethod("message")]
        public static void LogMethod(this UnityEngine.Object component)
        {
#if ENABLE_UBERLOGGING
            StackTrace stackTrace = new StackTrace();
            string methodName = stackTrace.GetFrame(1).GetMethod().Name;
            LogDebug(component, methodName);
#endif
        }

        
        [StringFormatMethod("message")]
        public static void LogDebug(this UnityEngine.Object component, string message, params object[] args)
        {
#if ENABLE_UBERLOGGING
            if (!LoggingEnabled)
            {
                return;
            }
            LogDebug(component, message, null, args);
#endif
        }

        
        [StringFormatMethod("message")]
        public static void LogWarn(this UnityEngine.Object component, string message, params object[] args)
        {
#if ENABLE_UBERLOGGING
            if (!LoggingEnabled)
            {
                return;
            }
            LogWarn(component, message, null, args);
#endif
        }

        
        [StringFormatMethod("message")]
        public static void LogError(this UnityEngine.Object component, string message, params object[] args)
        {
#if ENABLE_UBERLOGGING
            if (!LoggingEnabled)
            {
                return;
            }
            LogError(component, message, null, args);
#endif
        }

        
        [StringFormatMethod("message")]
        public static void Log(this UnityEngine.Object component, string message, Exception e, params object[] args)
        {
#if ENABLE_UBERLOGGING
            if (!LoggingEnabled)
            {
                return;
            }
            LogDebug(component, message, e, args);
#endif
        }

        
        [StringFormatMethod("message")]
        public static void LogDebug(this UnityEngine.Object component, string message, Exception e, params object[] args)
        {
#if ENABLE_UBERLOGGING
            if (!LoggingEnabled)
            {
                return;
            }
            if (Equals(Thread.CurrentThread, UnityMainThread))
            {
                Debug.Log(Wrap(component, string.Format(message, args), e));
            }
#endif
        }

        
        [StringFormatMethod("message")]
        public static void LogWarn(this UnityEngine.Object component, string message, Exception e, params object[] args)
        {
#if ENABLE_UBERLOGGING
            if (!LoggingEnabled)
            {
                return;
            }
            if (Equals(Thread.CurrentThread, UnityMainThread))
            {
                Debug.LogWarning(Wrap(component, string.Format(message, args), e));
            }
#endif
        }

        
        [StringFormatMethod("message")]
        public static void LogError(this UnityEngine.Object component, string message, Exception e, params object[] args)
        {
#if ENABLE_UBERLOGGING
            if (!LoggingEnabled)
            {
                return;
            }
            if (Equals(Thread.CurrentThread, UnityMainThread))
            {
                Debug.LogError(Wrap(component, string.Format(message, args), e));
            }
#endif
        }

        private static string Wrap(UnityEngine.Object component, string message, Exception e)
        {
#if ENABLE_UBERLOGGING
            float now = Time.time;
            string componentType = component == null ? "NO_TYPE" : component.GetType().Name;
            string gameObjectName = "NO_NAME";
            if (component != null)
            {
                GameObject owner = component.GetGameObject();
                if (owner != null)
                {
                    gameObjectName = owner.name;
                }
            }

            string prepend = string.Format("{0}|{2}[{1}]|", now, componentType, gameObjectName);
            if (e != null)
            {
                return prepend + message + "\n    " + e;
            }

            return prepend + message;
#else
            return string.Empty;
#endif
        }
    }
}
