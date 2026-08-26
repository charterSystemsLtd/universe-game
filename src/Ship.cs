using Godot;

namespace UniverseGame;

// Placeholder ship movement — development roadmap step 1.
// Deliberately rough/untuned: real physics numbers (thrust, drag, mass)
// get established later once the reference Earth exists (roadmap steps
// 2-3) and this gets tuned against that same baseline (roadmap step 5).
// For now this just needs to exist and feel roughly right: rotate-and-
// thrust, zero friction, pure inertia — the foundational space-piloting
// archetype (see the Inspirations Registry).
public partial class Ship : Node2D
{
	// [Export] exposes this field in the Godot editor's Inspector panel,
	// so tuning doesn't require touching code at all — closest Java
	// comparison: imagine a public field that automatically gets a
	// generated property-editor UI, no getter/setter boilerplate needed.
	[Export] public float ThrustPower = 400f;
	[Export] public float RotationSpeed = 3f;
	[Export] public float DecelerationPower = 300f;

	// Current velocity, persists frame to frame. This is what gives us
	// "zero friction, pure inertia" — nothing here ever subtracts from
	// _velocity except the player actively thrusting in a new direction.
	private Vector2 _velocity = Vector2.Zero;

	// _Process runs once per rendered frame. In general OOP terms, this
	// is Godot calling back into an overridden method every frame — same
	// idea as overriding a game loop's update() method in a Java game
	// framework. `delta` is the time (in seconds) since the last frame,
	// used so movement speed stays consistent regardless of framerate.
	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// Rotation: A/D or Left/Right arrow keys turn the ship.
		// Godot's `Input` is a static-like global service — similar to
		// calling a static utility method in Java, no construction or
		// dependency injection needed to read input state.
		float rotationInput = Input.GetAxis("ship_rotate_left", "ship_rotate_right");
		Rotation += rotationInput * RotationSpeed * dt;

		// Thrust: W or Up arrow adds velocity in the direction the ship
		// is currently facing. Rotation is in radians; rotating the
		// "up" unit vector by our current facing gives a vector pointing
		// wherever the ship's nose is pointed right now.
		if (Input.IsActionPressed("ship_thrust"))
		{
			Vector2 forward = Vector2.Up.Rotated(Rotation);
			_velocity += forward * ThrustPower * dt;
		}

		// Deceleration: S or Down Arrow brakes, regardless of which way
		// the ship is currently facing. This is a deliberate simplification
		// — a "real" reverse-thrust model would only slow you down if you
		// were actively facing away from your direction of travel, but
		// that's more complexity than a step-1 placeholder needs.
		// Vector2.MoveToward steps a vector toward a target by a fixed
		// max distance per call, without ever overshooting past it — so
		// braking naturally stops exactly at zero rather than flipping
		// into reverse once nearly stopped.
		if (Input.IsActionPressed("ship_decelerate"))
		{
			_velocity = _velocity.MoveToward(Vector2.Zero, DecelerationPower * dt);
		}

		// Friction/drag otherwise stays absent on purpose — velocity only
		// changes from active thrust or active braking, never on its own.
		// This is the intentionally "clunky" placeholder feel the roadmap
		// calls for; real tuning (drag, max speed, mass-based acceleration)
		// happens once we're tuning against the reference Earth's physics
		// baseline, not now.
		Position += _velocity * dt;
	}
}
