#!/usr/bin/env node
// MIT License — Copyright (c) wallstop studios
//
// Adds one or more words to a cspell.json bucket without requiring a hand-edit
// of the 1000+-line file. Deduplicates, rejects cross-bucket duplicates, and
// validates the JSON round-trip before writing.
//
// Usage:
//   node scripts/add-cspell-word.js <bucket> <word> [<word>...]
//   node scripts/add-cspell-word.js --dry-run <bucket> <word> [...]
//
// Buckets:
//   unity-terms, csharp-terms, package-terms, tech-terms  -> dictionaryDefinitions
//   words                                                 -> root `words` array
//
// Exit codes:
//   0 success (or no-op if word already present)
//   1 usage / validation error

"use strict";

const fs = require("fs");
const path = require("path");
const { execSync } = require("child_process");

const CSPELL_PATH = path.resolve(__dirname, "..", "cspell.json");
const DICT_BUCKETS = new Set(["unity-terms", "csharp-terms", "package-terms", "tech-terms"]);
const ROOT_BUCKET = "words";

function usage(exitCode) {
  const msg = [
    "Usage: node scripts/add-cspell-word.js [--dry-run] <bucket> <word> [<word>...]",
    "",
    "Buckets:",
    "  unity-terms    Unity Engine APIs, components, lifecycle hooks",
    "  csharp-terms   C# language / BCL / SDK vocabulary",
    "  package-terms  Public API / symbols exported by this package",
    "  tech-terms     General programming / tooling vocabulary",
    "  words          Root words array (project-specific, lint-error prefixes)"
  ].join("\n");
  console.error(msg);
  process.exit(exitCode);
}

function parseArgs(argv) {
  const args = argv.slice(2);
  let dryRun = false;
  const positional = [];
  for (const arg of args) {
    if (arg === "--dry-run") {
      dryRun = true;
      continue;
    }
    if (arg === "-h" || arg === "--help") {
      usage(0);
    }
    positional.push(arg);
  }
  if (positional.length < 2) {
    usage(1);
  }
  const [bucket, ...words] = positional;
  return { bucket, words, dryRun };
}

function loadConfig() {
  const raw = fs.readFileSync(CSPELL_PATH, "utf8");
  return { raw, config: JSON.parse(raw) };
}

function findWordLocations(config, lowerWord) {
  const locations = [];
  if (Array.isArray(config.dictionaryDefinitions)) {
    for (const dict of config.dictionaryDefinitions) {
      if (Array.isArray(dict.words) && dict.words.some((w) => w.toLowerCase() === lowerWord)) {
        locations.push(`dictionary:${dict.name}`);
      }
    }
  }
  if (Array.isArray(config.words) && config.words.some((w) => w.toLowerCase() === lowerWord)) {
    locations.push("root:words");
  }
  return locations;
}

function getBucketWords(config, bucket) {
  if (bucket === ROOT_BUCKET) {
    if (!Array.isArray(config.words)) {
      config.words = [];
    }
    return config.words;
  }
  if (!Array.isArray(config.dictionaryDefinitions)) {
    throw new Error("cspell.json is missing dictionaryDefinitions");
  }
  const dict = config.dictionaryDefinitions.find((d) => d.name === bucket);
  if (!dict) {
    throw new Error(`Bucket '${bucket}' not found in dictionaryDefinitions`);
  }
  if (!Array.isArray(dict.words)) {
    dict.words = [];
  }
  return dict.words;
}

function main() {
  const { bucket, words, dryRun } = parseArgs(process.argv);

  if (!DICT_BUCKETS.has(bucket) && bucket !== ROOT_BUCKET) {
    console.error(`Error: unknown bucket '${bucket}'.`);
    usage(1);
  }

  const { raw, config } = loadConfig();
  const targetWords = getBucketWords(config, bucket);

  const added = [];
  const skipped = [];
  for (const word of words) {
    if (!word || /\s/.test(word)) {
      console.error(`Error: refusing to add empty or whitespace-containing word: '${word}'`);
      process.exit(1);
    }
    const lower = word.toLowerCase();
    const existingLocations = findWordLocations(config, lower);
    if (existingLocations.length > 0) {
      skipped.push({ word, locations: existingLocations });
      continue;
    }
    targetWords.push(word);
    added.push(word);
  }

  if (skipped.length > 0) {
    for (const s of skipped) {
      console.error(
        `Error: '${s.word}' already present in: ${s.locations.join(", ")}. Refusing duplicate.`
      );
    }
    process.exit(1);
  }

  if (added.length === 0) {
    console.log("No new words to add.");
    process.exit(0);
  }

  // Preserve existing line-ending style so .gitattributes-driven eol:check
  // (cspell.json is mandated CRLF by .gitattributes) doesn't flag the write.
  const eol = raw.includes("\r\n") ? "\r\n" : "\n";
  const serialized = JSON.stringify(config, null, 2) + "\n";
  const output = serialized.replace(/\r?\n/g, eol);
  // Validate round-trip before writing
  try {
    JSON.parse(output);
  } catch (err) {
    console.error(`Error: JSON round-trip validation failed: ${err.message}`);
    process.exit(1);
  }

  if (dryRun) {
    console.log(`[dry-run] Would add to '${bucket}': ${added.join(", ")}`);
    process.exit(0);
  }

  fs.writeFileSync(CSPELL_PATH, output, "utf8");
  console.log(`Added to '${bucket}': ${added.join(", ")}`);

  // Post-steps: format and validate. Do not fail the script if these tools
  // are unavailable (e.g. node_modules not yet installed); just surface a hint.
  const repoRoot = path.resolve(__dirname, "..");
  const runOptional = (label, cmd) => {
    try {
      execSync(cmd, { cwd: repoRoot, stdio: "inherit" });
      console.log(`[${label}] ok`);
    } catch (err) {
      console.warn(`[${label}] skipped: ${err.message}`);
    }
  };
  runOptional("prettier", "npx --no-install prettier --write -- cspell.json");
  runOptional("lint:spelling:config", "node scripts/lint-cspell-config.js");
  // Preserve original raw for diagnostics in case a caller wants diff
  void raw;
  process.exit(0);
}

main();
