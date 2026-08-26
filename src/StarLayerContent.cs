using Godot;

namespace UniverseGame;

// The procedural visual content of ONE parallax depth-layer (stars,
// planets, an occasional nebula placeholder). This class only draws what
// goes inside a single repeating tile — the actual camera-relative
// movement and seamless wraparound are handled entirely by the parent
// ParallaxLayer node's `motion_scale` and `motion_mirroring` properties
// (set per-layer in Starfield.tscn), not by anything in this script.
//
// FIRST PASS, per Xander's explicit framing (2026-08-25): the movement/
// tiling/layering MECHANICS here are real and durable. The visuals
// themselves (plain circles standing in for stars/planets, a soft blob
// standing in for a nebula) are explicitly unverified placeholders,
// expected to be replaced once real art exists.
public partial class StarLayerContent : Node2D
{
    // Must match this layer's ParallaxLayer.motion_mirroring exactly —
    // it's the size of the area we scatter objects across, and has to
    // agree with the wraparound size or the tiling seam becomes visible.
    [Export] public Vector2 TileSize = new Vector2(2000, 2000);

    [Export] public int StarCount = 200;
    [Export] public int PlanetCount = 5;
    [Export] public Vector2 StarSizeRange = new Vector2(1f, 2f);
    [Export] public Vector2 PlanetSizeRange = new Vector2(8f, 20f);
    [Export] public bool IncludeNebula = false;

    // Each layer gets its own seed so the three layers don't accidentally
    // generate identical-looking patterns despite having different tile
    // sizes and object counts.
    [Export] public int RandomSeed = 1;

    public override void _Draw()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)RandomSeed;

        // Nebula/supernova placeholder — a soft, very low-opacity large
        // circle. Real art (an actual nebula/supernova texture) replaces
        // this later; this only exists so the layering itself is visible
        // and testable now.
        if (IncludeNebula)
        {
            Vector2 nebulaPos = RandomPointInTile(rng);
            DrawCircle(nebulaPos, 250f, new Color(0.6f, 0.35f, 0.85f, 0.08f));
        }

        // TODO(2026-08-25): PlanetCount is currently 0 on all three layers
        // in Starfield.tscn, and should stay that way. Two separate reasons
        // stack here, not just one:
        //   1. Any major body the player can actually see should be a real,
        //      visitable entity handled by the Planetary Generation System,
        //      not a decorative parallax object with no substance behind it.
        //   2. Even setting (1) aside, our own numbers (Planetary Generation
        //      System doc: 500x500 sm coordinates, only 75% spawn chance,
        //      region-scale distances in the thousands of sm) mean planets
        //      should be RARE at any given moment, not ambient clutter.
        //      Barely moving and seeing "hundreds of planets" directly
        //      contradicts the vastness the design calls for.
        // Do NOT just re-enable this loop with a small PlanetCount as a
        // quick fix later — the real fix is wiring actual visitable planet
        // positions from the generation system once it exists, spaced per
        // our real sm-based numbers (this is exactly what development
        // roadmap step 7, the sm-consistency audit, is for — deliberately
        // not tackled here/now, before physics tuning against the
        // reference Earth). Deep Space's nebula placeholder is NOT part of
        // this — that one's fine as pure background dressing, nothing to
        // visit there, and stays as-is.
        for (int i = 0; i < PlanetCount; i++)
        {
            Vector2 pos = RandomPointInTile(rng);
            float size = rng.RandfRange(PlanetSizeRange.X, PlanetSizeRange.Y);
            Color color = new Color(
                rng.RandfRange(0.5f, 1f),
                rng.RandfRange(0.5f, 1f),
                rng.RandfRange(0.5f, 1f),
                0.9f
            );
            DrawCircle(pos, size, color);
        }

        // Stars — small, plain near-white dots.
        for (int i = 0; i < StarCount; i++)
        {
            Vector2 pos = RandomPointInTile(rng);
            float size = rng.RandfRange(StarSizeRange.X, StarSizeRange.Y);
            DrawCircle(pos, size, new Color(1f, 1f, 1f, 0.8f));
        }
    }

    private Vector2 RandomPointInTile(RandomNumberGenerator rng)
    {
        return new Vector2(
            rng.RandfRange(-TileSize.X / 2f, TileSize.X / 2f),
            rng.RandfRange(-TileSize.Y / 2f, TileSize.Y / 2f)
        );
    }
}
