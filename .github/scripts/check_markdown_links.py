#!/usr/bin/env python3
import os
import re
import sys
import urllib.parse


EXCLUDE_DIRS = {".git", "node_modules", ".vs"}


def normalize_name(s: str) -> str:
    if not s:
        return ""
    # remove extension, strip non-alphanumerics, lowercase
    base = re.sub(r"\.[^.]+$", "", s)
    return re.sub(r"[^A-Za-z0-9]", "", base).lower()


LINK_RE = re.compile(r"(?<!\!)\[(?P<text>[^\]]+)\]\((?P<target>[^)\s]+)(?:\s+\"[^\"]*\")?\)")


def should_check_target(target: str) -> bool:
    if re.match(r"^(#|https?://|mailto:|tel:|data:)", target):
        return False
    # only check links that end in .md (ignoring anchors/query)
    core = re.sub(r"[?#].*$", "", target)
    try:
        core = urllib.parse.unquote(core)
    except Exception:
        pass
    return core.lower().endswith(".md")


def main(root: str) -> int:
    issues = 0
    for dirpath, dirnames, filenames in os.walk(root):
        # prune excluded directories
        dirnames[:] = [d for d in dirnames if d not in EXCLUDE_DIRS]
        for filename in filenames:
            if not filename.lower().endswith(".md"):
                continue
            path = os.path.join(dirpath, filename)
            try:
                with open(path, "r", encoding="utf-8") as f:
                    lines = f.readlines()
            except Exception:
                continue
            for idx, line in enumerate(lines, start=1):
                for m in LINK_RE.finditer(line):
                    text = m.group("text").strip()
                    target_raw = m.group("target").strip()
                    if not should_check_target(target_raw):
                        continue
                    target_core = re.sub(r"[?#].*$", "", target_raw)
                    try:
                        target_core = urllib.parse.unquote(target_core)
                    except Exception:
                        pass
                    file_name = os.path.basename(target_core)
                    base_name, _ = os.path.splitext(file_name)

                    is_exact_file_name = text.lower() == file_name.lower()
                    looks_like_path = (("/" in text) or ("\\" in text)) and not re.search(r"\s", text)
                    looks_like_markdown = text.strip().lower().endswith(".md")

                    if (
                        is_exact_file_name
                        or looks_like_path
                        or looks_like_markdown
                    ):
                        issues += 1
                        msg = f"{path}:{idx}: Link text '{text}' should be human-readable, not a raw file name or path (target: {target_raw})"
                        print(msg)

    if issues:
        print(
            f"Found {issues} documentation link(s) with non-human-readable text.",
            file=sys.stderr,
        )
        print(
            "Use a descriptive phrase instead of the raw file name.", file=sys.stderr
        )
        return 1
    return 0


if __name__ == "__main__":
    root = sys.argv[1] if len(sys.argv) > 1 else "."
    sys.exit(main(root))
