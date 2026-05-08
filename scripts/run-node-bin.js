#!/usr/bin/env node

const { spawnSync } = require("node:child_process");
const { existsSync, readFileSync } = require("node:fs");
const path = require("node:path");

const toolMap = {
  prettier: { packageName: "prettier", binName: "prettier" },
  cspell: { packageName: "cspell", binName: "cspell" },
  markdownlint: { packageName: "markdownlint-cli", binName: "markdownlint" }
};

const requestedTool = process.argv[2];
if (!requestedTool || !toolMap[requestedTool]) {
  const tools = Object.keys(toolMap).join(", ");
  console.error(`Usage: node scripts/run-node-bin.js <${tools}> [args...]`);
  process.exit(2);
}

const repoRoot = path.resolve(__dirname, "..");
const { packageName, binName } = toolMap[requestedTool];
const packageJsonPath = path.join(repoRoot, "node_modules", packageName, "package.json");

if (!existsSync(packageJsonPath)) {
  console.error(
    `${requestedTool} is not installed in this repository. Run \`npm install\` on the same host that runs git hooks.`
  );
  process.exit(127);
}

let packageJson;
try {
  packageJson = JSON.parse(readFileSync(packageJsonPath, "utf8"));
} catch (error) {
  console.error(`Failed to read ${packageJsonPath}: ${error.message}`);
  process.exit(1);
}

const bin = packageJson.bin;
const relativeBinPath =
  typeof bin === "string" ? bin : bin && typeof bin === "object" ? bin[binName] : undefined;

if (!relativeBinPath) {
  console.error(`${packageName} does not declare a '${binName}' bin entry.`);
  process.exit(1);
}

const binPath = path.resolve(path.dirname(packageJsonPath), relativeBinPath);
if (!existsSync(binPath)) {
  console.error(`${requestedTool} bin entry does not exist at ${binPath}. Run \`npm install\`.`);
  process.exit(127);
}

let stdinBuffer;
if (!process.stdin.isTTY) {
  try {
    stdinBuffer = readFileSync(0);
  } catch (error) {
    console.error(`Failed to read stdin for ${requestedTool}: ${error.message}`);
    process.exit(1);
  }
}

const result = spawnSync(process.execPath, [binPath, ...process.argv.slice(3)], {
  cwd: repoRoot,
  input: stdinBuffer,
  stdio: [stdinBuffer ? "pipe" : "inherit", "inherit", "inherit"],
  windowsHide: true
});

if (result.error) {
  console.error(`Failed to launch ${requestedTool}: ${result.error.message}`);
  process.exit(1);
}

process.exit(result.status ?? 1);
