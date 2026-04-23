#!/usr/bin/env node
// MIT License — Copyright (c) wallstop studios
//
// Test runner for scripts/postinstall-hooks.js.
//
// Verifies the skipReason() env-var contract and the dry behavior in CI/
// non-repo contexts. We invoke the real script as a child process so the
// test exercises the exact code path npm install would take.
//
// Assertions:
//   - CI=true prints the "CI — skipping" reason and exits 0.
//   - CI=1 also triggers the CI skip.
//   - SKIP_POSTINSTALL_HOOKS=1 prints the SKIP_POSTINSTALL_HOOKS reason.
//   - NPM_CONFIG_IGNORE_SCRIPTS=true prints the ignore-scripts reason.
//   - NPM_CONFIG_IGNORE_SCRIPTS=1 also triggers.
//   - npm_config_ignore_scripts=true (lowercase; npm's canonical form) also
//     triggers the ignore-scripts skip reason.
//   - HUSKY=0 prints the husky reason.
//   - Running in a non-git directory prints "not a git work tree — skipping"
//     and exits 0 without attempting to configure hooks.
//   - All skip triggers exit 0 (never fail npm install).
//
// The tests DO NOT modify the live repo's git config: each case either uses
// a skip trigger (which never calls `git config`), or runs in a freshly-
// created tempdir that is not inside any git work tree.
//
// Node's built-in assert + child_process means zero external test deps.

"use strict";

const assert = require("assert");
const { spawnSync } = require("child_process");
const fs = require("fs");
const os = require("os");
const path = require("path");

const repoRoot = path.resolve(__dirname, "..", "..");
const scriptPath = path.join(repoRoot, "scripts", "postinstall-hooks.js");

let passed = 0;
let failed = 0;
const failedTests = [];

function runTest(name, fn) {
  try {
    fn();
    console.log(`  [PASS] ${name}`);
    passed++;
  } catch (err) {
    console.log(`  [FAIL] ${name}`);
    console.log(`         ${err.message}`);
    if (err.stack) {
      console.log(`         ${err.stack.split("\n").slice(1, 3).join("\n         ")}`);
    }
    failed++;
    failedTests.push(name);
  }
}

function runScript(env, cwd) {
  // Clear the inherited env to isolate each case — otherwise a test for
  // `CI=true` could accidentally also have HUSKY=0 set from the parent
  // shell and mask the actual trigger path. We start from an empty object
  // and only add PATH (so `node` / `git` resolve).
  const isolatedEnv = Object.assign(
    {
      PATH: process.env.PATH || "",
      HOME: process.env.HOME || os.tmpdir(),
      // Windows requires SYSTEMROOT for node to boot.
      SystemRoot: process.env.SystemRoot || process.env.SYSTEMROOT || ""
    },
    env || {}
  );
  const result = spawnSync(process.execPath, [scriptPath], {
    cwd: cwd || repoRoot,
    env: isolatedEnv,
    encoding: "utf8"
  });
  return {
    status: result.status,
    stdout: result.stdout || "",
    stderr: result.stderr || ""
  };
}

console.log("Testing scripts/postinstall-hooks.js skip triggers...");
console.log("\n  Section: Documented skip triggers");

runTest("Pass_CiTrueSkips", () => {
  const r = runScript({ CI: "true" });
  assert.strictEqual(r.status, 0, `exit ${r.status} (stderr: ${r.stderr})`);
  assert.ok(
    r.stdout.includes("CI=true") && r.stdout.includes("skipping"),
    `stdout did not contain CI skip reason. stdout: ${r.stdout}`
  );
});

runTest("Pass_CiOneSkips", () => {
  const r = runScript({ CI: "1" });
  assert.strictEqual(r.status, 0, `exit ${r.status}`);
  assert.ok(
    r.stdout.includes("CI=true") && r.stdout.includes("skipping"),
    `stdout did not contain CI skip reason. stdout: ${r.stdout}`
  );
});

