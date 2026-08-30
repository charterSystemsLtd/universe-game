using Godot;

namespace UniverseGame;

// Ground texture for the Individual Scale test scene - gives a real
// visual reference for how fast the character is actually moving.
//
// REBUILT 2026-08-28: now draws the real grass tile Xander made in
// Aseprite ("Grass Basic.aseprite", dropped in 07 Inbox), replacing the
// earlier flat-color-block placeholder. Exported via the same Aseprite
// CLI pipeline used for the character sprites (scripts/aseprite/) - see
// assets/sprites/ground/source/Grass Basic.aseprite for the original.
//
// REBUILT 2026-08-27: this used to be an infinitely-tiling illusion via
// ParallaxLayer mirroring - a small pattern repeated to look endless.
// That had a real, provable performance problem: how many times the
// pattern needs repeating to fill the screen scales directly with how
// much world is visible, which scales directly with InGameResolution -
// at 1920x1080 it needed to composite roughly 6x as many repeats as at
// 320x180, which is what caused the FPS collapse Xander hit. Now that
// there's a real, finite Planet with actual bounds, the ground is drawn
// ONCE, at its true full size, matching the Planet's real dimensions -
// no repeating, no mirroring, no scaling-with-resolution cost at all.
// _Draw() only runs once per node lifetime unless queue_redraw() is
// explicitly called (never is, here), so even a few hundred textured
// rects drawn once is genuinely trivial - the earlier cost was entirely
// from Parallax's repeat/mirror mechanism, not from this drawing logic.
//
// Drawn on a 16px grid to match the character's own tile size and the
// Planet's own TileSizePixels, so ground detail reads at a consistent
// "chunkiness" alongside the character. The grass tile itself is 16x16,
// matching GridStep exactly - no scaling needed.
public partial class GroundTileContent : Node2D
{
    // Manual fallback size, only used if SourcePlanet isn't assigned.
    [Export] public Vector2 TileSize = new Vector2(480, 480);
    [Export] public int GridStep = 16;
    [Export] public int RandomSeed = 7;

    // If assigned, the ground's real size is DERIVED from the planet
    // rather than duplicated as an independent constant - this closes a
    // real gap the earlier version had: TileSize here and Planet's own
    // SizeInTiles/TileSizePixels were two separate values that happened
    // to agree, with nothing keeping them in sync. Change Planet's size
    // later without this link and Ground would silently stop matching
    // the actual walkable/wrap boundary - the character could walk onto
    // unrendered space, or the visible ground could extend past where
    // wrapping actually happens.
    [Export] public Planet SourcePlanet;

    [Export] public string GrassTexturePath = "res://assets/sprites/ground/grass_basic.png";
    private Texture2D _grassTexture;

    // Applied as a per-tile modulate on top of the texture's own internal
    // dithering (not a separate draw color, now that there's a real
    // texture) - the fine per-pixel noise in the sprite alone isn't
    // coarse enough to read at a glance while moving; this coarser,
    // per-tile light/dark variation is what actually makes camera
    // movement readable, same reasoning the original flat-color
    // placeholder was built around.
    private static readonly Color VariantModulate = new Color(0.85f, 0.92f, 0.85f);

    public override void _Ready()
    {
        _grassTexture = GD.Load<Texture2D>(GrassTexturePath);
    }

    public override void _Draw()
    {
        if (_grassTexture == null)
        {
            _grassTexture = GD.Load<Texture2D>(GrassTexturePath);
        }

        Vector2 tileSize = SourcePlanet != null
            ? new Vector2(SourcePlanet.BoundsPixels * 2, SourcePlanet.BoundsPixels * 2)
            : TileSize;
        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)RandomSeed;

        int cols = (int)(tileSize.X / GridStep);
        int rows = (int)(tileSize.Y / GridStep);

        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                float x = -tileSize.X / 2f + col * GridStep;
                float y = -tileSize.Y / 2f + row * GridStep;

                Color modulate = rng.Randf() < 0.15f ? VariantModulate : Colors.White;
                DrawTextureRect(_grassTexture, new Rect2(x, y, GridStep, GridStep), false, modulate);
            }
        }
    }
}
