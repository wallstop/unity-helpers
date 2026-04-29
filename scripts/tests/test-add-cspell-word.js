#!/usr/bin/env node
// MIT License — Copyright (c) wallstop studios
//
// Regression tests for scripts/add-cspell-word.js.

"use strict";

const assert = require("assert");
const { spawnSync } = require("child_process");
const fs = require("fs");
const os = require("os");
const path = require("path");

const repoRoot = path.resolve(__dirname, "..", "..");
const sourceScriptPath = path.join(repoRoot, "scripts", "add-cspell-word.js");

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
    failed++;
    failedTests.push(name);
  }
}

function baseConfig() {
  return {
    version: "0.2",
    dictionaryDefinitions: [
      { name: "unity-terms", words: [] },
      { name: "csharp-terms", words: [] },
      { name: "package-terms", words: [] },
      { name: "tech-terms", words: [] }
    ],
    words: []
  };
}

function writeJsonWithLf(filePath, value) {
  const serialized = `${JSON.stringify(value, null, 2)}\n`;
  fs.writeFileSync(filePath, serialized, "utf8");
}

function createFixtureRepo(config) {
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), "add-cspell-word-test-"));
  const scriptsDir = path.join(tempDir, "scripts");
  fs.mkdirSync(scriptsDir, { recursive: true });
  fs.copyFileSync(sourceScriptPath, path.join(scriptsDir, "add-cspell-word.js"));
  writeJsonWithLf(path.join(tempDir, "cspell.json"), config);
  return tempDir;
}

function loadConfig(repoDir) {
  return JSON.parse(fs.readFileSync(path.join(repoDir, "cspell.json"), "utf8"));
}

function getBucketWords(config, bucket) {
  if (bucket === "words") {
    return config.words;
  }

  const dict = config.dictionaryDefinitions.find((entry) => entry.name === bucket);
  assert.ok(dict, `Missing bucket '${bucket}' in fixture config.`);
  return dict.words;
}

function runScript(repoDir, args) {
  const result = spawnSync(
    process.execPath,
    [path.join(repoDir, "scripts", "add-cspell-word.js"), ...args],
    {
      cwd: repoDir,
      encoding: "utf8"
    }
  );

  return {
    status: result.status,
    stdout: result.stdout || "",
    stderr: result.stderr || ""
  };
}

console.log("Testing scripts/add-cspell-word.js...");

runTest("Pass_AddsWordToTargetBucket", () => {
  const tempDir = createFixtureRepo(baseConfig());
  try {
    const result = runScript(tempDir, ["tech-terms", "Reentrant"]);
    assert.strictEqual(result.status, 0, `exit ${result.status} (stderr: ${result.stderr})`);

    const updatedConfig = loadConfig(tempDir);
    const techTerms = getBucketWords(updatedConfig, "tech-terms");
    assert.ok(techTerms.includes("Reentrant"), "Expected new word to be added to tech-terms.");
  } finally {
    fs.rmSync(tempDir, { recursive: true, force: true });
  }
});

runTest("Pass_ExistingWordInTargetBucketIsNoOp", () => {
  const config = baseConfig();
  config.dictionaryDefinitions.find((entry) => entry.name === "tech-terms").words.push("Reentrant");

  const tempDir = createFixtureRepo(config);
  try {
    const result = runScript(tempDir, ["tech-terms", "reentrant"]);
    assert.strictEqual(result.status, 0, `exit ${result.status} (stderr: ${result.stderr})`);
    assert.ok(
      result.stdout.includes("Already present in 'tech-terms'"),
      `Expected no-op message. stdout: ${result.stdout}`
    );

    const updatedConfig = loadConfig(tempDir);
    const techTerms = getBucketWords(updatedConfig, "tech-terms");
    assert.deepStrictEqual(
      techTerms,
      ["Reentrant"],
      "Target bucket should remain unchanged for same-bucket duplicates."
    );
  } finally {
    fs.rmSync(tempDir, { recursive: true, force: true });
  }
});

runTest("Fail_CrossBucketDuplicateIsRejected", () => {
  const config = baseConfig();
  config.words.push("UNH");

  const tempDir = createFixtureRepo(config);
  try {
    const result = runScript(tempDir, ["tech-terms", "unh"]);
    assert.notStrictEqual(result.status, 0, "Cross-bucket duplicate should fail.");
    assert.ok(
      result.stderr.includes("root:words"),
      `Expected duplicate diagnostic to name the existing bucket. stderr: ${result.stderr}`
    );

    const updatedConfig = loadConfig(tempDir);
    const techTerms = getBucketWords(updatedConfig, "tech-terms");
    assert.deepStrictEqual(
      techTerms,
      [],
      "Cross-bucket duplicates must not mutate the requested target bucket."
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
  for (const testName of failedTests) {
    console.log(`  - ${testName}`);
  }
}

process.exit(failed === 0 ? 0 : 1);
