# GitHub Actions Script Pattern

<!-- trigger: actions, workflow, yaml, ci, script | Extract GHA logic to testable scripts | Feature -->

## Summary

When creating GitHub Actions workflows with complex logic, **extract that logic into standalone scripts** instead of embedding it directly in YAML. This makes the code testable, maintainable, and easier to debug.

## When to Use This Pattern

Use standalone scripts when your GitHub Actions workflow needs to:

- Transform or process files (link transformation, content generation)
- Perform complex string manipulation or parsing
- Generate dynamic content (sidebars, footers, indices)
- Execute multi-step processing pipelines
- Handle data that varies based on repository structure

## Why Not Embed Logic in YAML?

### Problems with embedded logic

1. **Untestable**: Can't run `pytest` on bash heredocs in YAML
2. **YAML parsing issues**: Heredocs preserve indentation, causing unexpected whitespace
3. **Debugging difficulty**: Hard to test locally without running the full workflow
4. **Regex limitations**: Shell regex differs from Python/Perl; complex patterns fail silently
5. **Maintenance burden**: Inline scripts are harder to review and modify
6. **No IDE support**: No syntax highlighting, linting, or autocompletion

### Benefits of standalone scripts

1. **Fully testable**: Write unit tests with pytest, verify edge cases
2. **Local debugging**: Run and debug locally before pushing
3. **IDE support**: Full syntax highlighting, linting, refactoring
4. **Reusable**: Scripts can be called from multiple workflows or locally
5. **Version control friendly**: Meaningful diffs for code changes
6. **Better error messages**: Script failures provide stack traces

## Implementation Pattern

### Script location

Place scripts in `scripts/` with logical subdirectories:

```text
scripts/
  wiki/
    transform_wiki_links.py    # Link transformation
    generate_wiki_sidebar.py   # Sidebar generation
    generate_wiki_footer.py    # Footer generation
    prepare_wiki.py            # Main orchestration script
    test_wiki_scripts.py       # Tests for all wiki scripts
```

### Script structure

Each script should:

1. Be executable with clear CLI interface
2. Use `argparse` for command-line arguments
3. Have comprehensive docstrings
4. Include error handling with clear messages
5. Be importable as a module for testing

```python
#!/usr/bin/env python3
"""
Brief description of what this script does.

Usage:
    python script.py input.md --output output.md
    python script.py --source /path/to/src --dest /path/to/dest
"""

import argparse
import sys
from pathlib import Path


def main_function(source: Path, dest: Path) -> int:
    """Core logic that can be imported and tested."""
    # Implementation
    return 0


def main():
    parser = argparse.ArgumentParser(description="Brief description.")
    parser.add_argument("--source", "-s", required=True, help="Source path")
    parser.add_argument("--dest", "-d", required=True, help="Destination path")
    args = parser.parse_args()

    result = main_function(Path(args.source), Path(args.dest))
    sys.exit(result)


if __name__ == "__main__":
    main()
```

### Workflow integration

Call scripts from workflows with minimal YAML:

```yaml
- name: Set up Python
  uses: actions/setup-python@v5
  with:
    python-version: "3.11"

- name: Prepare wiki content
  run: |
    python scripts/wiki/prepare_wiki.py \
      --source main \
      --dest wiki \
      --verbose
```

### Testing requirements

Every script must have corresponding tests:

```python
import pytest
from script_module import transform_function, process_function


class TestTransformFunction:
    def test_normal_case(self):
        assert transform_function("input") == "expected_output"

    def test_edge_case_empty(self):
        assert transform_function("") == ""

    def test_edge_case_special_chars(self):
        assert transform_function("special#chars") == "expected"
```

Run tests locally before committing:

```bash
python -m pytest scripts/wiki/test_wiki_scripts.py -v
```

## Example: Wiki Generation

### Before (embedded in YAML)

```yaml
# ❌ BAD: Complex logic embedded in YAML
- name: Fix wiki links
  run: |
    cd wiki
    for file in *.md; do
      sed -i -E 's|\(\.\.?/([^)]+)\.md\)|(\1)|g' "$file"
      sed -i -E 's|\(([a-z][^)]*)/([^)]*)\)|(\u\1-\u\2)|g' "$file"
    done
```

### After (standalone scripts)

```yaml
# ✅ GOOD: Logic in testable Python script
- name: Prepare wiki content
  run: |
    python scripts/wiki/prepare_wiki.py \
      --source main --dest wiki --verbose
```

## Checklist

Before adding logic to a GitHub Actions workflow:

- [ ] Can this logic be tested independently?
- [ ] Would a standalone script be easier to maintain?
- [ ] Does this involve string manipulation or file processing?
- [ ] Will this logic need to be debugged or modified later?

If you answered "yes" to any of these, create a standalone script.

## Related Skills

- [validate-before-commit](./validate-before-commit.md) — Ensure scripts pass linting
- [create-test](./create-test.md) — Write comprehensive tests for scripts
- [search-codebase](./search-codebase.md) — Find existing scripts to extend
