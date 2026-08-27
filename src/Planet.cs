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
// TODO(!architecture): current wrap-around is a hard "pop" (character and
// camera teleport instantly at the boundary), not smooth/seamless wrapping.
// Real fix, extended 2026-08-27: don't render the whole planet at all -
// render only what's actually visible on screen (character position +
// InGameResolution), plus a matching-size region from the wrapped
// opposite side when near an edge. Underlying item/creature logic still
// runs planet-wide on the backend; only the visual draw gets scoped to
// what's on screen - real compute savings once the planet has real
// content to render, not just a visual trick. Wrap-safe exactly as long
// as planet size >= visible render area; falls back to hard-wall
// collision the instant the planet is smaller. Not built. Full writeup:
// Planetary Generation System (Public) doc, "Finite-planet surface
// topology" section.
//
// TODO(!architecture): item/machine functionality that physically
// straddles the planet boundary (a connectable's input/output near or
// over the edge) needs to route through the wrap to the "stitched"
// opposite side, extending IConnectable (Item & Crafting Systems doc).
// No concrete code location yet - logged here since Planet.WrapPosition/
// WrappedDistance are the tools this will need. Full writeup: Planetary
// Generation System (Public) doc, "Finite-planet surface topology".
//
// TODO(!architecture): planets smaller than the visible render area risk
// showing the same physical location (or the character) more than once
// simultaneously once the smooth-wrap border-duplication TODO above gets
// built - a real physics-breaking duplicate, not just visual. Suggested
// direction: cap the effective visible render area to the planet's real
// size via the existing InGameResolution system, rather than a hard wall
// or camera zoom. Not a risk yet (current hard-pop wrap can't show two
// copies at once) - flagged for whenever the border-duplication work
// happens. Full writeup: Planetary Generation System (Public) doc,
// "Finite-planet surface topology".
//
// TODO(!architecture): Planet has no reference yet to the planet's actual
// identity/position/icon within the Universal Scale (the Star Chart, per
// the Master Design Document) - needed before real generation connects
// Universal Scale to individual landable planets. May be able to live as
// plain data on Planet itself rather than needing a separate system.
// Full writeup: Planetary Generation System (Public) doc, "Finite-planet
// surface topology" section.
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

    // Real distance between two surface positions, correctly accounting
    // for wrap-around - the "going around the seam" path counts if it's
    // actually shorter than the direct path. Without this, any proximity
    // check (event triggers, future item/machine boundary-routing, NPC
    // awareness) would treat two points that are genuinely right next to
    // each other across the seam as being almost the planet's full width
    // apart, which is wrong. Not wired into anything yet - this is the
    // foundational piece other systems will build on.
    public float WrappedDistance(PlanetPosition a, PlanetPosition b)
    {
        float dx = WrappedAxisDistance(a.X, b.X);
        float dz = WrappedAxisDistance(a.Z, b.Z);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // Shorter of "direct distance" and "distance the other way around"
    // for a single axis.
    private float WrappedAxisDistance(int a, int b)
    {
        float range = BoundsPixels * 2f;
        float direct = Mathf.Abs(a - b);
        return Mathf.Min(direct, range - direct);
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
