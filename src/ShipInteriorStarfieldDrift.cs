using Godot;

namespace UniverseGame;

// Ambient "we're still coasting through space" visual for the ship
// interior's own copy of the starfield - added 2026-09-10, answering
// Xander's question about keeping the parallax background consistent
// with the ship's actual momentum once you step back to Individual
// Scale (walking inside). Revised same day after real playtesting
// feedback (three real bugs, one real deferred limitation - see below).
//
// This is NOT a full physics simulation of the real Ship continuing to
// fly - EnvironmentManager's render-active toggle (SetLayerRenderActive)
// sets the whole Space/Ship layer's ProcessMode to Disabled while it's
// hidden underneath ShipInterior, so Ship._Process genuinely stops
// running the entire time the player is inside. Nothing about the real
// Ship node is advancing in the background - it's frozen, not coasting.
// Simulating a genuinely-still-moving ship in the background while
// unloaded/hidden is a real, much bigger feature (this is exactly what
// the not-yet-built Scheduled Event Queue is for elsewhere in the
// codebase - see EnvironmentManager.cs's TODOs) and deliberately out of
// scope here.
//
// What this IS: a cheap, decoupled illusion instead - seeded ONCE with
// the ship's real velocity, angular velocity, AND facing at the instant
// of boarding (see EnvironmentTestBootstrap, which reads these straight
// off Ship right before calling LoadEnvironment), then this just keeps
// integrating both forward for as long as the player is inside. Good
// enough to sell "we're still moving (and spinning)" without needing the
// real Ship to actually keep ticking.
public partial class ShipInteriorStarfieldDrift : ParallaxBackground
{
    public Vector2 DriftVelocity = Vector2.Zero;
    public float DriftAngularVelocity = 0f;

    // The avatar actually walking around inside - set by the bootstrap
    // right after LoadEnvironment spawns it. Needed for the character-
    // parallax term below; genuinely optional (stays null-safe) so this
    // script doesn't hard-require wiring it up everywhere.
    public Node2D CharacterAvatar;

    // How strongly the character's OWN movement inside the cabin shows
    // up as apparent exterior shift - 2026-09-10, Xander's own framing:
    // "like being in a moving car and moving your head side to side" -
    // a small fraction of 1:1, layered UNDERNEATH the dominant ship-
    // drift term, not replacing it. 0 disables this term entirely.
    public float CharacterParallaxStrength = 0.1f;

    private Vector2? _characterReference; // captured lazily, first frame CharacterAvatar exists
    private Vector2 _shipDriftAccumulated = Vector2.Zero;

    // The ship's real Rotation at the moment of boarding. Needed because
    // the interior scene's own "forward" (nose-up in its local, never-
    // rotating geometry - Xander's own call: rotating the actual interior
    // room to match would be visually nicer but meaningfully more
    // expensive, especially once larger multi-room ships/stations exist)
    // doesn't know which way the ship actually happened to be facing in
    // world space when you walked up to the console. Point 1 from
    // Xander's feedback: the view out the window has to be expressed in
    // the SHIP's own frame of reference, not the universe's raw one - a
    // stationary ship pointed east and one pointed west should not show
    // an identical starfield.
    public float InitialShipRotation = 0f;

    // How far the ship has continued to (simulated-)spin since boarding -
    // accumulated separately from InitialShipRotation so the two stay
    // easy to reason about independently (one is a snapshot, one is an
    // ongoing integration).
    private float _spinSinceBoarding = 0f;

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // Point 1 (reference frame + spin): rotate the WHOLE parallax
        // background - ParallaxBackground extends CanvasLayer, which
        // (unlike a plain Node2D parent-transform trick) has its own
        // real Rotation property that applies to everything it renders
        // as one unit, without touching the interior room geometry at
        // all. Negative sign is the actual inversion a rotating
        // reference frame produces: as the ship rotates one way, a
        // fixed external starfield has to visually rotate the OTHER way
        // relative to the ship's own windows, or the illusion breaks.
        //
        // Fixed 2026-09-10 (real playtest bug): plain `Rotation = angle`
        // pivots around CanvasLayer's own local origin, which sits at the
        // viewport's TOP-LEFT corner in screen space, not wherever the
        // player/camera actually is - visually, the whole starfield
        // swung around a corner of the screen instead of around the
        // ship. Verified directly via a headless probe (2026-09-10):
        // building the transform as `Transform2D(angle, center -
        // center.Rotated(angle))` keeps `center` itself fixed under
        // rotation for any angle, confirmed numerically for four
        // different test angles. `GetViewportRect().Size / 2` is used as
        // that pivot rather than a hardcoded resolution constant, so
        // this keeps working correctly regardless of window size/stretch
        // settings - and since Character's own Camera2D has no special
        // offset configured, viewport-center already IS wherever the
        // player (and thus the ship, conceptually) currently is.
        _spinSinceBoarding += DriftAngularVelocity * dt;
        float totalRotation = -(InitialShipRotation + _spinSinceBoarding);
        Vector2 pivot = GetViewport().GetVisibleRect().Size / 2f;
        Transform = new Transform2D(totalRotation, pivot - pivot.Rotated(totalRotation));

