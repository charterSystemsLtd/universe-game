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
    // 480x270 listed first - it's the confirmed standard now, not just one
    // option among equals (see StellarMeasure.cs / the Planetary
    // Generation System doc for the sm derivation this decision completed).
    public static readonly List<Vector2I> ResolutionPresets = new()
    {
        new Vector2I(480, 270),
        new Vector2I(320, 180),
        new Vector2I(640, 360),
        new Vector2I(960, 540),
        new Vector2I(1280, 720),
        new Vector2I(1920, 1080),
    };

    // 480x270 confirmed 2026-08-27 as the real standard resolution, not
    // just a preset among equals - chosen by testing character-scale feel
    // fullscreen on real hardware (character centered, ~15-16 tiles of
    // context on each side). This is the actual completion of the sm
    // unit's originally-planned derivation order (Planetary Generation
    // System doc): character-scale feel decides the resolution, the
    // resolution decides the sm - not the other way around. See
    // PlanetPosition.cs / the design doc for the resulting sm-in-tiles
    // value.
    [Export] public Vector2I InGameResolution = new Vector2I(480, 270);
    [Export] public bool Fullscreen = false;

    // The aspect ratio windowed dragging gets locked to - deliberately
    // the physical screen's own native aspect ratio (whatever fullscreen
    // actually renders as), not InGameResolution's 16:9. The screen
    // itself might not be exactly 16:9 (e.g. a 16:10 MacBook panel), and
    // the point of this lock is "windowed dragging feels like zooming
    // the same picture fullscreen shows", not free-shaping into some
    // other aspect entirely.
    private float _windowAspect;
    private Vector2I _lastWindowSize;
    private bool _resizingForAspectLock = false;
    private bool _wasFullscreen = false;

    // Debounce state for the aspect-lock correction - see the 2026-08-28
    // note in OnWindowSizeChanged for why this exists (correcting on
    // every single intermediate SizeChanged event during a live corner
    // drag fights the OS's own resize-tracking session in real time).
    private Timer _resizeSettleTimer;
    private bool _dragInProgress = false;

    // Covers the screen while a resize/drag burst is in progress - see
    // OnWindowSizeChanged. Godot has no OS-level "lock the window's
    // aspect ratio while the user is actively dragging it" API, so the
    // window is left free to visually distort mid-drag (unavoidable,
    // that's genuinely what's happening on screen for that brief window)
    // but the player is never shown it - this overlay hides the game
    // entirely until OnResizeSettled has cleaned the shape back up.
    private CanvasLayer _dragOverlayLayer;
    private ColorRect _dragOverlay;

    public override void _Ready()
    {
        Window window = GetWindow();

        // Windowed mode: dragging the window's edges resizes the actual
        // window (the upscale target), NOT InGameResolution - that's the
        // real compute-cost setting and only the preset list below
        // changes it. Fullscreen mode has no window edges to drag, so
        // the preset list becomes the only way to change how much is
        // rendered. Both cases fall out naturally from ContentScaleMode
        // = Viewport below; nothing extra needed to make window-dragging
        // behave this way.
        window.Unresizable = false;

        Vector2I screenSize = DisplayServer.ScreenGetSize();
        _windowAspect = (float)screenSize.X / screenSize.Y;

        ApplyInGameResolution(InGameResolution);
        ApplyFullscreen(Fullscreen);

        _lastWindowSize = window.Size;

        // One-shot, restarted on every resize event - the aspect-lock
        // correction only actually runs once this timer elapses without
        // being restarted again, i.e. once resizing has genuinely
        // stopped. 0.15s is short enough to feel immediate once you let
        // go, long enough that a fast native drag (which fires SizeChanged
        // continuously) never lets it fire mid-drag.
        _resizeSettleTimer = new Timer();
        _resizeSettleTimer.OneShot = true;
        _resizeSettleTimer.WaitTime = 0.15;
        AddChild(_resizeSettleTimer);
        _resizeSettleTimer.Timeout += OnResizeSettled;

        // Layer 128 - render on top of literally everything else so the
        // overlay can never be accidentally covered by game content or
        // other UI. FullRect anchors mean it automatically tracks
        // whatever size the (currently-distorting) viewport is each
        // frame, with no extra code needed to keep it covering the
        // screen through the drag.
        _dragOverlayLayer = new CanvasLayer();
        _dragOverlayLayer.Layer = 128;
        AddChild(_dragOverlayLayer);

        _dragOverlay = new ColorRect();
        _dragOverlay.Color = Colors.Black;
        _dragOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        _dragOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _dragOverlay.Visible = false;
        _dragOverlayLayer.AddChild(_dragOverlay);

        // Bug found 2026-08-27: windowed mode was observed showing
        // pillarbox black bars on the sides (letterbox-style) even
        // though Expand is set below - Expand should never produce bars
        // at any window aspect ratio, so something was reverting the
        // content-scale config after startup. project.godot has no
        // [display] section (no baked-in default window size/stretch),
        // so the OS window is created with Godot's own engine defaults
        // BEFORE this autoload's _Ready() runs and reconfigures it -
        // fullscreen never hits this gap because it just fills the
        // physical screen outright regardless of any windowed-sizing
        // quirk. Re-applying on every resize closes the gap defensively:
        // whatever the window's actual size/aspect is at any moment,
        // the content-scale settings get re-asserted against it rather
        // than trusting a single startup call to stick.
        //
        // Added 2026-08-27: this same handler also enforces the aspect
        // lock below - Godot has no native "lock window aspect ratio"
        // property, so it's done by watching for resize and snapping the
        // window back onto the locked aspect ratio immediately after.
        window.SizeChanged += OnWindowSizeChanged;
    }

    private void OnWindowSizeChanged()
    {
        ApplyInGameResolution(InGameResolution);

        Window window = GetWindow();

        // Bug found 2026-08-28 (#1): a black bar + vertically-squashed
        // picture would appear after leaving fullscreen via macOS's own
        // green-button control, persisting until re-entering fullscreen.
        // Root cause: this handler was gating on our own `Fullscreen`
        // C# field, which is only ever updated when ApplyFullscreen()
        // runs - the native macOS fullscreen control changes the window
        // directly at the OS level and never calls that method, so
        // `Fullscreen` stayed stale/wrong the whole time, and the
        // aspect-lock correction ran against a window size still
        // mid-transition out of the OS's own fullscreen-exit animation.
        // Reading the engine's actual live window.Mode instead of our
        // own tracked flag means this can't drift out of sync, regardless
        // of whether fullscreen was entered/exited through our UI, code,
        // or native OS controls.
        bool isFullscreen = window.Mode == Window.ModeEnum.Fullscreen
            || window.Mode == Window.ModeEnum.ExclusiveFullscreen;
        Fullscreen = isFullscreen;

        if (isFullscreen || _resizingForAspectLock)
        {
            _lastWindowSize = window.Size;
            _wasFullscreen = isFullscreen;
            _dragInProgress = false;
            _dragOverlay.Visible = false;
            return;
        }

        Vector2I size = window.Size;
        bool justLeftFullscreen = _wasFullscreen && !isFullscreen;
        _wasFullscreen = false;

        if ((size == _lastWindowSize && !justLeftFullscreen) || size.X <= 0 || size.Y <= 0)
        {
            return;
        }

        // Bug found 2026-08-28 (#2): fixing bug #1 above exposed a
        // second, deeper issue - correcting the aspect ratio on every
        // single SizeChanged event during a live corner-drag means this
        // code is calling window.Size while macOS's own resize-tracking
        // session still owns and is actively driving that same property,
        // several times a second. The two fight each other in real time:
        // visually the content stretches to whatever shape the live drag
        // currently is (Expand faithfully reflecting the transient,
        // not-yet-corrected window shape), and the final settled size
        // ends up unpredictable rather than cleanly locked. Debouncing -
        // only actually correcting once no further resize events have
        // arrived for a short window - avoids ever fighting the OS mid-
        // drag at all: the drag is left alone to run freely, and exactly
        // one clean correction happens after it ends. See
        // _resizeSettleTimer in _Ready() / OnResizeSettled().
        //
        // Bug found 2026-08-28 (#3): the overlay below hides the visibly-
        // distorting content during that drag window - a player was
        // never meant to see the transient off-ratio shape, only the
        // "stretch while dragging" being an acceptable trade-off was
        // ever meant for us during testing, not for actual play.
        if (!_dragInProgress)
        {
            _dragInProgress = true;
            _dragOverlay.Visible = true;
        }

        _lastWindowSize = size;
        _resizeSettleTimer.Start();
    }

    private void OnResizeSettled()
    {
        _dragInProgress = false;

        Window window = GetWindow();
        Vector2I size = window.Size;

        // Width is always authoritative - deliberately not a per-drag
        // guess at "which axis did the user mean to change" (an earlier
        // version compared X/Y deltas from the start of the drag, which
        // flipped unpredictably and produced surprising snap directions
        // whenever the two were close). Width governs everywhere else in
        // this system already (ResolutionPresets, InGameResolution,
        // StellarMeasure are all width-first), so this is one consistent
        // rule instead of a heuristic: whatever width you drag to is
        // final, height always gets rederived from the locked aspect
        // ratio, regardless of how much vertical movement was involved.
        Vector2I corrected = size;
        corrected.Y = Mathf.RoundToInt(size.X / _windowAspect);

        if (corrected != size)
        {
            _resizingForAspectLock = true;
            window.Size = corrected;
            _resizingForAspectLock = false;
        }

        _lastWindowSize = window.Size;
        _dragOverlay.Visible = false;
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
