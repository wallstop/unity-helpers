Logging – Tag Formatter Demo

Showcases `UnityLogTagFormatter`, the logging extension methods (`this.Log`, `this.LogWarn`, etc.), and how to register custom decorations plus runtime toggles.

How to use

- Open `Scenes/LoggingDemo.unity` and press Play.
- Use the on-screen controls to enable/disable global logging, toggle pretty output, and emit info/warn/error logs.
- Edit the `LoggingDemoController` fields (NPC callsign, status label, pretty toggle) to see how decorator registration affects output.
  ![Image placeholder: Logging demo Game view with overlay UI and formatted logs in the Console]
  ![GIF placeholder: Logging demo UI toggles triggering info/warn/error logs with colored tags]

What it shows

- `[RuntimeInitializeOnLoadMethod]` bootstrap registering custom tag decorators (e.g., `npc` and `status=`).
- Runtime UI toggles calling `GlobalEnableLogging`, `DisableLogging`, and `SetGlobalLoggingEnabled`.
- Usage of `this.Log`, `this.LogWarn`, and `this.LogError` with custom formatting tags and pretty-mode toggles.
