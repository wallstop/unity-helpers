namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target where one button in a group has explicit placement and others use defaults.
    /// This should NOT generate a placement conflict warning since only one explicit value is set.
    /// Mirrors the user scenario: one button sets groupPlacement explicitly, others default to UseGlobalSetting.
    /// </summary>
    public sealed class WButtonMixedExplicitAndDefaultPlacementTarget : ScriptableObject
    {
        // "Debug Tools" group - first button has explicit Top placement, second uses default
        [WButton("Log State", groupName: "Debug Tools", groupPlacement: WButtonGroupPlacement.Top)]
        public void LogState() { }

        [WButton("Clear Console", groupName: "Debug Tools")]
        public void ClearConsole() { }

        // "Save System" group - first button has explicit Bottom placement, others use default
        [WButton(
            "Save Game",
            groupName: "Save System",
            groupPlacement: WButtonGroupPlacement.Bottom
        )]
        public void SaveGame() { }

        [WButton("Load Game", groupName: "Save System")]
        public void LoadGame() { }

        [WButton("Delete Save", groupName: "Save System")]
        public void DeleteSave() { }
    }
}