        // Point 2/3 (speed mismatch + only-one-layer-moving): verified
        // directly against Godot's own source via a standalone headless
        // probe (2026-09-10) - ParallaxLayer.position actually computes
        // as `scroll_offset * motion_scale + motion_offset`. The camera-
        // driven scroll_offset component gets scaled per-layer (that's
        // the whole point of motion_scale - Near/Far/Deep moving at
        // different rates). motion_offset does NOT - it's added raw,
        // identically, regardless of that layer's own motion_scale.
        //
        // Real follow-up bug found on the next playtest pass, root cause
        // deeper than the fix above: Godot's ParallaxBackground ALSO
        // auto-computes scroll_offset every frame from whichever camera
        // is currently active - which, once inside the ship, is
        // Character's own Camera2D. That gave a second, fully
        // uncontrolled parallax contribution driven by the character
        // just walking around a tiny room, fighting the intentional
        // drift/rotation. Fix: read whatever Godot's automatic camera-
        // tracking just computed this frame, cancel it out exactly, and
        // replace it with our OWN fully-controlled total - the dominant
        // ship-drift term plus a small, deliberately attenuated
        // character-movement term (CharacterParallaxStrength) standing
        // in for "the same subtle shift you'd see in distant scenery
        // moving your head side to side in a moving car."
        //
        // Real sign regression found on the NEXT playtest pass ("stars
        // moving backwards"), root-caused properly this time via a
        // reference probe rather than reasoning about signs again -
        // built a Camera2D that IS the ship (position directly driven by
        // DriftVelocity, exactly like the real Space.tscn piloting view
        // already works) and recorded what layer.position naturally does
        // as ground truth. Confirmed: moving the observer by worldDelta
        // changes layer.position by -worldDelta * motion_scale - the
        // background moves OPPOSITE the observer's own motion. This
        // version had DROPPED that negation. Same probe also confirmed
        // something important for the fix itself: camera ROTATION has
        // ZERO effect on this translation math (verified by rotating a
        // fixed-position reference camera through several angles -
        // layer.position never changed) - translation and rotation are
        // fully independent in Godot's own model. An earlier attempt at
        // this fix tried rotating DriftVelocity into "the ship's local
        // frame" before accumulating, based on physical intuition about
        // rotating reference frames - reverted, since that's not
        // actually how the reference (camera-driven) system behaves, and
        // "reasoning it out" is exactly what produced two sign bugs in a
        // row. The same "opposite of motion" convention applies to the
        // character term for the same reason - it's standing in for
        // what Godot's own (now-cancelled) automatic tracking would have
        // produced.
        _shipDriftAccumulated -= DriftVelocity * dt;

        // Real bug found from Xander's round-5 playtest ("stars moving in
        // opposite directions [while] ship moving slowly" + round-4's
        // "stars switching direction based on character's direction of
        // movement" during rotation) - root-caused by re-reading this
        // method's own frame-of-reference model rather than a new guess.
        // characterOffset is measured in the ShipInterior SCENE's own
        // local coordinates - the never-rotating room (see InitialShipRotation's
        // comment above: rotating the actual room geometry to match the
        // ship was ruled out as more expensive than it's worth). But
        // _shipDriftAccumulated is a WORLD-frame quantity (built directly
        // from Ship.Velocity, verified against the "camera IS the ship"
        // reference probe). Combining a local-room vector directly with a
        // world-frame vector was the bug: walking "right" in the room
        // means something different in the universe depending on which
        // way the ship currently happens to be facing - exactly the
        // "personal-view nose should point relative to the operational-
        // view nose" mismatch flagged in round 4. Fix: rotate
        // characterOffset into world space by the ship's actual current
        // orientation (InitialShipRotation + spin-since-boarding) before
        // combining. ShipWorldRotation is the positive form of that
        // orientation - totalRotation above is its negation (that's the
        // "background rotates opposite the ship" convention already
        // verified correct for the drift-only case), so re-negating it
        // here recovers the ship's real facing to rotate BY, not the
        // background's own display rotation.
        float shipWorldRotation = -totalRotation;

        Vector2 characterOffset = Vector2.Zero;
        if (CharacterAvatar != null)
        {
            _characterReference ??= CharacterAvatar.GlobalPosition;
            Vector2 localCharacterOffset = CharacterAvatar.GlobalPosition - _characterReference.Value;
            characterOffset = localCharacterOffset.Rotated(shipWorldRotation);
        }

        Vector2 totalDesiredShift = _shipDriftAccumulated - characterOffset * CharacterParallaxStrength;
        Vector2 realScrollOffset = ScrollOffset;

        foreach (Node child in GetChildren())
        {
            if (child is ParallaxLayer layer)
            {
                layer.MotionOffset = (totalDesiredShift - realScrollOffset) * layer.MotionScale;
            }
        }
    }
}

// TODO(!architecture) - point 4 from Xander's 2026-09-10 playtest
// feedback, deliberately parked (his own framing): the Ship's exterior
// position right now is 100% a function of its rendered Node2D.Position,
// which is frozen the instant its layer is hidden (ProcessMode.Disabled)
// - so undocking always resumes at the exact same spot you boarded from,
// even though the ship should have kept drifting the whole time you were
// inside (same "pure inertia, never stops on its own" model already
// established for Ship.cs's actual controls). Real fix needs to stop
// treating position as something that only exists via being rendered -
// Ship's location should become a MATH problem (last known position +
// velocity * elapsed real time, computed once on re-entry, the same
// closed-form "solve for state at time T" approach the Scheduled Event
// Queue already uses elsewhere in this codebase - see EnvironmentManager
// .cs/Universe Game.md's 2026-08-31 decisions-log entry) rather than
// something that has to be continuously ticked by a live, unhidden node
// to advance at all. Genuinely blocked, not just deferred: there's no
// real Universal Scale coordinate system yet for "current position" to
// be expressed in (Ship.Position is still bare screen-space pixels, the
// same placeholder status the sm-consistency audit roadmap step already
// exists to fix). Revisit once that exists - not before.
