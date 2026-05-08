#!/usr/bin/env node

const { spawnSync } = require("node:child_process");
const path = require("node:path");

const repoRoot = path.resolve(__dirname, "..");
const runnerPath = path.join(repoRoot, "scripts", "run-node-bin.js");

const result = spawnSync(process.execPath, [runnerPath, "prettier", ...process.argv.slice(2)], {
  cwd: repoRoot,
  stdio: "inherit",
  windowsHide: true
});

if (result.error) {
  console.error(`Failed to launch Prettier: ${result.error.message}`);
  process.exit(1);
}

process.exit(result.status ?? 1);
