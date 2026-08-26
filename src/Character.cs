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
public partial class Character : CharacterBody2D
{
    [Export] public float MoveSpeed = 100f;

    // Which of the 8 directions we're currently facing, so we know which
    // AnimatedSprite2D animation to play. Named to match the source sprite
    // pack's own per-direction file names exactly (down, up, left, right,
    // down left, down right, up left, up right) — see the character
    // folder's `source/` directory.
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

        Velocity = inputDir * MoveSpeed;
        MoveAndSlide(); // Godot's built-in collide-and-slide movement resolution.

        if (inputDir != Vector2.Zero)
        {
            _facing = DirectionName(inputDir);
            _sprite.Play(_facing);
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

        if (degrees >= 337.5f || degrees < 22.5f) return "right";
        if (degrees < 67.5f) return "down right";
        if (degrees < 112.5f) return "down";
        if (degrees < 157.5f) return "down left";
        if (degrees < 202.5f) return "left";
        if (degrees < 247.5f) return "up left";
        if (degrees < 292.5f) return "up";
        return "up right";
    }
}
