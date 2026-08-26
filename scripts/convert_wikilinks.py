#!/usr/bin/env python3
"""
Converts Obsidian wikilink syntax ([[path]] and ![[path]]) into standard
Markdown ([text](path) and ![](path)) across every .md file in a folder,
recursively.

Why this exists: the vault's design docs use native Obsidian wikilinks
(for backlinks, graph view, etc.) but the *synced copy* published to
GitHub needs to render as plain GitHub-flavored Markdown, which doesn't
understand Obsidian's [[...]] syntax at all - GitHub just shows the
literal bracket text instead of a working link/image. Run automatically
by sync-docs.sh on the synced copy only; the vault source is never
touched, so Obsidian-native functionality there is unaffected.

Only converts links pointing at the known Public-docs prefix (the only
wikilink targets that actually exist in the synced copy) - other
wikilinks are left alone rather than turned into dangling links.
"""

import re
import sys
from pathlib import Path

PREFIX = "03 Active Projects/Universe Game/Design/Public/"


def _encode(path: str) -> str:
    return path.replace(" ", "%20")


def _image_repl(match: re.Match) -> str:
    path = match.group(1)
    return f"![]({_encode(path)})"


def _link_repl(match: re.Match) -> str:
    path = match.group(1)
    name = path.rsplit("/", 1)[-1]
    if name.endswith(".md"):
        name = name[:-3]
    return f"[{name}]({_encode(path)})"


def convert(text: str) -> str:
    prefix_re = re.escape(PREFIX)
    # Images first - ![[...]] contains [[...]] as a substring, so it must
    # be handled before the plain-link pattern or the plain-link regex
    # would partially match inside it and corrupt the result.
    text = re.sub(r"!\[\[" + prefix_re + r"([^\]]+)\]\]", _image_repl, text)
    # Plain links - negative lookbehind excludes any ![[...]] the image
    # pass already converted (or missed because it didn't match the prefix).
    text = re.sub(r"(?<!!)\[\[" + prefix_re + r"([^\]]+)\]\]", _link_repl, text)
    return text


def main() -> None:
    if len(sys.argv) != 2:
        print("Usage: convert_wikilinks.py <folder>", file=sys.stderr)
        sys.exit(1)

    root = Path(sys.argv[1])
    changed = []
    for md_file in root.rglob("*.md"):
        original = md_file.read_text(encoding="utf-8")
        updated = convert(original)
        if updated != original:
            md_file.write_text(updated, encoding="utf-8")
            changed.append(md_file)

    if changed:
        print(f"Converted wikilinks in {len(changed)} file(s):")
        for f in changed:
            print(f"  {f}")
    else:
        print("No wikilinks found to convert.")


if __name__ == "__main__":
    main()
