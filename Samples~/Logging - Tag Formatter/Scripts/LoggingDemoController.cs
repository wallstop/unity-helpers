namespace Samples.UnityHelpers.Logging
{
    using System;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;

    /// <summary>
    /// Runtime MonoBehaviour that exercises the logging extensions and exposes toggleable controls.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoggingDemoController : MonoBehaviour
    {
        [Header("Logging Settings")]
        [SerializeField]
        private bool logOnStart = true;

        [SerializeField]
        private bool startMuted;

        [SerializeField]
        private bool pretty = true;

        [Header("Decorator Inputs")]
        [SerializeField]
        private string npcCallsign = "Scout-17";

        [SerializeField]
        private string statusLabel = "alert";

        [SerializeField]
        private string reportMessage = "Intruder spotted near maintenance tunnel";

        [SerializeField]
        private Vector2 sectorRange = new Vector2(1f, 6f);

        private bool localLoggingEnabled = true;

        private void Awake()
        {
            localLoggingEnabled = !startMuted;
            if (startMuted)
            {
                this.DisableLogging();
            }
            else
            {
                this.EnableLogging();
            }
        }

        private void Start()
        {
            if (logOnStart)
            {
                EmitInfoLog();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 360f, 260f), GUI.skin.box);
            GUILayout.Label("Logging Demo Controls", GUI.skin.label);

            HandlePrettyToggle();
            HandleGlobalToggle();
            HandleLocalToggle();

            GUILayout.Space(8f);

            if (GUILayout.Button("Emit Info Log"))
            {
                EmitInfoLog();
            }

            if (GUILayout.Button("Emit Warning Log"))
            {
                EmitWarnLog();
            }

            if (GUILayout.Button("Emit Error Log"))
            {
                EmitErrorLog();
            }

            GUILayout.EndArea();
        }

        private void HandlePrettyToggle()
        {
            bool nextPretty = GUILayout.Toggle(pretty, "Pretty output (timestamp + thread)");
            if (nextPretty != pretty)
            {
                pretty = nextPretty;
            }
        }

        private void HandleGlobalToggle()
        {
            bool globalEnabled = WallstopStudiosLogger.IsGlobalLoggingEnabled();
            bool nextGlobal = GUILayout.Toggle(globalEnabled, "Global logging enabled");
            if (nextGlobal == globalEnabled)
            {
                return;
            }

            WallstopStudiosLogger.SetGlobalLoggingEnabled(nextGlobal);
        }

        private void HandleLocalToggle()
        {
            bool nextLocal = GUILayout.Toggle(localLoggingEnabled, "Component logging enabled");
            if (nextLocal == localLoggingEnabled)
            {
                return;
            }

            localLoggingEnabled = nextLocal;
            if (localLoggingEnabled)
            {
                this.EnableLogging();
            }
            else
            {
                this.DisableLogging();
            }
        }

        private void EmitInfoLog()
        {
            this.Log(BuildMessage($"{reportMessage}"), pretty: pretty);
        }

        private void EmitWarnLog()
        {
            FormattableString message = BuildMessage("Power draw exceeds safe limits");
            this.LogWarn(message, pretty: pretty);
        }

        private void EmitErrorLog()
        {
            FormattableString message = BuildMessage("Sensor grid offline — dispatch repair unit");
            this.LogError(message, pretty: pretty);
        }

        private FormattableString BuildMessage(string content)
        {
            string npc = string.IsNullOrWhiteSpace(npcCallsign) ? "Scout-17" : npcCallsign.Trim();
            string status = string.IsNullOrWhiteSpace(statusLabel) ? "alert" : statusLabel.Trim();
            string statusFormat = $"status={status}";
            string location =
                $"Sector-{UnityEngine.Random.Range(sectorRange.x, sectorRange.y):0.0}";

            return FormattableStringFactory.Create(
                "{0:npc} :: {1:" + statusFormat + "} @ {2:color=#7AD7FF}",
                npc,
                content,
                location
            );
        }
    }
}
