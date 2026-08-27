using Godot;

namespace UniverseGame;

// Procedural ground texture for the Individual Scale test scene - gives a
// real visual reference for how fast the character is actually moving,
// replacing the previous flat solid-color ground (which made speed
// impossible to judge, same underlying problem the starfield's camera-
// centering issue caused for the ship earlier).
//
// FIRST PASS, same framing as StarLayerContent: the tiling/repeat
// mechanics here are real and durable, the actual grass visuals (flat
// color blocks standing in for real grass texture) are an unverified
// placeholder.
//
// Drawn on a 16px grid to match the character's own tile size, so ground
// detail reads at a consistent "chunkiness" alongside the character
// rather than looking like two different pixel scales sitting on top of
// each other.
public partial class GroundTileContent : Node2D
{
    [Export] public Vector2 TileSize = new Vector2(320, 320);
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
