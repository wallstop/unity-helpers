#!/usr/bin/env node
// MIT License — Copyright (c) wallstop studios
//
// postinstall hook: ensure git hooks are installed for local contributors so
// `npm install` is sufficient to get pre-commit / pre-push safety nets.
//
// Idempotent: if core.hooksPath is already `.githooks`, does nothing.
//
// Safe in CI / non-repo checkouts:
//   - Skip when not inside a git work tree.
//   - Skip when CI=true (CI images install hooks explicitly if needed).
//   - Skip when NPM_CONFIG_IGNORE_SCRIPTS or HUSKY=0 equivalents requested.
//   - Never fails npm install: any error is logged and we exit 0.
//
// Cross-platform: the chmod is best-effort; on Windows, git-bash/cmd do not
// require chmod for hook execution (git invokes hooks via the shebang).

"use strict";

const { execSync, spawnSync } = require("child_process");
const fs = require("fs");
const path = require("path");

function skipReason() {
  if (process.env.CI === "true" || process.env.CI === "1") {
    return "CI=true — skipping postinstall hook install";
  }
  if (process.env.SKIP_POSTINSTALL_HOOKS === "1") {
    return "SKIP_POSTINSTALL_HOOKS=1 — skipping";
  }
  return null;
}

function isGitRepo(cwd) {
  const result = spawnSync("git", ["rev-parse", "--is-inside-work-tree"], {
    cwd,
    stdio: ["ignore", "pipe", "ignore"]
  });
  return result.status === 0 && String(result.stdout).trim() === "true";
}

function currentHooksPath(cwd) {
  const result = spawnSync("git", ["config", "--get", "core.hooksPath"], {
    cwd,
    stdio: ["ignore", "pipe", "ignore"]
  });
  if (result.status !== 0) {
    return "";
  }
  return String(result.stdout).trim();
}

function main() {
  const reason = skipReason();
  if (reason) {
    console.log(`[postinstall-hooks] ${reason}`);
    return;
  }

  const repoRoot = path.resolve(__dirname, "..");
  const hooksDir = path.join(repoRoot, ".githooks");

  if (!fs.existsSync(hooksDir)) {
    console.log("[postinstall-hooks] .githooks directory not present — skipping");
    return;
  }

  if (!isGitRepo(repoRoot)) {
    console.log("[postinstall-hooks] not a git work tree — skipping");
    return;
  }

  const existing = currentHooksPath(repoRoot);
  if (existing === ".githooks") {
    // Already configured — idempotent no-op.
    return;
  }

  if (existing !== "") {
    // A custom hooksPath is already set (power-user config) — do NOT overwrite.
    // Surface a hint so the contributor can opt in explicitly if they want this
    // repo's hooks.
    console.log(
      `[postinstall-hooks] core.hooksPath is "${existing}" — leaving unchanged. ` +
        "Run 'npm run hooks:install' if you want this repo's hooks."
    );
    return;
  }

  try {
    execSync("git config core.hooksPath .githooks", { cwd: repoRoot, stdio: "inherit" });
    // Best-effort chmod on Unix; ignored on Windows.
    if (process.platform !== "win32") {
      const hookFiles = ["pre-commit", "pre-merge-commit", "pre-push"];
      for (const hook of hookFiles) {
        const full = path.join(hooksDir, hook);
        if (fs.existsSync(full)) {
          try {
            fs.chmodSync(full, 0o755);
          } catch (_err) {
            // Non-fatal: user may already have correct permissions.
          }
        }
      }
    }
    console.log("[postinstall-hooks] git hooks installed (core.hooksPath=.githooks)");
  } catch (err) {
    // Never fail npm install over hook setup — log and move on.
    console.log(
      `[postinstall-hooks] warning: could not configure hooks (${err.message}). Run 'npm run hooks:install' manually.`
    );
  }
}

try {
  main();
} catch (err) {
  console.log(`[postinstall-hooks] warning: ${err.message}`);
}
