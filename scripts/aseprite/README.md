# Aseprite automation scripts

## export_transparent_frames.lua

Batch-processes an `.aseprite` file into clean, transparent-background PNG
frames — built to fix a real recurring issue with the character template
pack (and likely useful for future sprite work too): Aseprite's special
"Background" layer type is always fully opaque, so straightforward exports
bake in a solid color instead of real transparency.

**What it does, per file:**
1. Auto-detects the background color by sampling the top-left pixel of frame 1 (doesn't need to be hardcoded per file).
2. Flattens each frame into a fresh, fully alpha-capable image (sidesteps the Background-layer limitation entirely — never modifies the original `.aseprite` file).
3. Converts pixels matching the background color (within a small tolerance, for anti-aliasing) to true transparency (alpha=0).
4. Skips any frame that's pixel-identical to an already-exported frame for that file (duplicate-frame removal — the character template's 4-frame walk cycles are actually 2 unique poses each, padded to 4 for even timing).
5. Saves the unique frames as `<outprefix>1.png`, `<outprefix>2.png`, etc.

**Usage:**
```bash
/Applications/Aseprite.app/Contents/MacOS/aseprite -b \
  --script-param input="path/to/file.aseprite" \
  --script-param outprefix="path/to/output-basename" \
  --script scripts/aseprite/export_transparent_frames.lua
```

Run once per source file — e.g. looped across all 8 direction files for the character template (`down`, `up`, `left`, `right`, `down left`, `down right`, `up left`, `up right`).

**Known limitation:** background detection assumes the top-left pixel of frame 1 is genuinely background, not character silhouette. True for this template pack; worth a quick visual check if reused on differently-composed source art.
