using Godot;

namespace UniverseGame;

// Temporary test harness for EnvironmentManager — not a real gameplay
// flow. Starts the player piloting Ship in Space. Press Enter/Space
// (Godot's built-in "ui_accept" action, no project.godot input-map
// entry needed):
//   - Near the EarthMarker in Space → land (swaps to Ground, spawning
//     Character there); near Ground's ExitTile → leave (pops back to
//     Space, Ship resumes exactly where it was parked).
//   - Anywhere else while piloting Ship in Space → board it (swaps to
//     ShipInterior, spawning Character at its EntryPoint); near
//     ShipInterior's own NavConsole → undock (pops back to Space, same
//     "resume exactly where it was parked" guarantee - see below for
//     why that's already correct for free).
// Proves Load/Unload, per-layer avatars, and the render-active toggle
// all work end to end. The real ship-to-space transition (roadmap step
// 4) replaces this debug harness later; this scene and script get
// deleted once that exists, not iterated on further.
public partial class EnvironmentTestBootstrap : Node2D
{
    [Export] public PackedScene ShipScene;
    [Export] public PackedScene CharacterScene;
    [Export] public PackedScene GroundScene;
    [Export] public PackedScene SpaceScene;
    [Export] public PackedScene ShipInteriorScene;

    // How close the avatar needs to be to EarthMarker/ExitTile/NavConsole
    // for ui_accept to actually trigger a transition — plain distance
    // checks, not Area2D collision, since this harness doesn't need
    // real physics-layer setup to prove the underlying mechanism works.
    [Export] public float InteractionRadius = 40f;

    private EnvironmentManager _environmentManager;

    public override void _Ready()
    {
        _environmentManager = GetNode<EnvironmentManager>("/root/EnvironmentManager");
        _environmentManager.LoadEnvironment(SpaceScene, ShipScene);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // IsEcho() guards against OS key-repeat firing this multiple
        // times from a single held keypress — without it, holding
        // Enter down could spam repeated Load/Unload calls.
        if (!@event.IsActionPressed("ui_accept") || @event.IsEcho()) return;

        Node2D avatar = _environmentManager.CurrentAvatar;
        Node2D instance = _environmentManager.CurrentInstance;
        if (avatar == null || instance == null) return;

        if (_environmentManager.StackCount == 1)
        {
            // In Space, piloting Ship — land takes priority if close
            // enough to EarthMarker; otherwise, piloting the ship at all
            // means you're always "at" it, so ui_accept boards it
            // instead. No distance check needed for boarding - there's
            // no meaningful "distance to my own ship" while I AM the ship.
            Node2D earthMarker = instance.GetNodeOrNull<Node2D>("EarthMarker");
            if (earthMarker != null && avatar.GlobalPosition.DistanceTo(earthMarker.GlobalPosition) <= InteractionRadius)
            {
                _environmentManager.LoadEnvironment(GroundScene, CharacterScene);
                return;
            }

            // Snapshot the ship's real velocity/spin/facing before it
            // gets frozen (SetLayerRenderActive(false) disables this
            // whole layer's _Process the instant the new layer pushes on
            // top) - this is what ShipInteriorStarfieldDrift uses to fake
            // "we're still coasting (and spinning)" once we're inside.
            // The real Ship node itself is untouched otherwise - not
            // destroyed, just hidden - so its actual velocity/rotation/
            // position resume correctly on their own the moment we
            // undock; this snapshot is ONLY for seeding the interior's
            // cosmetic parallax drift, not for restoring the ship itself.
            Vector2 shipVelocity = Vector2.Zero;
            float shipAngularVelocity = 0f;
            float shipRotation = 0f;
            if (avatar is Ship ship)
            {
                shipVelocity = ship.Velocity;
                shipAngularVelocity = ship.AngularVelocity;
                shipRotation = ship.Rotation;
            }

            _environmentManager.LoadEnvironment(ShipInteriorScene, CharacterScene);

            Node2D newInstance = _environmentManager.CurrentInstance;
            ShipInteriorStarfieldDrift drift = newInstance?.GetNodeOrNull<ShipInteriorStarfieldDrift>("Starfield");
            if (drift != null)
            {
                drift.DriftVelocity = shipVelocity;
                drift.DriftAngularVelocity = shipAngularVelocity;
                drift.InitialShipRotation = shipRotation;
                // The Character avatar LoadEnvironment just spawned - the
                // interior's parallax needs a live reference to it for
                // the small "head movement in a moving car" term (see
                // ShipInteriorStarfieldDrift.CharacterParallaxStrength).
                drift.CharacterAvatar = _environmentManager.CurrentAvatar;
            }
        }
        else if (instance.Name == "Planet")
        {
            // On Ground, walking as Character — only leave if standing
            // near the (placeholder, purple) ExitTile.
            Node2D exitTile = instance.GetNodeOrNull<Node2D>("ExitTile");
            if (exitTile != null && avatar.GlobalPosition.DistanceTo(exitTile.GlobalPosition) <= InteractionRadius)
            {
                _environmentManager.UnloadEnvironment();
            }
        }
        else if (instance.Name == "ShipInterior")
        {
            // Inside the ship, walking as Character — only undock if
            // standing near the NavConsole. UnloadEnvironment destroys
            // this ShipInterior instance (and its Character avatar) and
            // reactivates the Space/Ship layer underneath exactly as it
            // was left - same guarantee the Ground/ExitTile path above
            // already relies on, no extra state-restoring code needed
            // here at all.
            Node2D navConsole = instance.GetNodeOrNull<Node2D>("NavConsole");
            if (navConsole != null && avatar.GlobalPosition.DistanceTo(navConsole.GlobalPosition) <= InteractionRadius)
            {
                _environmentManager.UnloadEnvironment();
            }
        }
    }
}