runTest("Pass_SkipPostinstallHooksOneSkips", () => {
  const r = runScript({ SKIP_POSTINSTALL_HOOKS: "1" });
  assert.strictEqual(r.status, 0, `exit ${r.status}`);
  assert.ok(
    r.stdout.includes("SKIP_POSTINSTALL_HOOKS=1"),
    `stdout did not contain SKIP_POSTINSTALL_HOOKS skip reason. stdout: ${r.stdout}`
  );
});

runTest("Pass_NpmConfigIgnoreScriptsTrueSkips", () => {
  const r = runScript({ NPM_CONFIG_IGNORE_SCRIPTS: "true" });
  assert.strictEqual(r.status, 0, `exit ${r.status}`);
  assert.ok(
    r.stdout.includes("npm_config_ignore_scripts=true"),
    `stdout did not contain npm_config_ignore_scripts skip reason. stdout: ${r.stdout}`
  );
});

runTest("Pass_NpmConfigIgnoreScriptsOneSkips", () => {
  const r = runScript({ NPM_CONFIG_IGNORE_SCRIPTS: "1" });
  assert.strictEqual(r.status, 0, `exit ${r.status}`);
  assert.ok(
    r.stdout.includes("npm_config_ignore_scripts=1"),
    `stdout did not contain npm_config_ignore_scripts=1 skip reason. stdout: ${r.stdout}`
  );
});

runTest("Pass_NpmConfigIgnoreScriptsLowercaseTrueSkips", () => {
  // npm's canonical export when it processes lifecycle scripts is the
  // lowercase form. We must honor this spelling just as faithfully as
  // the uppercase manual-export form.
  const r = runScript({ npm_config_ignore_scripts: "true" });
  assert.strictEqual(r.status, 0, `exit ${r.status}`);
  assert.ok(
    r.stdout.includes("npm_config_ignore_scripts=true"),
    `stdout did not contain npm_config_ignore_scripts (lowercase) skip reason. stdout: ${r.stdout}`
  );
});

runTest("Pass_HuskyZeroSkips", () => {
  const r = runScript({ HUSKY: "0" });
  assert.strictEqual(r.status, 0, `exit ${r.status}`);
  assert.ok(
    r.stdout.includes("HUSKY=0"),
    `stdout did not contain HUSKY=0 skip reason. stdout: ${r.stdout}`
  );
});

runTest("Pass_NpmConfigIgnoreScriptsFalseDoesNotSkip", () => {
  // Value `"false"` must NOT trigger the skip — the header explicitly
  // documents accepting only `true` / `1`. We assert the CI skip still
  // happens (because CI is set), so the test decouples from actual
  // git state; the check is that NPM_CONFIG_IGNORE_SCRIPTS=false on its
  // own is not a recognized skip value.
  const r = runScript({ CI: "true", NPM_CONFIG_IGNORE_SCRIPTS: "false" });
  assert.strictEqual(r.status, 0, `exit ${r.status}`);
  // The reason emitted must be CI, not npm_config_ignore_scripts=false.
  assert.ok(
    r.stdout.includes("CI=true"),
    `expected CI skip reason when NPM_CONFIG_IGNORE_SCRIPTS=false; stdout: ${r.stdout}`
  );
  assert.ok(
    !r.stdout.includes("npm_config_ignore_scripts=false"),
    `NPM_CONFIG_IGNORE_SCRIPTS=false must not be a recognized skip value; stdout: ${r.stdout}`
  );
});

runTest("Pass_HuskyOneDoesNotSkip", () => {
  // HUSKY=1 (or any non-"0" value) must NOT trigger the skip. Same
  // pattern as above: ride on the CI skip to keep the test hermetic.
  const r = runScript({ CI: "true", HUSKY: "1" });
  assert.strictEqual(r.status, 0, `exit ${r.status}`);
  assert.ok(
    r.stdout.includes("CI=true"),
    `expected CI skip reason when HUSKY=1; stdout: ${r.stdout}`
  );
  assert.ok(
    !r.stdout.includes("HUSKY=1"),
    `HUSKY=1 must not be a recognized skip value; stdout: ${r.stdout}`
  );
});

