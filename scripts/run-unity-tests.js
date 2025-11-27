#!/usr/bin/env node

const { spawn } = require("child_process");
const { join } = require("path");

const isWindows = process.platform === "win32";
const scriptName = isWindows ? "run-unity-tests.ps1" : "run-unity-tests.sh";
const scriptPath = join(__dirname, scriptName);
const shell = isWindows ? "pwsh" : "bash";
const args = isWindows
  ? ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath]
  : [scriptPath];

const child = spawn(shell, args, {
  stdio: "inherit",
  env: process.env,
});

child.on("exit", (code) => {
  process.exit(code ?? 1);
});
