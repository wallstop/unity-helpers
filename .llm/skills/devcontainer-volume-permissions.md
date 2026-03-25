# Devcontainer Volume Permissions

<!-- trigger: devcontainer, volume-permissions, docker-permissions, EACCES | Docker volume permission fixes for non-root devcontainer users | Feature -->

## When to Use

When configuring Docker named volume mounts in devcontainer.json for non-root users, or debugging EACCES/permission-denied errors during `postCreateCommand`.

## The Problem

Docker named volumes are **always created with root:root ownership**. When `remoteUser` is a non-root user (e.g., `"vscode"`, UID 1000), tools that write to volume-mounted directories fail with permission errors:

```text
npm error code EACCES
npm error path /home/vscode/.npm/_cacache
Unhandled exception: Access to the path '/home/vscode/.nuget/packages/...' is denied.
```

This is a [known Docker limitation](https://github.com/microsoft/vscode-remote-release/issues/9931) with no built-in fix from the devcontainer spec.

## Solution: Defense-in-Depth (Two Layers)

### Layer 1: Dockerfile Pre-Creation (New Volumes)

Docker copies image-layer permissions into **empty** volumes on first mount. Pre-create directories with correct ownership:

```dockerfile
# MUST be before USER vscode
RUN mkdir -p /home/vscode/.npm \
    && mkdir -p /home/vscode/.nuget/packages \
    && mkdir -p /home/vscode/.cache/pip \
    && chown -R vscode:vscode /home/vscode/.npm /home/vscode/.nuget /home/vscode/.cache
```

**Limitation**: Only works for brand-new volumes. Existing volumes retain their (root) ownership.

### Layer 2: postCreateCommand chown (All Volumes)

The critical fix. Run `sudo chown` as the **first operation** before any tools write to these directories:

```bash
sudo chown -R "$(id -u):$(id -g)" /home/vscode/.npm /home/vscode/.nuget /home/vscode/.cache/pip
```

**Must run before**: `dotnet tool restore`, `npm ci`, `npm install`, `pip install`

## Best Practices

### Extract postCreateCommand to a Script

Complex setup chains should live in `.devcontainer/post-create.sh`:

```json
"postCreateCommand": "bash .devcontainer/post-create.sh"
```

Benefits:

- Proper error handling (`set -euo pipefail`)
- Clear step logging
- Testable and lintable (shellcheck, bash -n)
- Easier to maintain than a 200-char JSON string

### Use Dynamic UID, Not Hardcoded

```bash
# GOOD: Works with any user
sudo chown -R "$(id -u):$(id -g)" "$dir"

# BAD: Breaks if UID changes or updateRemoteUserUID is active
sudo chown -R 1000:1000 "$dir"
```

### Volume Mount Ordering in devcontainer.json

Every volume mount target in `devcontainer.json` must have a corresponding:

1. `mkdir -p && chown` in the **Dockerfile** (defense-in-depth)
2. `sudo chown` in **post-create.sh** (critical fix)

### The postCreateCommand Cannot Use Object Format

The devcontainer spec supports an object format for parallel execution:

```json
"postCreateCommand": {
  "fix-perms": "sudo chown ...",
  "npm-install": "npm ci"
}
```

**Do NOT use this** when commands have ordering dependencies. The `chown` **must** complete before `npm ci` and `dotnet tool restore`. Use a sequential script instead.

## Lifecycle Command Order

```text
initializeCommand  -> Runs on HOST (before container)
onCreateCommand    -> Runs in container (after create, before user config)
updateContentCommand -> Runs in container (after clone/content update)
postCreateCommand  -> Runs in container (after user config applied) <-- USE THIS
postStartCommand   -> Runs every container start
postAttachCommand  -> Runs every VS Code attach
```

`postCreateCommand` is the correct hook because it runs after the container is fully configured with the `remoteUser` context, and only on first creation (not every start).

## Testing

Run the cross-validation test:

```bash
bash scripts/tests/test-post-create.sh --verbose
```

This data-driven test extracts volume mount targets from `devcontainer.json` and verifies they are handled in both `post-create.sh` and the `Dockerfile`.

## References

- [VS Code: Improve disk performance](https://code.visualstudio.com/remote/advancedcontainers/improve-performance)
- [Volume permissions issue #9931](https://github.com/microsoft/vscode-remote-release/issues/9931)
- [Dev container lifecycle commands](https://containers.dev/implementors/json_reference/)
