using Godot;

namespace UniverseGame;

// Autoload (singleton) — registered in Project Settings so exactly one
// instance exists and it's reachable from any scene without a node
// reference, closest Java comparison: a static/singleton service locator,
// except Godot wires it up declaratively via Project Settings rather than
// a manual getInstance() pattern.
//
// This is the single source of truth for the "8-bit calculable visuals"
// pipeline: how many actual pixels the game renders internally (the real
// compute-saving part - fewer pixels means less GPU work regardless of
// what size the window is), what window mode we're in, and how the low
// internal resolution gets scaled up to fill the window. Building this as
// its own Autoload rather than scattering these settings across scenes is
// what makes it possible to later drop real UI (sliders, a settings menu)
// on top without touching this logic at all - the UI just calls these
// methods.
public partial class DisplaySettings : Node
{
    // The actual internal render resolution - this is the real "how many
    // pixels does the GPU have to compute" number. Starts at a value in
    // the same family as what was discussed early on (320x180-ish),
    // adjustable at runtime via ApplyInternalResolution below.
    [Export] public int InternalWidth = 320;
    [Export] public int InternalHeight = 180;

    [Export] public bool Fullscreen = false;

    public override void _Ready()
    {
        // The window not being resizable was one of the real issues
        // raised - Godot defaults new projects to a resizable window, but
        // confirming it explicitly here rather than relying on whatever
        // the project file happens to have.
        GetWindow().Unresizable = false;

        ApplyInternalResolution(InternalWidth, InternalHeight);
        ApplyFullscreen(Fullscreen);
    }

    // Sets the real internal render resolution and switches the window's
    // stretch mode to actually use it. ContentScaleMode.Viewport means
    // Godot renders the whole game at (width, height) internally, then
    // scales that fixed-size result up to fill however large the actual
    // window is - the scaling itself is just stretching a small finished
    // image, not extra rendering work, which is where the real compute
    // savings come from versus rendering at native window resolution.
    //
    // ContentScaleAspect.Expand, not .Keep: Keep preserves the exact
    // internal aspect ratio by letterboxing (black bars) whenever the
    // window/screen doesn't match it exactly - real problem on a MacBook
    // screen that isn't exactly 16:9. Expand instead reveals more (or
    // less) of the game world at the edges to genuinely fill the window,
    // with every tile still rendered at a consistent, undistorted pixel
    // size - true screen-to-screen fill on native macOS fullscreen, no
    // black bars, nothing stretched out of shape.
    public void ApplyInternalResolution(int width, int height)
    {
        InternalWidth = width;
        InternalHeight = height;

        Window window = GetWindow();
        window.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleSize = new Vector2I(width, height);
    }

    public void ApplyFullscreen(bool fullscreen)
    {
        Fullscreen = fullscreen;
        GetWindow().Mode = fullscreen
            ? Window.ModeEnum.Fullscreen
            : Window.ModeEnum.Windowed;
    }
}
