// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace Samples.UnityHelpers.DI.Reflex
{
    using UnityEngine;

    /// <summary>
    /// Small service registered in the Reflex container so relational components receive dependencies.
    /// </summary>
    public sealed class ReflexPaletteService
    {
        private readonly Color _accentColor;
        private readonly Color _inactiveColor;
        private readonly Color _warningColor;

        public ReflexPaletteService(Color accentColor, Color inactiveColor, Color warningColor)
        {
            _accentColor = accentColor;
            _inactiveColor = inactiveColor;
            _warningColor = warningColor;
        }

        public Color AccentColor
        {
            get { return _accentColor; }
        }

        public Color InactiveColor
        {
            get { return _inactiveColor; }
        }

        public Color WarningColor
        {
            get { return _warningColor; }
        }
    }
}