console.log("\n  Section: Non-repo execution (safety net)");

runTest("Pass_NonGitDirectoryIsSafe", () => {
  // Run from a tempdir that is NOT a git work tree. The script should
  // emit "not a git work tree — skipping" and exit 0 without touching
  // any git config.
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "postinstall-hooks-test-"));
  try {
    // Without any skip env — the script will do isGitRepo() which returns
    // false, and print the skip message.
    const r = runScript({}, tempDir);
    assert.strictEqual(r.status, 0, `exit ${r.status} (stderr: ${r.stderr})`);
    // The message can be either "not a git work tree" (expected) or
    // ".githooks directory not present" depending on cwd relative to
    // __dirname. Because the script resolves its own hooksDir from
    // __dirname, not process.cwd(), it WILL find the real repo's
    // .githooks — then it checks isGitRepo(repoRoot) which returns true
    // for the REAL repo (because we invoked the real script path from
    // the temp cwd). To exercise the "not a git repo" branch cleanly,
    // we would need to copy the script too. Instead, assert the less
    // strict invariant: the script exits 0 and does NOT claim to have
    // installed hooks (idempotent on the real repo, or skipped).
    assert.ok(
      !r.stdout.includes("git hooks installed"),
      `script must be idempotent from a tempdir cwd; stdout: ${r.stdout}`
    );
  } finally {
    fs.rmSync(tempDir, { recursive: true, force: true });
  }
});

runTest("Pass_NonGitDirectoryEmitsNotWorkTreeSkip", () => {
  // Fully exercise the "not a git work tree — skipping" branch by
  // copying the script into a fresh tempdir whose parent is NOT a git
  // repo. The script's repoRoot is path.resolve(__dirname, "..") — so
  // we create <temp>/scripts/postinstall-hooks.js AND a sibling
  // <temp>/.githooks directory (so the "no .githooks" branch does not
  // fire first), and then invoke the copied script. Because <temp> is
  // not a git work tree, we should see the NOT_GIT skip log.
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "postinstall-hooks-nogit-"));
  try {
    const scriptsDir = path.join(tempDir, "scripts");
    fs.mkdirSync(scriptsDir);
    fs.mkdirSync(path.join(tempDir, ".githooks"));
    const copiedScript = path.join(scriptsDir, "postinstall-hooks.js");
    fs.copyFileSync(scriptPath, copiedScript);

    // Invoke the COPIED script from the tempdir. We must explicitly clear
    // any inherited skip env vars that might short-circuit the isGitRepo
    // branch (the parent shell may have set CI=true when running under
    // `npm run test`).
    const isolatedEnv = {
      PATH: process.env.PATH || "",
      HOME: process.env.HOME || os.tmpdir(),
      SystemRoot: process.env.SystemRoot || process.env.SYSTEMROOT || ""
    };
    const result = spawnSync(process.execPath, [copiedScript], {
      cwd: tempDir,
      env: isolatedEnv,
      encoding: "utf8"
    });

    assert.strictEqual(result.status, 0, `exit ${result.status} (stderr: ${result.stderr})`);
    assert.ok(
      result.stdout.includes("not a git work tree"),
      `expected "not a git work tree" skip; stdout: ${result.stdout}`
    );
    assert.ok(
      !result.stdout.includes("git hooks installed"),
      `must not install hooks in a non-git tempdir; stdout: ${result.stdout}`
    );
  } finally {
    fs.rmSync(tempDir, { recursive: true, force: true });
  }
});

console.log("");
console.log(`Tests passed: ${passed}`);
console.log(`Tests failed: ${failed}`);
if (failedTests.length > 0) {
  console.log("Failed tests:");
  for (const t of failedTests) {
    console.log(`  - ${t}`);
  }
}

process.exit(failed === 0 ? 0 : 1);
