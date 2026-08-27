using Godot;

namespace UniverseGame;

// Individual Scale on-foot character controller — first real test of the
// 16x16 sprite scale, part of the visual-style comparison pass (roadmap
// step 2 prep work: comparing 16x16 vs. 16x24 before committing, not yet
// the "real" reference Earth of step 3).
//
// CharacterBody2D (rather than plain Node2D, like Ship) gives us built-in
// collision/movement physics for free — relevant now that walls and trees
// are coming next, which the ship never needed since open space has
// nothing to collide with.
//
// TODO(!polish): character sprite's feet are cut off / read as missing.
// Confirmed via raw pixel inspection (2026-08-26): the current 16x16 CC0
// template art is drawn edge-to-edge with zero bottom margin - the
// character's feet are fully opaque all the way to the literal last pixel
// row of the canvas (row 15), so there's no room for the feet to read as
// grounded, and taller walking poses can clip outright. This is a real
// source-art constraint, not a rendering bug - not fixable by adjusting
// code/camera/collision. Real fix is the already-planned 16x24 sprite
// comparison (roadmap step 2), which gives the art actual headroom.
// Parked for a real art/polish pass, not blocking further prototype work.
public partial class Character : CharacterBody2D
{
    [Export] public float MoveSpeed = 100f;

    // Single source of truth for walk-animation playback speed (frames
    // per second). Previously each of the 8 animations had its own
    // "speed" value baked into the SpriteFrames resource in
    // Character.tscn, and they'd drifted inconsistent (down was 8 fps,
    // everything else was 5). Rather than hand-syncing 8 separate values
    // in the editor, _Ready below force-overwrites all 8 to this one
    // number every time the game starts - so the .tscn's baked values
    // become irrelevant, even if they drift again later.
    [Export] public float WalkFps = 8f;

    // How much faster both movement and animation get while sprinting -
    // one factor applied identically to both, so the animation always
    // stays visually consistent with actual movement speed instead of
    // the character appearing to slide/moonwalk.
    [Export] public float SprintMultiplier = 1.8f;

    // The planet this character is currently standing on - needed to know
    // the real, finite bounds to wrap movement against. Assigned in the
    // Godot editor (drag the Planet node onto this field in the
    // Inspector), not looked up automatically, since which planet a
    // character is on is exactly the kind of thing that changes when they
    // actually travel between planets later.
    [Export] public Planet CurrentPlanet;

    // Real position on the planet's surface, in whole pixels from its
    // center - see PlanetPosition.cs for why this is integer pixels, not
    // float tiles. Derived from this node's actual Godot Position each
    // physics frame, not maintained independently - Godot's Position is
    // still the real source of truth for where the character visually is,
    // this is a read-friendly, planet-relative view of the same fact.
    //
    // Axis mapping, worth being explicit about: Godot's 2D engine only has
    // X and Y. Our own design docs define the ground plane as X/Z (with Y
    // reserved for floor/altitude - see Scale Terminology). So Godot's Y
    // (screen-down) maps to our game's Z here, and PlanetPosition.Y stays
    // 0 until floors are actually implemented - this mapping needs to
    // stay consistent everywhere position gets touched, not just here.
    public PlanetPosition Position2D { get; private set; }

    // TODO(!architecture): need real functions for entering/leaving a
    // planet's surface, tied to the Containment Collapse Model's
    // expand/collapse mechanism (Master Design Document). Two separate,
    // currently-undefined operations:
    //   1. Instantiating a character's PlanetPosition on landing/arrival -
    //      given an incoming ship/context, where on the surface does the
    //      character actually start? (a designated landing zone? the
    //      ship's own touchdown position converted to surface
    //      coordinates? something else?)
    //   2. Ending/tearing down a character's PlanetPosition on departure -
    //      does it get discarded entirely, or preserved so returning to
    //      the same planet resumes near where they left? This directly
    //      affects whether a planet needs to remember per-character state
    //      at all once collapsed.
    // Neither is implemented yet - both are real open design questions
    // that matter once the Universal Scale <-> planet-landing connection
    // gets built (development roadmap step 6+), not urgent for the
    // current single-planet prototype.

    // Which of the 8 directions we're currently facing, so we know which
    // AnimatedSprite2D animation to play. Matches the animation names as
    // actually saved in Character.tscn's SpriteFrames (down, up, left,
    // right, down_left, down_right, up_left, up_right) - underscores, not
    // spaces, since Godot's animation-name field doesn't accept spaces.
    private string _facing = "down";

    private AnimatedSprite2D _sprite;

