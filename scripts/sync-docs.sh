#!/usr/bin/env bash
# Syncs the sanitized/public design docs (vault source of truth: ../Design/Public,
# relative to this repo's parent project folder) into docs/design/ inside this repo,
# so the public design documentation gets versioned and published alongside the code.
#
# Source of truth is always the vault copy at Design/Public — never edit the
# docs/design/ copy in this repo directly, it will just get overwritten next sync.
#
# Usage: run from anywhere, e.g. `./scripts/sync-docs.sh`
#   then `git add docs/design && git commit -m "Sync public design docs"`

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SRC="$REPO_ROOT/../Design/Public"
DEST="$REPO_ROOT/docs/design"

if [ ! -d "$SRC" ]; then
  echo "Error: source folder not found at $SRC" >&2
  exit 1
fi

rm -rf "$DEST"
mkdir -p "$DEST"
cp -R "$SRC"/. "$DEST"/

# The vault source uses native Obsidian [[wikilinks]] (for backlinks/graph
# view there) - GitHub doesn't understand that syntax at all and just shows
# literal bracket text. Convert to standard Markdown links/images in the
# synced copy only; the vault source itself is never touched by this.
python3 "$SCRIPT_DIR/convert_wikilinks.py" "$DEST"

echo "Synced public design docs:"
echo "  $SRC"
echo "  -> $DEST"
echo ""
echo "Now run: git add docs/design && git commit -m \"Sync public design docs\""
