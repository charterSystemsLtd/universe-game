# docs/

`docs/design/` is a synced copy of this project's design documentation, kept up to date alongside the code.

**Do not edit files under `docs/design/` directly** — they're a generated copy and get overwritten on the next sync. Source edits happen elsewhere; run `./scripts/sync-docs.sh` after making changes, then commit the result:

```
./scripts/sync-docs.sh
git add docs/design
git commit -m "Sync public design docs"
```
