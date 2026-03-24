#!/usr/bin/env node
// MIT License — Copyright (c) wallstop studios
//
// Lints cspell.json for common configuration issues:
//   1. Case-redundant entries (when caseSensitive is false)
//   2. Cross-dictionary duplicates (same word in multiple dictionaries)
//   3. Root words that belong in a categorized dictionary
//
// Usage:
//   node scripts/lint-cspell-config.js          # Check only
//   node scripts/lint-cspell-config.js --fix    # Auto-deduplicate

"use strict";

const fs = require("fs");
const path = require("path");

const CSPELL_PATH = path.resolve(__dirname, "..", "cspell.json");

function loadConfig() {
  try {
    const raw = fs.readFileSync(CSPELL_PATH, "utf8");
    return JSON.parse(raw);
  } catch (err) {
    console.error(`Error loading cspell.json: ${err.message}`);
    process.exit(1);
  }
}

/**
 * Find case-redundant entries within a single word list.
 * Returns an array of { canonical, duplicates } objects.
 */
function findCaseRedundant(words) {
  const seen = new Map(); // lowercase → first occurrence
  const redundant = [];

  for (const word of words) {
    const lower = word.toLowerCase();
    if (seen.has(lower)) {
      const existing = seen.get(lower);
      existing.variants.push(word);
    } else {
      seen.set(lower, { canonical: word, variants: [] });
    }
  }

  for (const [, entry] of seen) {
    if (entry.variants.length > 0) {
      redundant.push({
        canonical: entry.canonical,
        duplicates: entry.variants
      });
    }
  }

  return redundant;
}

/**
 * Deduplicate a word list, keeping the first occurrence of each case-insensitive word.
 */
function deduplicateWords(words) {
  const seen = new Set();
  const result = [];
  for (const word of words) {
    const lower = word.toLowerCase();
    if (!seen.has(lower)) {
      seen.add(lower);
      result.push(word);
    }
  }
  return result;
}

/**
 * Find words that appear in multiple dictionaries or in both a dictionary and root words.
 */
function findCrossDuplicates(config) {
  const locations = new Map(); // lowercase → [location names]

  // Check categorized dictionaries
  if (config.dictionaryDefinitions) {
    for (const dict of config.dictionaryDefinitions) {
      if (dict.words) {
        for (const word of dict.words) {
          const lower = word.toLowerCase();
          if (!locations.has(lower)) {
            locations.set(lower, []);
          }
          locations.get(lower).push(`dictionary:${dict.name}`);
        }
      }
    }
  }

  // Check root words
  if (config.words) {
    for (const word of config.words) {
      const lower = word.toLowerCase();
      if (!locations.has(lower)) {
        locations.set(lower, []);
      }
      locations.get(lower).push("root:words");
    }
  }

  const duplicates = [];
  for (const [word, locs] of locations) {
    if (locs.length > 1) {
      duplicates.push({ word, locations: locs });
    }
  }

  return duplicates;
}

function main() {
  const fixMode = process.argv.includes("--fix");
  const config = loadConfig();
  const isCaseInsensitive = config.caseSensitive === false;
  let issueCount = 0;

  // ── Check 1: Case-redundant entries ──
  if (isCaseInsensitive) {
    // Check categorized dictionaries
    if (config.dictionaryDefinitions) {
      for (const dict of config.dictionaryDefinitions) {
        if (dict.words) {
          const redundant = findCaseRedundant(dict.words);
          for (const r of redundant) {
            issueCount++;
            const all = [r.canonical, ...r.duplicates].join(", ");
            console.log(
              `  [dictionary:${dict.name}] Case-redundant: ${all} (caseSensitive is false)`
            );
          }
          if (fixMode) {
            dict.words = deduplicateWords(dict.words);
          }
        }
      }
    }

    // Check root words
    if (config.words) {
      const redundant = findCaseRedundant(config.words);
      for (const r of redundant) {
        issueCount++;
        const all = [r.canonical, ...r.duplicates].join(", ");
        console.log(`  [root:words] Case-redundant: ${all} (caseSensitive is false)`);
      }
      if (fixMode) {
        config.words = deduplicateWords(config.words);
      }
    }
  }

  // ── Check 2: Cross-dictionary duplicates (warnings only — not blocking) ──
  const crossDups = findCrossDuplicates(config);
  let warningCount = 0;
  for (const d of crossDups) {
    warningCount++;
    console.error(
      `  [cross-duplicate] (warning) "${d.word}" appears in: ${d.locations.join(", ")}`
    );
  }

  // ── Summary ──
  if (issueCount > 0 && !fixMode) {
    console.log(`\ncspell.json: ${issueCount} error(s), ${warningCount} warning(s).`);
    console.log("Run with --fix to auto-deduplicate case-redundant entries.");
    process.exit(1);
  } else if (issueCount > 0 && fixMode) {
    // Write back
    const output = JSON.stringify(config, null, 2) + "\n";
    fs.writeFileSync(CSPELL_PATH, output, "utf8");
    console.log(
      `cspell.json: Fixed ${issueCount} case-redundant entries. Run prettier to re-format.`
    );
    process.exit(0);
  } else {
    if (warningCount > 0) {
      console.log(`cspell.json: No errors. ${warningCount} warning(s) (cross-duplicates).`);
    } else {
      console.log("cspell.json: No issues found.");
    }
    process.exit(0);
  }
}

main();
