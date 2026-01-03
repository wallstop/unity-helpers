// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;

    /// <summary>
    /// Global and type-specific configuration registry for intelligent pool purging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a hierarchical configuration system for pool purging behavior:
    /// <list type="number">
    ///   <item><description>Specific type configuration (e.g., <c>List&lt;int&gt;</c>)</description></item>
    ///   <item><description>Generic type pattern configuration (e.g., <c>List&lt;&gt;</c> for any <c>List&lt;T&gt;</c>)</description></item>
    ///   <item><description>Global defaults</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// By default, intelligent purging is <strong>disabled</strong> (opt-in). When enabled,
    /// conservative settings are used to prioritize avoiding GC churn over memory savings.
    /// </para>
    /// <example>
    /// <code><![CDATA[
    /// // Enable globally with default conservative settings
    /// PoolPurgeSettings.GlobalEnabled = true;
    ///
    /// // Configure type-specific settings
    /// PoolPurgeSettings.Configure<List<int>>(options => {
    ///     options.Enabled = true;
    ///     options.IdleTimeoutSeconds = 600f; // 10 minutes
    ///     options.MinRetainCount = 10;
    /// });
    ///
    /// // Configure all List<T> types
    /// PoolPurgeSettings.ConfigureGeneric(typeof(List<>), options => {
    ///     options.IdleTimeoutSeconds = 300f;
    /// });
    ///
    /// // Disable for expensive objects
    /// PoolPurgeSettings.Disable<ExpensiveObject>();
    /// ]]></code>
    /// </example>
    /// </remarks>
    public static class PoolPurgeSettings
    {
        /// <summary>
        /// Default idle timeout in seconds when intelligent purging is enabled.
        /// Set to 5 minutes by default to be conservative and avoid GC churn.
        /// </summary>
        public const float DefaultIdleTimeoutSeconds = 300f;

        /// <summary>
        /// Default minimum retain count during purge operations.
        /// </summary>
        public const int DefaultMinRetainCount = 0;

        /// <summary>
        /// Default buffer multiplier for comfortable pool size calculation.
        /// The comfortable size is calculated as <c>max(MinRetainCount, rollingHighWaterMark * BufferMultiplier)</c>.
        /// </summary>
        public const float DefaultBufferMultiplier = 1.5f;

        /// <summary>
        /// Default rolling window duration in seconds for high water mark tracking.
        /// </summary>
        public const float DefaultRollingWindowSeconds = 300f;

        /// <summary>
        /// Default hysteresis duration in seconds.
        /// After a usage spike, purging is suppressed for this duration to prevent purge-allocate cycles.
        /// </summary>
        public const float DefaultHysteresisSeconds = 60f;

        /// <summary>
        /// Default spike threshold multiplier.
        /// A spike is detected when concurrent rentals exceed the rolling average by this factor.
        /// </summary>
        public const float DefaultSpikeThresholdMultiplier = 2.0f;

        private static int _globalEnabled;
        private static float _defaultIdleTimeoutSeconds = DefaultIdleTimeoutSeconds;
        private static int _defaultMinRetainCount = DefaultMinRetainCount;
        private static float _defaultBufferMultiplier = DefaultBufferMultiplier;
        private static float _defaultRollingWindowSeconds = DefaultRollingWindowSeconds;
        private static float _defaultHysteresisSeconds = DefaultHysteresisSeconds;
        private static float _defaultSpikeThresholdMultiplier = DefaultSpikeThresholdMultiplier;

        private static readonly object ConfigLock = new();
        private static readonly Dictionary<Type, PoolPurgeTypeOptions> TypeConfigurations = new();
        private static readonly Dictionary<Type, PoolPurgeTypeOptions> GenericTypeConfigurations =
            new();
        private static readonly HashSet<Type> DisabledTypes = new();

        // Settings-based per-type configurations (lower priority than programmatic API)
        private static readonly Dictionary<Type, PoolPurgeTypeOptions> SettingsTypeConfigurations =
            new();
        private static readonly Dictionary<
            Type,
            PoolPurgeTypeOptions
        > SettingsGenericTypeConfigurations = new();
        private static readonly HashSet<Type> SettingsDisabledTypes = new();

        /// <summary>
        /// Gets or sets whether intelligent pool purging is globally enabled.
        /// Default is <c>false</c> (disabled) to maintain backward compatibility.
        /// </summary>
        public static bool GlobalEnabled
        {
            get => Volatile.Read(ref _globalEnabled) != 0;
            set => Volatile.Write(ref _globalEnabled, value ? 1 : 0);
        }

        /// <summary>
        /// Gets or sets the default idle timeout in seconds for pools without type-specific configuration.
        /// Items idle longer than this are eligible for purging when intelligent purging is enabled.
        /// </summary>
        public static float DefaultGlobalIdleTimeoutSeconds
        {
            get => Volatile.Read(ref _defaultIdleTimeoutSeconds);
            set => Volatile.Write(ref _defaultIdleTimeoutSeconds, value);
        }

        /// <summary>
        /// Gets or sets the default minimum retain count for pools without type-specific configuration.
        /// Purge operations will never reduce the pool below this count.
        /// </summary>
        public static int DefaultGlobalMinRetainCount
        {
            get => Volatile.Read(ref _defaultMinRetainCount);
            set => Volatile.Write(ref _defaultMinRetainCount, value);
        }

        /// <summary>
        /// Gets or sets the default buffer multiplier for comfortable pool size calculation.
        /// </summary>
        public static float DefaultGlobalBufferMultiplier
        {
            get => Volatile.Read(ref _defaultBufferMultiplier);
            set => Volatile.Write(ref _defaultBufferMultiplier, value);
        }

        /// <summary>
        /// Gets or sets the default rolling window duration in seconds for high water mark tracking.
        /// </summary>
        public static float DefaultGlobalRollingWindowSeconds
        {
            get => Volatile.Read(ref _defaultRollingWindowSeconds);
            set => Volatile.Write(ref _defaultRollingWindowSeconds, value);
        }

        /// <summary>
        /// Gets or sets the default hysteresis duration in seconds.
        /// </summary>
        public static float DefaultGlobalHysteresisSeconds
        {
            get => Volatile.Read(ref _defaultHysteresisSeconds);
            set => Volatile.Write(ref _defaultHysteresisSeconds, value);
        }

        /// <summary>
        /// Gets or sets the default spike threshold multiplier.
        /// </summary>
        public static float DefaultGlobalSpikeThresholdMultiplier
        {
            get => Volatile.Read(ref _defaultSpikeThresholdMultiplier);
            set => Volatile.Write(ref _defaultSpikeThresholdMultiplier, value);
        }

        /// <summary>
        /// Configures intelligent purging options for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to configure.</typeparam>
        /// <param name="configure">Action to configure the options.</param>
        /// <exception cref="ArgumentNullException">Thrown when configure is null.</exception>
        public static void Configure<T>(Action<PoolPurgeTypeOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            Type type = typeof(T);
            PoolPurgeTypeOptions options = new();
            configure(options);

            lock (ConfigLock)
            {
                TypeConfigurations[type] = options;
                DisabledTypes.Remove(type);
            }
        }

        /// <summary>
        /// Configures intelligent purging options for all types matching a generic type definition.
        /// </summary>
        /// <param name="genericTypeDefinition">The generic type definition (e.g., <c>typeof(List&lt;&gt;)</c>).</param>
        /// <param name="configure">Action to configure the options.</param>
        /// <exception cref="ArgumentNullException">Thrown when genericTypeDefinition or configure is null.</exception>
        /// <exception cref="ArgumentException">Thrown when genericTypeDefinition is not a generic type definition.</exception>
        public static void ConfigureGeneric(
            Type genericTypeDefinition,
            Action<PoolPurgeTypeOptions> configure
        )
        {
            if (genericTypeDefinition == null)
            {
                throw new ArgumentNullException(nameof(genericTypeDefinition));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (!genericTypeDefinition.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    "Type must be a generic type definition (e.g., typeof(List<>)).",
                    nameof(genericTypeDefinition)
                );
            }

            PoolPurgeTypeOptions options = new();
            configure(options);

            lock (ConfigLock)
            {
                GenericTypeConfigurations[genericTypeDefinition] = options;
            }
        }

        /// <summary>
        /// Disables intelligent purging for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to disable purging for.</typeparam>
        public static void Disable<T>()
        {
            Type type = typeof(T);
            lock (ConfigLock)
            {
                DisabledTypes.Add(type);
                TypeConfigurations.Remove(type);
            }
        }

        /// <summary>
        /// Disables intelligent purging for a specific type.
        /// </summary>
        /// <param name="type">The type to disable purging for.</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        public static void Disable(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            lock (ConfigLock)
            {
                DisabledTypes.Add(type);
                TypeConfigurations.Remove(type);
            }
        }

        /// <summary>
        /// Enables intelligent purging for a specific type that was previously disabled.
        /// </summary>
        /// <typeparam name="T">The type to enable purging for.</typeparam>
        public static void Enable<T>()
        {
            Type type = typeof(T);
            lock (ConfigLock)
            {
                DisabledTypes.Remove(type);
            }
        }

        /// <summary>
        /// Enables intelligent purging for a specific type that was previously disabled.
        /// </summary>
        /// <param name="type">The type to enable purging for.</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        public static void Enable(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            lock (ConfigLock)
            {
                DisabledTypes.Remove(type);
            }
        }

        /// <summary>
        /// Removes all type-specific and generic type configurations.
        /// Does not affect global settings.
        /// </summary>
        public static void ClearTypeConfigurations()
        {
            lock (ConfigLock)
            {
                TypeConfigurations.Clear();
                GenericTypeConfigurations.Clear();
                DisabledTypes.Clear();
            }
        }

        /// <summary>
        /// Clears all settings-based type configurations.
        /// This is typically called before reloading configurations from UnityHelpersSettings.
        /// </summary>
        public static void ClearSettingsTypeConfigurations()
        {
            lock (ConfigLock)
            {
                SettingsTypeConfigurations.Clear();
                SettingsGenericTypeConfigurations.Clear();
                SettingsDisabledTypes.Clear();
            }
        }

        /// <summary>
        /// Configures settings-based per-type options.
        /// These have lower priority than programmatic API configurations.
        /// </summary>
        /// <param name="type">The type to configure.</param>
        /// <param name="options">The configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when type or options is null.</exception>
        public static void ConfigureFromSettings(Type type, PoolPurgeTypeOptions options)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            lock (ConfigLock)
            {
                SettingsTypeConfigurations[type] = options;
                SettingsDisabledTypes.Remove(type);
            }
        }

        /// <summary>
        /// Configures settings-based per-type options for a generic type definition.
        /// These have lower priority than programmatic API configurations.
        /// </summary>
        /// <param name="genericTypeDefinition">The generic type definition (e.g., typeof(List&lt;&gt;)).</param>
        /// <param name="options">The configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when genericTypeDefinition or options is null.</exception>
        /// <exception cref="ArgumentException">Thrown when genericTypeDefinition is not a generic type definition.</exception>
        public static void ConfigureGenericFromSettings(
            Type genericTypeDefinition,
            PoolPurgeTypeOptions options
        )
        {
            if (genericTypeDefinition == null)
            {
                throw new ArgumentNullException(nameof(genericTypeDefinition));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!genericTypeDefinition.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    "Type must be a generic type definition (e.g., typeof(List<>)).",
                    nameof(genericTypeDefinition)
                );
            }

            lock (ConfigLock)
            {
                SettingsGenericTypeConfigurations[genericTypeDefinition] = options;
            }
        }

        /// <summary>
        /// Disables a type from settings-based configuration.
        /// </summary>
        /// <param name="type">The type to disable.</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        public static void DisableFromSettings(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            lock (ConfigLock)
            {
                SettingsDisabledTypes.Add(type);
                SettingsTypeConfigurations.Remove(type);
            }
        }

        /// <summary>
        /// Resets all settings to their default values.
        /// </summary>
        public static void ResetToDefaults()
        {
            GlobalEnabled = false;
            DefaultGlobalIdleTimeoutSeconds = DefaultIdleTimeoutSeconds;
            DefaultGlobalMinRetainCount = DefaultMinRetainCount;
            DefaultGlobalBufferMultiplier = DefaultBufferMultiplier;
            DefaultGlobalRollingWindowSeconds = DefaultRollingWindowSeconds;
            DefaultGlobalHysteresisSeconds = DefaultHysteresisSeconds;
            DefaultGlobalSpikeThresholdMultiplier = DefaultSpikeThresholdMultiplier;
            ClearTypeConfigurations();
            ClearSettingsTypeConfigurations();
        }

        /// <summary>
        /// Gets the effective configuration for a specific type, considering the hierarchy:
        /// specific type > generic type pattern > global defaults.
        /// </summary>
        /// <typeparam name="T">The type to get configuration for.</typeparam>
        /// <returns>The effective purge configuration for the type.</returns>
        public static PoolPurgeEffectiveOptions GetEffectiveOptions<T>()
        {
            return GetEffectiveOptions(typeof(T));
        }

        /// <summary>
        /// Gets the effective configuration for a specific type, considering the hierarchy:
        /// <list type="number">
        ///   <item><description>Programmatic API (Configure, Disable) - highest priority</description></item>
        ///   <item><description>UnityHelpersSettings per-type configuration</description></item>
        ///   <item><description>PoolPurgePolicyAttribute on the type</description></item>
        ///   <item><description>Generic type patterns by specificity (exact > inner open > outer open)</description></item>
        ///   <item><description>UnityHelpersSettings global defaults</description></item>
        ///   <item><description>Hardcoded defaults - lowest priority</description></item>
        /// </list>
        /// </summary>
        /// <param name="type">The type to get configuration for.</param>
        /// <returns>The effective purge configuration for the type.</returns>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        /// <remarks>
        /// <para>
        /// For generic types like <c>List&lt;List&lt;int&gt;&gt;</c>, patterns are matched in order of specificity:
        /// <list type="number">
        ///   <item><description><c>List&lt;List&lt;int&gt;&gt;</c> - exact match</description></item>
        ///   <item><description><c>List&lt;List&lt;&gt;&gt;</c> - inner generic open</description></item>
        ///   <item><description><c>List&lt;&gt;</c> - outer generic open</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public static PoolPurgeEffectiveOptions GetEffectiveOptions(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            bool globalEnabled = GlobalEnabled;
            bool typeDisabled;
            bool settingsTypeDisabled;
            PoolPurgeTypeOptions typeOptions = null;
            PoolPurgeTypeOptions settingsTypeOptions = null;
            PoolPurgeTypeOptions bestProgrammaticGenericOptions = null;
            PoolPurgeTypeOptions bestSettingsGenericOptions = null;
            int bestProgrammaticGenericPriority = int.MaxValue;
            int bestSettingsGenericPriority = int.MaxValue;

            lock (ConfigLock)
            {
                // Check programmatic disabled first (highest priority)
                typeDisabled = DisabledTypes.Contains(type);
                settingsTypeDisabled = SettingsDisabledTypes.Contains(type);

                if (!typeDisabled)
                {
                    // Programmatic type-specific configuration
                    TypeConfigurations.TryGetValue(type, out typeOptions);

                    // Settings-based type-specific configuration (lower priority)
                    SettingsTypeConfigurations.TryGetValue(type, out settingsTypeOptions);

                    // For generic types, find the best matching pattern by specificity
                    if (type.IsGenericType)
                    {
                        // Get all possible patterns for this type in order of specificity
                        foreach (Type pattern in PoolTypeResolver.GetAllMatchingPatterns(type))
                        {
                            // Skip the exact type (already handled above)
                            if (pattern == type)
                            {
                                continue;
                            }

                            // Check programmatic generic configurations
                            if (
                                GenericTypeConfigurations.TryGetValue(
                                    pattern,
                                    out PoolPurgeTypeOptions programmaticOptions
                                )
                            )
                            {
                                int priority = PoolTypeResolver.GetMatchPriority(type, pattern);
                                if (priority < bestProgrammaticGenericPriority)
                                {
                                    bestProgrammaticGenericPriority = priority;
                                    bestProgrammaticGenericOptions = programmaticOptions;
                                }
                            }

                            // Check settings-based generic configurations
                            if (
                                SettingsGenericTypeConfigurations.TryGetValue(
                                    pattern,
                                    out PoolPurgeTypeOptions settingsOptions
                                )
                            )
                            {
                                int priority = PoolTypeResolver.GetMatchPriority(type, pattern);
                                if (priority < bestSettingsGenericPriority)
                                {
                                    bestSettingsGenericPriority = priority;
                                    bestSettingsGenericOptions = settingsOptions;
                                }
                            }
                        }
                    }
                }
            }

            // 1. Programmatic disabled (highest priority for disabling)
            if (typeDisabled)
            {
                return new PoolPurgeEffectiveOptions(
                    enabled: false,
                    idleTimeoutSeconds: 0f,
                    minRetainCount: 0,
                    bufferMultiplier: DefaultBufferMultiplier,
                    rollingWindowSeconds: DefaultRollingWindowSeconds,
                    hysteresisSeconds: DefaultHysteresisSeconds,
                    spikeThresholdMultiplier: DefaultSpikeThresholdMultiplier,
                    source: PoolPurgeConfigurationSource.TypeDisabled
                );
            }

            // 2. Programmatic type-specific configuration
            if (typeOptions != null)
            {
                return BuildEffectiveOptions(
                    typeOptions,
                    PoolPurgeConfigurationSource.TypeSpecific
                );
            }

            // 3. Settings-based per-type configuration
            if (settingsTypeOptions != null)
            {
                return BuildEffectiveOptions(
                    settingsTypeOptions,
                    PoolPurgeConfigurationSource.UnityHelpersSettingsPerType
                );
            }

            // 4. Settings-based disabled
            if (settingsTypeDisabled)
            {
                return new PoolPurgeEffectiveOptions(
                    enabled: false,
                    idleTimeoutSeconds: 0f,
                    minRetainCount: 0,
                    bufferMultiplier: DefaultBufferMultiplier,
                    rollingWindowSeconds: DefaultRollingWindowSeconds,
                    hysteresisSeconds: DefaultHysteresisSeconds,
                    spikeThresholdMultiplier: DefaultSpikeThresholdMultiplier,
                    source: PoolPurgeConfigurationSource.UnityHelpersSettingsPerType
                );
            }

            // 5. PoolPurgePolicyAttribute on the type
            bool hasTypeAttribute = HasPoolPurgePolicyAttribute(
                type,
                out bool attributeEnabled,
                out PoolPurgeTypeOptions attributeOptions
            );
            if (hasTypeAttribute)
            {
                if (!attributeEnabled)
                {
                    return new PoolPurgeEffectiveOptions(
                        enabled: false,
                        idleTimeoutSeconds: 0f,
                        minRetainCount: 0,
                        bufferMultiplier: DefaultBufferMultiplier,
                        rollingWindowSeconds: DefaultRollingWindowSeconds,
                        hysteresisSeconds: DefaultHysteresisSeconds,
                        spikeThresholdMultiplier: DefaultSpikeThresholdMultiplier,
                        source: PoolPurgeConfigurationSource.Attribute
                    );
                }

                return BuildEffectiveOptions(
                    attributeOptions,
                    PoolPurgeConfigurationSource.Attribute
                );
            }

            // 6. Programmatic generic pattern (best match by specificity)
            if (bestProgrammaticGenericOptions != null)
            {
                return BuildEffectiveOptions(
                    bestProgrammaticGenericOptions,
                    PoolPurgeConfigurationSource.GenericPattern
                );
            }

            // 7. Settings-based generic pattern (best match by specificity)
            if (bestSettingsGenericOptions != null)
            {
                return BuildEffectiveOptions(
                    bestSettingsGenericOptions,
                    PoolPurgeConfigurationSource.UnityHelpersSettingsPerType
                );
            }

            // 8. Global defaults
            return new PoolPurgeEffectiveOptions(
                enabled: globalEnabled,
                idleTimeoutSeconds: DefaultGlobalIdleTimeoutSeconds,
                minRetainCount: DefaultGlobalMinRetainCount,
                bufferMultiplier: DefaultGlobalBufferMultiplier,
                rollingWindowSeconds: DefaultGlobalRollingWindowSeconds,
                hysteresisSeconds: DefaultGlobalHysteresisSeconds,
                spikeThresholdMultiplier: DefaultGlobalSpikeThresholdMultiplier,
                source: PoolPurgeConfigurationSource.GlobalDefaults
            );
        }

        /// <summary>
        /// Checks if intelligent purging is enabled for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to check.</typeparam>
        /// <returns><c>true</c> if intelligent purging is enabled; otherwise, <c>false</c>.</returns>
        public static bool IsEnabled<T>()
        {
            return GetEffectiveOptions<T>().Enabled;
        }

        /// <summary>
        /// Checks if intelligent purging is enabled for a specific type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if intelligent purging is enabled; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        public static bool IsEnabled(Type type)
        {
            return GetEffectiveOptions(type).Enabled;
        }

        private static PoolPurgeEffectiveOptions BuildEffectiveOptions(
            PoolPurgeTypeOptions options,
            PoolPurgeConfigurationSource source
        )
        {
            bool enabled = options.Enabled ?? GlobalEnabled;
            float idleTimeout = options.IdleTimeoutSeconds ?? DefaultGlobalIdleTimeoutSeconds;
            int minRetain = options.MinRetainCount ?? DefaultGlobalMinRetainCount;
            float buffer = options.BufferMultiplier ?? DefaultGlobalBufferMultiplier;
            float window = options.RollingWindowSeconds ?? DefaultGlobalRollingWindowSeconds;
            float hysteresis = options.HysteresisSeconds ?? DefaultGlobalHysteresisSeconds;
            float spike = options.SpikeThresholdMultiplier ?? DefaultGlobalSpikeThresholdMultiplier;

            return new PoolPurgeEffectiveOptions(
                enabled,
                idleTimeout,
                minRetain,
                buffer,
                window,
                hysteresis,
                spike,
                source
            );
        }

        private static bool HasPoolPurgePolicyAttribute(
            Type type,
            out bool enabled,
            out PoolPurgeTypeOptions attributeOptions
        )
        {
            enabled = true;
            attributeOptions = null;
            PoolPurgePolicyAttribute attribute =
                type.GetCustomAttribute<PoolPurgePolicyAttribute>();
            if (attribute == null)
            {
                return false;
            }

            enabled = attribute.Enabled;
            attributeOptions = new PoolPurgeTypeOptions
            {
                Enabled = attribute.Enabled,
                IdleTimeoutSeconds = attribute.IdleTimeoutSeconds,
                MinRetainCount = attribute.MinRetainCount,
            };
            return true;
        }
    }

    /// <summary>
    /// Mutable options for configuring intelligent pool purging for a specific type.
    /// </summary>
    public sealed class PoolPurgeTypeOptions
    {
        /// <summary>
        /// Gets or sets whether intelligent purging is enabled for this type.
        /// If null, uses the global setting.
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the idle timeout in seconds.
        /// Items idle longer than this are eligible for purging.
        /// If null, uses the global default.
        /// </summary>
        public float? IdleTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of items to always retain during purge operations.
        /// If null, uses the global default.
        /// </summary>
        public int? MinRetainCount { get; set; }

        /// <summary>
        /// Gets or sets the buffer multiplier for comfortable pool size calculation.
        /// If null, uses the global default.
        /// </summary>
        public float? BufferMultiplier { get; set; }

        /// <summary>
        /// Gets or sets the rolling window duration in seconds for high water mark tracking.
        /// If null, uses the global default.
        /// </summary>
        public float? RollingWindowSeconds { get; set; }

        /// <summary>
        /// Gets or sets the hysteresis duration in seconds.
        /// After a usage spike, purging is suppressed for this duration.
        /// If null, uses the global default.
        /// </summary>
        public float? HysteresisSeconds { get; set; }

        /// <summary>
        /// Gets or sets the spike threshold multiplier.
        /// A spike is detected when concurrent rentals exceed the rolling average by this factor.
        /// If null, uses the global default.
        /// </summary>
        public float? SpikeThresholdMultiplier { get; set; }
    }

    /// <summary>
    /// Immutable effective configuration for intelligent pool purging.
    /// </summary>
    public readonly struct PoolPurgeEffectiveOptions
    {
        /// <summary>
        /// Gets whether intelligent purging is enabled.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Gets the idle timeout in seconds.
        /// </summary>
        public float IdleTimeoutSeconds { get; }

        /// <summary>
        /// Gets the minimum number of items to always retain.
        /// </summary>
        public int MinRetainCount { get; }

        /// <summary>
        /// Gets the buffer multiplier for comfortable pool size calculation.
        /// </summary>
        public float BufferMultiplier { get; }

        /// <summary>
        /// Gets the rolling window duration in seconds for high water mark tracking.
        /// </summary>
        public float RollingWindowSeconds { get; }

        /// <summary>
        /// Gets the hysteresis duration in seconds.
        /// </summary>
        public float HysteresisSeconds { get; }

        /// <summary>
        /// Gets the spike threshold multiplier.
        /// </summary>
        public float SpikeThresholdMultiplier { get; }

        /// <summary>
        /// Gets the source of this configuration.
        /// </summary>
        public PoolPurgeConfigurationSource Source { get; }

        /// <summary>
        /// Creates a new effective options instance.
        /// </summary>
        public PoolPurgeEffectiveOptions(
            bool enabled,
            float idleTimeoutSeconds,
            int minRetainCount,
            float bufferMultiplier,
            float rollingWindowSeconds,
            float hysteresisSeconds,
            float spikeThresholdMultiplier,
            PoolPurgeConfigurationSource source
        )
        {
            Enabled = enabled;
            IdleTimeoutSeconds = idleTimeoutSeconds;
            MinRetainCount = minRetainCount;
            BufferMultiplier = bufferMultiplier;
            RollingWindowSeconds = rollingWindowSeconds;
            HysteresisSeconds = hysteresisSeconds;
            SpikeThresholdMultiplier = spikeThresholdMultiplier;
            Source = source;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"PoolPurgeEffectiveOptions(Enabled={Enabled}, IdleTimeout={IdleTimeoutSeconds}s, "
                + $"MinRetain={MinRetainCount}, Buffer={BufferMultiplier}, Window={RollingWindowSeconds}s, "
                + $"Hysteresis={HysteresisSeconds}s, SpikeThreshold={SpikeThresholdMultiplier}, Source={Source})";
        }
    }

    /// <summary>
    /// Indicates the source of a pool purge configuration.
    /// </summary>
    public enum PoolPurgeConfigurationSource
    {
        /// <summary>
        /// Configuration comes from global defaults.
        /// </summary>
        GlobalDefaults = 0,

        /// <summary>
        /// Configuration comes from a type-specific setting.
        /// </summary>
        TypeSpecific = 1,

        /// <summary>
        /// Configuration comes from a generic type pattern.
        /// </summary>
        GenericPattern = 2,

        /// <summary>
        /// Configuration comes from a <see cref="PoolPurgePolicyAttribute"/> on the type.
        /// </summary>
        Attribute = 3,

        /// <summary>
        /// The type is explicitly disabled.
        /// </summary>
        TypeDisabled = 4,

        /// <summary>
        /// Configuration comes from UnityHelpersSettings per-type configuration.
        /// </summary>
        UnityHelpersSettingsPerType = 5,
    }
}
