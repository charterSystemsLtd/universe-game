using Godot;

namespace UniverseGame;

// A position on a planet's surface, in whole pixels from the planet's
// center (0, 0, 0).
//
// Integer, not float - deliberate. A float-tile coordinate (e.g.
// "3.4375 tiles") would accumulate rounding drift over many small
// movements/many frames, and our actual visual precision floor is 1
// pixel anyway (1/16 of a 16px tile - there's nothing on screen a
// fraction of a pixel could represent). Storing whole pixels sidesteps
// an entire category of drift/precision bugs for no real cost, since we
// were never going to render anything finer than a pixel regardless.
//
// X and Z are the flat ground plane (matches Godot's own X/Y screen axes
// - see the note in Character.cs on that mapping). Y is reserved for the
// floor-index concept already defined in the Scale Terminology design
// doc (Individual Scale verticality) - always 0 for now, kept in the
// type today rather than bolted on later so nothing has to migrate when
// floors actually get built.
public struct PlanetPosition
{
    public int X;
    public int Y;
    public int Z;

    public PlanetPosition(int x, int z, int y = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override string ToString() => $"({X}, {Y}, {Z})";
}
