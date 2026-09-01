using Godot;

namespace UniverseGame;

// Temporary test harness for EnvironmentManager — not a real gameplay
// flow. Starts the player piloting Ship in Space. Press Enter/Space
// (Godot's built-in "ui_accept" action, no project.godot input-map
// entry needed) near the EarthMarker in Space to land (swaps to Ground,
// spawning Character there); press it again while standing near Ground's
// ExitTile to leave (pops back to Space, Ship resumes exactly where it
// was parked). Proves Load/Unload, per-layer avatars, and the render-
// active toggle all work end to end. The real ship-to-space transition
// (roadmap step 4) replaces this debug harness later; this scene and
// script get deleted once that exists, not iterated on further.
public partial class EnvironmentTestBootstrap : Node2D
{
    [Export] public PackedScene ShipScene;
    [Export] public PackedScene CharacterScene;
    [Export] public PackedScene GroundScene;
    [Export] public PackedScene SpaceScene;

    // How close the avatar needs to be to EarthMarker/ExitTile for
    // ui_accept to actually trigger a transition — plain distance
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
            // In Space, piloting Ship — only land if actually near the
            // marker, rather than letting ui_accept work from anywhere.
            Node2D earthMarker = instance.GetNodeOrNull<Node2D>("EarthMarker");
            if (earthMarker != null && avatar.GlobalPosition.DistanceTo(earthMarker.GlobalPosition) <= InteractionRadius)
            {
                _environmentManager.LoadEnvironment(GroundScene, CharacterScene);
            }
        }
        else
        {
            // On Ground, walking as Character — only leave if standing
            // near the (placeholder, purple) ExitTile.
            Node2D exitTile = instance.GetNodeOrNull<Node2D>("ExitTile");
            if (exitTile != null && avatar.GlobalPosition.DistanceTo(exitTile.GlobalPosition) <= InteractionRadius)
            {
                _environmentManager.UnloadEnvironment();
            }
        }
    }
}
