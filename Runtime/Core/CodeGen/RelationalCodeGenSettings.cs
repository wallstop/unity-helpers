namespace WallstopStudios.UnityHelpers.Core.CodeGen
{
    using UnityEngine;

    /// <summary>
    /// Project-level defaults for relational component code-generation.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RelationalCodeGenSettings",
        menuName = "Wallstop Studios/Relational CodeGen Settings",
        order = 0
    )]
    public sealed class RelationalCodeGenSettings : ScriptableObject
    {
        [SerializeField]
        private RelationalCodeGenPreference siblingDefault = RelationalCodeGenPreference.Disabled;

        [SerializeField]
        private RelationalCodeGenPreference parentDefault = RelationalCodeGenPreference.Disabled;

        [SerializeField]
        private RelationalCodeGenPreference childDefault = RelationalCodeGenPreference.Disabled;

        public RelationalCodeGenPreference SiblingDefault => this.siblingDefault;

        public RelationalCodeGenPreference ParentDefault => this.parentDefault;

        public RelationalCodeGenPreference ChildDefault => this.childDefault;
    }
}
