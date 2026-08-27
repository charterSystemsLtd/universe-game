using Godot;

namespace UniverseGame;

// Represents a planet's real, finite surface bounds. Deliberately minimal
// for now - just size and edge behavior, enough to test movement against
// a real boundary instead of the previous illusory infinite one. Real
// composition/generation data (per the Planetary Generation System design
// doc) gets added to this later.
//
// SUGGESTED edge-of-world behavior (Xander asked for a recommendation,
// not decided unilaterally): WRAP AROUND, Asteroids-style - walking off
// one edge brings you out the opposite edge. Reasoning:
//   - It's a real, direct fit for this game's own existing lore: the
//     Planetary Generation System doc already establishes planets as
//     flat 2D areas, not spheres ("akin to a flat earth") - a flat,
//     wrapping surface is a coherent extension of that, not a bolt-on.
//   - It's cheap, well-understood, and battle-tested (the same technique
//     Asteroids uses, already in our own Inspirations Registry).
//   - It avoids having to invent real "what happens past the edge of a
//     30x30 test planet" behavior (a hard wall reads as an obviously
//     artificial testing limitation; wrap-around at least feels like a
//     real, intentional rule rather than a placeholder wall).
// This is very likely NOT the final answer for real generated planets
// (a genuinely enormous procedurally-generated planet probably shouldn't
// wrap at a hard edge the same way a small test planet does) - flagged
// as a first-pass choice for this test planet specifically, not a
// permanent design decision.
public partial class Planet : Node2D
{
    [Export] public int SizeInTiles = 30;
    [Export] public int TileSizePixels = 16;

    // Half-width/height from center, in pixels - the planet spans
    // [-BoundsPixels, +BoundsPixels] on both X and Z.
    public int BoundsPixels => (SizeInTiles * TileSizePixels) / 2;

    public PlanetPosition WrapPosition(PlanetPosition pos)
    {
        int wrappedX = Wrap(pos.X, -BoundsPixels, BoundsPixels);
        int wrappedZ = Wrap(pos.Z, -BoundsPixels, BoundsPixels);
        return new PlanetPosition(wrappedX, wrappedZ, pos.Y);
    }

    // Standard wrap-into-range math - the double-modulo handles negative
    // input correctly (C#'s % operator can return negative results for
    // negative input, unlike a true mathematical modulo; wrapping the
    // result a second time against `range` corrects that).
    private static int Wrap(int value, int min, int max)
    {
        int range = max - min;
        if (range <= 0) return min;
        int offset = value - min;
        int wrapped = ((offset % range) + range) % range;
        return wrapped + min;
    }
}
