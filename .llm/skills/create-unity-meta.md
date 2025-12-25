# Skill: Create Unity Meta File

**Trigger**: **MANDATORY** — After creating ANY new file or folder in the Unity package.

> ⚠️ **CRITICAL**: This skill is NOT optional. Every file and folder you create MUST have a corresponding `.meta` file generated immediately. Failure to generate meta files breaks Unity asset references and causes build failures.

---

## Why Meta Files Are Required

Unity requires a corresponding `.meta` file for every asset. Missing `.meta` files cause:

- Unity generating new ones with different GUIDs (breaks all references)
- Broken prefab, scene, and script references
- Lost inspector settings and serialized data
- Build failures and runtime errors

**This is a blocking requirement** — do not proceed with other tasks until meta files are generated.

---

## Command

```bash
./scripts/generate-meta.sh <path-to-file-or-folder>
```

---

## Examples

```bash
# For a new C# script
./scripts/generate-meta.sh Runtime/Core/NewFeature/MyNewClass.cs

# For a new folder (create parent folders' meta files first)
./scripts/generate-meta.sh Runtime/Core/NewFeature

# For documentation
./scripts/generate-meta.sh docs/features/new-feature.md

# For assembly definitions
./scripts/generate-meta.sh Runtime/NewAssembly.asmdef

# For shaders
./scripts/generate-meta.sh Shaders/NewShader.shader

# For UI Toolkit files
./scripts/generate-meta.sh Editor/Styles/NewStyle.uss
```

---

## When to Generate

Generate a `.meta` file whenever you create:

| File Type                         | Importer Used                       |
| --------------------------------- | ----------------------------------- |
| `.cs`                             | MonoImporter                        |
| `.asmdef`                         | AssemblyDefinitionImporter          |
| `.asmref`                         | AssemblyDefinitionReferenceImporter |
| `.shader`                         | ShaderImporter                      |
| `.compute`                        | ComputeShaderImporter               |
| `.shadergraph`, `.shadersubgraph` | ScriptedImporter                    |
| `.uss`, `.uxml`                   | UI Toolkit importers                |
| `.mat`                            | NativeFormatImporter                |
| `.asset`                          | NativeFormatImporter                |
| `.prefab`                         | PrefabImporter                      |
| `.unity`                          | DefaultImporter                     |
| `.png`, `.jpg`, `.tga`, etc.      | TextureImporter                     |
| `.wav`, `.mp3`, `.ogg`, etc.      | AudioImporter                       |
| `.fbx`, `.obj`, `.dae`, etc.      | ModelImporter                       |
| `.ttf`, `.otf`                    | TrueTypeFontImporter                |
| `.md`, `.txt`, `.json`, `.xml`    | TextScriptImporter                  |
| `package.json`                    | PackageManifestImporter             |
| directories                       | DefaultImporter (folderAsset)       |

---

## Important Rules

1. **Never skip meta file generation** — Every file and folder needs one. This is mandatory, not optional.
2. **Generate immediately** — Run the script right after creating the file/folder, before any other tasks
3. **Generate in creation order** — Parent folders before children
4. **Use the script** — Don't manually create meta files (proper GUIDs and importer settings)
5. **Don't modify existing meta files** — Changing GUIDs breaks references
6. **Verify generation** — Confirm the `.meta` file was created successfully

---

## Workflow for New Feature

```bash
# 1. Create folder structure
mkdir -p Runtime/Core/NewFeature

# 2. Generate meta for folder IMMEDIATELY
./scripts/generate-meta.sh Runtime/Core/NewFeature

# 3. Create the file (via create_file tool or editor)

# 4. Generate meta for file IMMEDIATELY
./scripts/generate-meta.sh Runtime/Core/NewFeature/MyClass.cs

# 5. Format code
dotnet tool run csharpier format .
```

---

## Checklist Before Proceeding

After creating any file or folder, verify:

- [ ] Meta file exists: `ls -la <path>.meta`
- [ ] Meta file is not empty and contains valid GUID
- [ ] Parent folder meta files also exist
