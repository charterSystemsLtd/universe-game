using Godot;
using System.Collections.Generic;

namespace UniverseGame;

// Autoload (singleton) — registered in Project Settings so exactly one
// instance exists and it's reachable from any scene without a node
// reference, closest Java comparison: a static/singleton service locator,
// except Godot wires it up declaratively via Project Settings rather than
// a manual getInstance() pattern.
//
// This is the single source of truth for the "8-bit calculable visuals"
// pipeline. Two genuinely separate things live here, on purpose:
//   - InGameResolution: the real internal render resolution - this is
//     the actual "how many pixels does the GPU compute" number, i.e. the
//     one setting in this whole system with a real performance cost.
//   - Fullscreen: whether the OS window is fullscreen or not.
// Everything about how big the final window/screen looks is handled by
// Godot's own stretch/upscale machinery, completely separate from - and
// free relative to - the above. See DisplaySettingsUI for the actual
// resolution-picker UI built on top of this.
public partial class DisplaySettings : Node
{
    // Curated preset list, not a continuous slider - deliberately not
    // tied to the 16px tile size at all. Partial ("spliced") tiles at the
    // screen edges are a real, intentional design choice here, not a
    // rounding error to avoid: they hint that the world continues past
    // the visible edge, same idea Terraria and plenty of other tile
    // games use rather than hard-locking resolution to a whole-tile grid.
    public static readonly List<Vector2I> ResolutionPresets = new()
    {
        new Vector2I(320, 180),
        new Vector2I(480, 270),
        new Vector2I(640, 360),
        new Vector2I(960, 540),
        new Vector2I(1280, 720),
        new Vector2I(1920, 1080),
    };

    [Export] public Vector2I InGameResolution = new Vector2I(320, 180);
    [Export] public bool Fullscreen = false;

    public override void _Ready()
    {
        // Windowed mode: dragging the window's edges resizes the actual
        // window (the upscale target), NOT InGameResolution - that's the
        // real compute-cost setting and only the preset list below
        // changes it. Fullscreen mode has no window edges to drag, so
        // the preset list becomes the only way to change how much is
        // rendered. Both cases fall out naturally from ContentScaleMode
        // = Viewport below; nothing extra needed to make window-dragging
        // behave this way.
        GetWindow().Unresizable = false;

        ApplyInGameResolution(InGameResolution);
        ApplyFullscreen(Fullscreen);
    }

    // ContentScaleMode.Viewport: Godot renders the whole game at
    // InGameResolution internally, then scales that fixed-size result up
    // to fill however large the actual window is - that final scale is a
    // single stretch operation on an already-finished image, not extra
    // rendering, which is the real source of the compute savings.
    //
    // ContentScaleAspect.Expand: reveals more (or less) of the game world
    // at the edges to genuinely fill the window/screen, rather than
    // letterboxing (Keep) or distorting (Ignore) when the window's
    // aspect ratio doesn't exactly match InGameResolution's.
    public void ApplyInGameResolution(Vector2I resolution)
    {
        InGameResolution = resolution;

        Window window = GetWindow();
        window.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleSize = resolution;
    }

    public void ApplyFullscreen(bool fullscreen)
    {
        Fullscreen = fullscreen;
        GetWindow().Mode = fullscreen
            ? Window.ModeEnum.Fullscreen
            : Window.ModeEnum.Windowed;
    }
}
