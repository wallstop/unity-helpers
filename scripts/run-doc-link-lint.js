#!/usr/bin/env node

/**
 * Cross-platform launcher for scripts/lint-doc-links.ps1.
 * Finds a PowerShell executable (pwsh/pwsh-preview/powershell) and executes the canonical script.
 */

const { spawnSync } = require('child_process');
const path = require('path');

const repoRoot = path.resolve(__dirname, '..');
const scriptPath = path.resolve(repoRoot, 'scripts', 'lint-doc-links.ps1');

const userArgs = process.argv
    .slice(2)
    .map((arg) => (arg === '--verbose' || arg === '-v' ? '-VerboseOutput' : arg));

const baseArgs = ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', scriptPath, ...userArgs];

const candidates =
    process.platform === 'win32'
        ? ['pwsh.exe', 'pwsh-preview.exe', 'powershell.exe', 'powershell']
        : ['pwsh', 'pwsh-preview', 'powershell', 'powershell.sh'];

let lastEnoentError = null;

for (const candidate of candidates) {
    const result = spawnSync(candidate, baseArgs, {
        stdio: 'inherit',
    });

    if (result.error) {
        if (result.error.code === 'ENOENT') {
            lastEnoentError = result.error;
            continue;
        }

        console.error(`[lint-doc-links] Failed to run ${candidate}:`, result.error);
        process.exit(typeof result.status === 'number' ? result.status : 1);
    }

    process.exit(typeof result.status === 'number' ? result.status : 0);
}

const searched = candidates.join(', ');
console.error(
    `[lint-doc-links] Could not find a PowerShell executable. Tried: ${searched}. Install PowerShell 7+ (https://learn.microsoft.com/powershell/) or ensure one of those commands is on your PATH.`
);

if (lastEnoentError) {
    console.error(lastEnoentError.message);
}

process.exit(1);
