namespace UniverseGame;

// The real, decided conversion for the game's proprietary distance unit
// (Stellar Measure, "sm" - see the Planetary Generation System design
// doc). Confirmed 2026-08-27, completing that doc's originally-planned
// derivation order: character-scale feel decides the standard resolution
// (480x270, see DisplaySettings.cs), and the resolution decides the sm -
// not the other way around. 480px wide / 16px tiles = 30 tiles exactly;
// rounded up to 32 (a clean power of two, deliberately chosen for binary-
// friendly distance math elsewhere) as the actual defined width of 1 sm.
//
// This is a real, load-bearing constant now, not a placeholder - every
// future system doing Universal/Operational Scale distance math should
// reference this rather than hand-picking its own tile-per-sm number.
public static class StellarMeasure
{
    public const int TilesPerStellarMeasure = 32;
    public const int PixelsPerStellarMeasure = TilesPerStellarMeasure * 16; // 512

    public static float PixelsToStellarMeasures(float pixels) =>
        pixels / PixelsPerStellarMeasure;

    public static float StellarMeasuresToPixels(float sm) =>
        sm * PixelsPerStellarMeasure;
}
