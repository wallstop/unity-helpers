# Contributing

Thanks for helping make Unity Helpers better! This project uses a few automated checks and formatters to keep the codebase consistent and easy to review.

## Formatting and Linting

- C# formatting: CSharpier (via dotnet tools)
- Markdown/JSON/YAML formatting: Prettier
- Markdown linting: markdownlint
- Link checks: lychee and custom script
- YAML linting: yamllint

### Dependabot PRs

Dependabot PRs are auto-formatted by CI. The bot pushes commits (same‑repo PRs) or opens a formatting PR (forked PRs) so they pass formatting gates.

### Opt‑In Formatting for Contributor PRs

If you want the bot to apply formatting to your PR:

- Comment on your PR with `/format` (aliases: `/autofix`, `/lint-fix`).
  - If your branch is in this repo, the bot pushes a commit with fixes.
  - If your PR is from a fork, the bot opens a formatting PR targeting the base branch.
  - The commenter must be the PR author or a maintainer/collaborator.
- Or run manually from the Actions tab: select "Opt‑in Formatting", click "Run workflow", and enter the PR number.

What gets auto‑fixed:

- C# via CSharpier
- Markdown/JSON/YAML via Prettier
- Markdown lint via markdownlint with `--fix`

What does not auto‑fix:

- Broken links (lychee)
- YAML issues that require manual edits

## Run Checks Locally

- Install tools once:
  - `npm ci` (or `npm i --no-audit --no-fund`)
  - `dotnet tool restore`
- Format C#: `dotnet tool run CSharpier format`
- Check docs/JSON/YAML: `npm run validate:content`
- Enforce EOL/encoding: `npm run eol:check`

## Style and Naming

Please follow the conventions outlined in `.editorconfig` and the repository guidelines (PascalCase types, camelCase fields, explicit types, braces required, no regions).
