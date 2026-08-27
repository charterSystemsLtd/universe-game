using Godot;

namespace UniverseGame;

// Procedural ground texture for the Individual Scale test scene - gives a
// real visual reference for how fast the character is actually moving,
// replacing the earlier flat solid-color ground.
//
// FIRST PASS, same framing as StarLayerContent: the drawing mechanics
// here are real and durable, the actual grass visuals (flat color blocks
// standing in for real grass texture) are an unverified placeholder.
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
// explicitly called (never is, here), so even a few hundred rects drawn
// once is genuinely trivial - the earlier cost was entirely from
// Parallax's repeat/mirror mechanism, not from this drawing logic itself.
//
// Drawn on a 16px grid to match the character's own tile size and the
// Planet's own TileSizePixels, so ground detail reads at a consistent
// "chunkiness" alongside the character.
public partial class GroundTileContent : Node2D
{
    [Export] public Vector2 TileSize = new Vector2(480, 480);
    [Export] public int GridStep = 16;
    [Export] public int RandomSeed = 7;

    // Two shades of green, matching the flat-color-block placeholder style
    // already used for the starfield's planets/stars.
    private static readonly Color BaseGreen = new Color(0.29f, 0.55f, 0.24f);
    private static readonly Color VariantGreen = new Color(0.25f, 0.48f, 0.21f);

    public override void _Draw()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)RandomSeed;

        int cols = (int)(TileSize.X / GridStep);
        int rows = (int)(TileSize.Y / GridStep);

        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                float x = -TileSize.X / 2f + col * GridStep;
                float y = -TileSize.Y / 2f + row * GridStep;

                // Random per-cell variant gives the ground visible texture
                // (a grid of subtly different green blocks) rather than a
                // perfectly uniform fill - this is what actually makes
                // camera movement readable, not just the color itself.
                Color color = rng.Randf() < 0.15f ? VariantGreen : BaseGreen;
                DrawRect(new Rect2(x, y, GridStep, GridStep), color);
            }
        }
    }
}
