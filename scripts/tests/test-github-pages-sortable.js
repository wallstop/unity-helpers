#!/usr/bin/env node
// MIT License — Copyright (c) wallstop studios
//
// Regression tests for GitHub Pages sortable-table semantic sorting hooks.

"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "..", "..");

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

function readRelative(filePath) {
  return fs.readFileSync(path.join(repoRoot, filePath), "utf8");
}

function extractFunctionSource(sourceText, functionName) {
  const escapedName = functionName.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const signatureRegex = new RegExp(`function\\s+${escapedName}\\s*\\(`);
  const signatureMatch = signatureRegex.exec(sourceText);
  assert.ok(signatureMatch, `Expected function '${functionName}' to exist.`);

  const bodyStart = sourceText.indexOf("{", signatureMatch.index);
  assert.ok(bodyStart >= 0, `Expected function '${functionName}' body to start with '{'.`);

  let braceDepth = 0;
  for (let index = bodyStart; index < sourceText.length; index++) {
    const char = sourceText[index];
    if (char === "{") {
      braceDepth++;
    } else if (char === "}") {
      braceDepth--;
      if (braceDepth === 0) {
        return sourceText.slice(signatureMatch.index, index + 1);
      }
    }
  }

  throw new Error(`Unable to extract function body for '${functionName}'.`);
}

console.log("Testing GitHub Pages sortable table semantic sorting contracts...");

runTest("Pass_HeadCustomSupportsDataSortValueOverride", () => {
  const headCustom = readRelative("_includes/head-custom.html");
  const getSortableCellValueSource = extractFunctionSource(headCustom, "getSortableCellValue");
  const getSortableCellValue = new Function(
    `${getSortableCellValueSource}; return getSortableCellValue;`
  )();

  const semanticCell = {
    getAttribute(name) {
      return name === "data-sort-value" ? "6" : null;
    },
    textContent: "Very Fast"
  };

  const textFallbackCell = {
    getAttribute() {
      return null;
    },
    textContent: "  Very Slow  "
  };

  assert.strictEqual(
    getSortableCellValue(semanticCell),
    "6",
    "Expected semantic data-sort-value to override visible text."
  );
  assert.strictEqual(
    getSortableCellValue(textFallbackCell),
    "Very Slow",
    "Expected visible text fallback when no data-sort-value is provided."
  );
});

runTest("Pass_RandomBenchmarkBuilderEmitsSemanticSortValues", () => {
  const builder = readRelative("Tests/Runtime/Performance/RandomBenchmarkMarkdownBuilder.cs");

  assert.ok(
    builder.includes('data-sort-value=\\"{result.SpeedBucketSortValue}\\"'),
    "Expected random benchmark summary rows to emit speed sort values."
  );
  assert.ok(
    builder.includes('data-sort-value=\\"{result.QualitySortValue}\\"'),
    "Expected random benchmark summary rows to emit quality sort values."
  );
});

runTest("Pass_RandomBenchmarkDocumentContainsSemanticSortValues", () => {
  const markdown = readRelative("docs/performance/random-performance.md");
  const summarySectionMatch = markdown.match(
    /## Summary \(fastest first\)([\s\S]*?)## Detailed Metrics/
  );
  assert.ok(summarySectionMatch, "Expected random benchmark summary section to exist.");
  const summarySection = summarySectionMatch[1];
  const summaryRowCount = (summarySection.match(/<tr><td>/g) || []).length;

  const speedCells =
    summarySection.match(
      /<td data-sort-value="\d+">(?:Very Slow|Slow|Moderate|Fast|Very Fast|Fastest|Unknown)<\/td>/g
    ) || [];
  const qualityCells =
    summarySection.match(
      /<td data-sort-value="\d+">(?:Experimental|Poor|Fair|Good|Very Good|Excellent|Unknown)<\/td>/g
    ) || [];

  assert.strictEqual(
    speedCells.length,
    summaryRowCount,
    "Expected every summary row to include a semantic speed data-sort-value attribute."
  );
  assert.strictEqual(
    qualityCells.length,
    summaryRowCount,
    "Expected every summary row to include a semantic quality data-sort-value attribute."
  );
  assert.strictEqual(
    speedCells.length,
    qualityCells.length,
    "Expected speed and quality semantic sort cells to remain in lockstep per summary row."
  );
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