    // _Ready runs once, after this node and its children have entered the
    // scene tree. Closest Java comparison: not quite a constructor —
    // child-node references (like grabbing AnimatedSprite2D below) have
    // to wait until _Ready, since the tree isn't fully built yet when the
    // C# object itself is first constructed.
    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        // Force every animation to the same baseline speed, overriding
        // whatever's individually baked into the SpriteFrames resource -
        // this is the actual fix for the inconsistent-FPS issue, not
        // just a one-time cleanup. SpriteFrames (the resource, not the
        // node) is what SetAnimationSpeed lives on.
        SpriteFrames frames = _sprite.SpriteFrames;
        foreach (StringName animName in frames.GetAnimationNames())
        {
            frames.SetAnimationSpeed(animName, WalkFps);
        }
    }

    // _PhysicsProcess (as opposed to _Process, which Ship.cs uses) runs on
    // the fixed physics tick rather than the variable render tick — the
    // correct callback to use whenever movement involves collision, since
    // Godot's physics engine (MoveAndSlide below) expects to be driven
    // from here for consistent collision resolution.
    public override void _PhysicsProcess(double delta)
    {
        // GetVector reads four separate actions and combines them into a
        // single normalized direction — e.g. holding both "up" and "right"
        // gives a diagonal vector of consistent length, not one that's
        // faster than a single direction alone.
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        // Sprint applies the SAME multiplier to movement speed and
        // animation playback speed - that's the whole point of centralizing
        // WalkFps above rather than leaving each animation's speed baked
        // independently. Without this, sprinting would either look like
        // the character is sliding (feet not keeping up with real speed)
        // or moonwalking (feet outpacing it).
        bool sprinting = Input.IsActionPressed("sprint");
        float speedMultiplier = sprinting ? SprintMultiplier : 1f;

        Velocity = inputDir * MoveSpeed * speedMultiplier;
        MoveAndSlide(); // Godot's built-in collide-and-slide movement resolution.

        // Update the planet-relative position from wherever MoveAndSlide
        // actually ended up, then check it against the planet's real
        // bounds. If CurrentPlanet isn't assigned, skip entirely rather
        // than crash - lets this scene keep working before a Planet node
        // exists in it (e.g. mid-refactor), same defensive instinct as
        // not assuming a resource is always wired up.
        if (CurrentPlanet != null)
        {
            // RoundToInt, not a raw (int) cast - C#'s (int) cast truncates
            // toward zero, which is asymmetric around zero (-3.9 becomes
            // -3, not -4). Near the planet's center that would make
            // movement in one direction report differently than the same
            // distance in the opposite direction. Rounding is symmetric.
            var rawPosition = new PlanetPosition(
                Mathf.RoundToInt(Position.X),
                Mathf.RoundToInt(Position.Y));
            var wrappedPosition = CurrentPlanet.WrapPosition(rawPosition);
            Position2D = wrappedPosition;

            // Only actually move the node if wrapping changed something -
            // writing to Position every single frame regardless would be
            // harmless but wasteful, and would fight MoveAndSlide's own
            // collision resolution unnecessarily on the vast majority of
            // frames where no wrap happened at all.
            if (wrappedPosition.X != rawPosition.X || wrappedPosition.Z != rawPosition.Y)
            {
                Position = new Vector2(wrappedPosition.X, wrappedPosition.Z);
            }
        }

        if (inputDir != Vector2.Zero)
        {
            _facing = DirectionName(inputDir);
            _sprite.Play(_facing);
            _sprite.SpeedScale = speedMultiplier;
        }
        else
        {
            // Freeze on the current frame rather than assuming a separate
            // "idle" animation exists for every direction — the source
            // pack only defines the 8 walk animations themselves.
            _sprite.Stop();
        }
    }

    // Converts a raw input vector into one of 8 named directions, sliced
    // into equal 45-degree wedges around the circle.
    private string DirectionName(Vector2 dir)
    {
        float degrees = Mathf.RadToDeg(dir.Angle());
        if (degrees < 0) degrees += 360f;

        // Underscores, not spaces - Godot's animation-name field doesn't
        // accept spaces, so that's what actually got saved in
        // Character.tscn's SpriteFrames when the animations were created
        // in the editor. Matching that here rather than the other way
        // around, since it's a real engine constraint, not a preference.
        if (degrees >= 337.5f || degrees < 22.5f) return "right";
        if (degrees < 67.5f) return "down_right";
        if (degrees < 112.5f) return "down";
        if (degrees < 157.5f) return "down_left";
        if (degrees < 202.5f) return "left";
        if (degrees < 247.5f) return "up_left";
        if (degrees < 292.5f) return "up";
        return "up_right";
    }
}
