using Godot;
using System.Collections.Generic;

namespace UniverseGame;

// Autoload singleton (see project.godot's [autoload] section — same
// slot DisplaySettings already occupies). Has to outlive every
// individual environment scene, since it owns the stack of what's
// currently expanded — no single environment scene can own that without
// destroying itself when it's the thing being parked.
//
// Closest Java comparison: a Spring-style singleton bean that outlives
// the request-scoped objects passing through it — except Godot wires
// this up declaratively via project.godot rather than an annotation.
//
// Full design trail (stack-not-tree reasoning, Load/Unload vs.
// Collapse/Expand as independent axes, the fog-transition idea,
// per-layer avatars) lives in Universe Game.md's decisions log,
// 2026-08-29 onward — worth reading there before touching this file,
// since almost every design choice below was arrived at by ruling
// something else out first, not obvious in isolation from the code
// alone.
public partial class EnvironmentManager : Node
{
    // One "spine" from root to wherever the player currently is — NOT
    // the whole universe's containment tree. Matches the whole point of
    // the Containment Collapse Model: unobserved environments (Planet A
    // producing iron while we're on Planet C) don't live here at all.
    // They live in a separate registry that doesn't exist yet:
    //
    // TODO(!architecture): PlanetRegistry (or similar name) — holds
    // every generated planet/entity's collapsed state (rate +
    // last-updated-tick) independent of this stack, so Planet A can
    // keep "producing" on paper while the player is on Planet C without
    // EnvironmentManager ever instantiating it. EnvironmentLayer's
    // CollapsedData field is the hook into this registry, not the
    // registry itself.
    //
    // TODO(!architecture): Scheduled Event Queue — the concrete
    // mechanism the registry above needs. A priority queue (min-heap
    // keyed by trigger tick) for anything whose state change is a
    // deterministic function of a linear rate against a bound (a rig
    // depleting a ground patch, a storage unit filling, a ship's
    // transit time) — solve for the exact future tick the bound is hit,
    // schedule one callback there, run nothing in between. See the
    // mining-rig worked example in Universe Game.md, 2026-08-31.
    private Stack<EnvironmentLayer> _stack = new();

    // Test-harness + external-query convenience. Not meant to become a
    // real gameplay API surface, just enough for EnvironmentTestBootstrap
    // (and anything else that needs to know "what am I currently
    // controlling, and what's around it") to work without duplicating
    // stack internals outside this class.
    public int StackCount => _stack.Count;
    public Node2D CurrentAvatar => _stack.Count > 0 ? _stack.Peek().Avatar : null;
    public Node2D CurrentInstance => _stack.Count > 0 ? _stack.Peek().Instance : null;

    // Signals — Godot's built-in pub/sub mechanism. Closest Java
    // comparison: declaring an event/listener interface and firing it,
    // except Godot wires "who's listening" up either in the editor
    // (drag a connection in the Inspector) or via Connect() in code —
    // no interface to implement, any method with a matching signature
    // can subscribe. A future fog controller subscribes to both of
    // these rather than EnvironmentManager needing to know fog exists.
    [Signal] public delegate void LayerLoadingEventHandler(); // fires the instant a new Loaded layer is still Collapsed — the fog's cue to appear.
    [Signal] public delegate void LayerReadyEventHandler();   // fires once that layer's Expand (RecalculateOnEntry) finishes — the fog's cue to fade.

    // Push/extend. Instantiates newScene, spawns avatarScene as this
    // layer's own Avatar (if given), and hides+disables whatever was
    // previously on top — NOT destroying it, just suspending it, so
    // nothing about it (position, facing, in-progress state) is lost.
    // Deliberately does NOT touch the previous top layer's
    // Collapse/Expand state — a layer doesn't lose its data-liveness
    // just because it's no longer the top of the stack, only because
    // the player structurally leaves it (Unload). This matters for
    // cases like a firefight continuing on a planet surface while the
    // player has their star chart open — opening a UI must never be
    // able to double as a way to go invincible.
    public void LoadEnvironment(PackedScene newScene, PackedScene avatarScene = null)
    {
        if (_stack.Count > 0)
        {
            SetLayerRenderActive(_stack.Peek(), false);
        }

        Node2D instance = newScene.Instantiate<Node2D>();
        // CallDeferred rather than a direct AddChild: LoadEnvironment
        // can legitimately be called from another node's own _Ready()
        // (as EnvironmentTestBootstrap does), and the scene tree root
        // is still mid-setup on the very first frame — a synchronous
        // AddChild on it fails with "Parent node is busy setting up
        // children." Deferring is always safe, not just a first-frame
        // workaround, so it's the default here rather than a special
        // case. Building the rest of instance's subtree (EntryPoint
        // lookup, spawning the avatar into it below) doesn't need
        // instance to already be live in the tree — Godot allows
        // assembling node hierarchies off-tree freely; only the actual
        // insertion into the busy root was the problem.
        GetTree().Root.CallDeferred(Node.MethodName.AddChild, instance);

        Marker2D entryPoint = instance.GetNodeOrNull<Marker2D>("EntryPoint");
        Vector2 entryPos = entryPoint?.Position ?? Vector2.Zero;

        var newLayer = new EnvironmentLayer { Scene = newScene, Instance = instance, AvatarScene = avatarScene };

        if (avatarScene != null)
        {
            newLayer.Avatar = SpawnAvatar(avatarScene, instance, entryPos);
        }

        _stack.Push(newLayer);

        // newLayer.IsCollapsed defaults to true (EnvironmentLayer's own
        // default) — meaning the instant a layer is Loaded, it's also
        // still Collapsed. That combination *is* the fog state, with no
        // extra bookkeeping needed to represent it — it just falls out
        // of Load/Unload and Collapse/Expand being independent axes.
        EmitSignal(SignalName.LayerLoading);
        ExpandLayer(newLayer);
    }

    // Pop/collapse. Captures the leaving layer's live state (Collapse),
    // destroys its instance (and, as its child, its Avatar — correct,
    // since that layer is genuinely gone, not just hidden), and
    // reactivates whatever's now on top.
    public void UnloadEnvironment()
    {
        if (_stack.Count == 0) return;

        var leaving = _stack.Pop();
        CollapseLayer(leaving);
        leaving.Instance?.QueueFree();

        if (_stack.Count > 0)
        {
            var reentering = _stack.Peek();
            // Almost always already Expanded here — LoadEnvironment no
            // longer force-collapses the previous top when pushing a
            // new layer on top of it, so whatever the player left
            // behind was still live the whole time they were away.
            // ExpandLayer no-ops safely if it's already Expanded.
            ExpandLayer(reentering);
            SetLayerRenderActive(reentering, true);
        }
    }

    // Instantiates a layer's Avatar as a direct child of that layer's
    // own environment Instance — deliberately NOT reparented in from
    // elsewhere, unlike the old single-global-Player design. This is
    // what makes "hide the layer" and "hide its avatar" the same
    // operation for free, and what lets Ship stay parked in Space
    // exactly where you left it while Character walks around on the
    // ground below, and vice versa.
    private Node2D SpawnAvatar(PackedScene avatarScene, Node2D environmentInstance, Vector2 position)
    {
        Node2D avatar = avatarScene.Instantiate<Node2D>();
        environmentInstance.AddChild(avatar);
        avatar.Position = position;

        // Deferred for the same reason instance's own AddChild above is:
        // MakeCurrent() requires the camera to already be inside the
        // live scene tree, but environmentInstance (avatar's parent)
        // was just queued for deferred insertion a moment ago, not
        // actually in the tree yet at this point in the call. Caught
        // via the headless boot check (2026-08-31) — this would have
        // silently thrown on every single Load, not just the first one.
        Camera2D camera = avatar.GetNodeOrNull<Camera2D>("Camera2D");
        camera?.CallDeferred(Camera2D.MethodName.MakeCurrent);

        // TODO(!architecture): direct Character/Planet coupling, added
        // for the first Load/Unload test (2026-08-31) rather than
        // designed properly — EnvironmentManager knowing about specific
        // avatar/environment types breaks the "generic manager" idea
        // the rest of this class holds to. Real fix once more avatar
        // types exist: something like an IEnvironmentAware interface
        // with an OnEnterEnvironment(Node2D) hook that Character/Ship/
        // etc. implement themselves, so this class never needs a
        // type-specific if-check like the one below.
        if (avatar is Character character)
        {
            character.CurrentPlanet = environmentInstance as Planet;
        }

        return avatar;
    }

    // Rendering + processing toggle for a Loaded-but-not-current layer.
    // Deliberately two things at once, not one: Visible alone doesn't
    // stop _Process/_PhysicsProcess from running, which would let a
    // hidden layer's avatar keep responding to input it shares physical
    // keys with (Ship and Character both read WASD) — a real bug caught
    // 2026-08-31 while building this. ProcessMode.Disabled stops that
    // entirely, and is also a genuine CPU saving for a whole hidden
    // subtree, not just a visual fix — this optimization exists to help
    // CPU load, not hinder it, so it needs to actually stop computation,
    // not just stop drawing it.
    //
    // Deliberately separate from Collapse/Expand (IsCollapsed) — this
    // toggle is purely "is this being rendered/interacted with right
    // now," independent of whether the layer's data is being tracked at
    // real event granularity or collapsed to passive math. A layer can
    // be render-inactive and still Expanded (the bug-army-while-star-
    // chart-open case from earlier design work), even though nothing in
    // this test harness exercises that combination yet.
    private void SetLayerRenderActive(EnvironmentLayer layer, bool active)
    {
        layer.Instance.Visible = active;
        layer.Instance.ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
    }

    // Sustained Expand — for a layer the player is actually Loaded
    // into. Runs the catch-up math, marks it live, and only THEN
    // signals ready. This is deliberately not instant even though the
    // method body below has nothing slow in it yet — RecalculateOnEntry
    // is where real, possibly-slow catch-up work (a populated planet's
    // worth of entities unparking) will eventually live, and the fog
    // stays up for exactly as long as that call takes.
    //
    // TODO(!architecture): this is the SUSTAINED variant only — stays
    // Expanded until CollapseLayer is explicitly called (on Unload). A
    // second, separate variant is needed later: a MOMENTARY Expand for
    // an off-stack system reacting to a single Scheduled Event Queue
    // firing (e.g. a mining rig's storage filling up), which recomputes
    // a new steady state and then re-Collapses itself immediately
    // after — never staying live longer than the single event it woke
    // up to handle. That variant doesn't operate on stack layers at
    // all, won't live on EnvironmentManager, and needs the Scheduled
    // Event Queue above to exist first. Structurally similar to this
    // method otherwise — it'll just need a clean way of self-collapsing
    // once its recompute finishes, per Xander's own framing.
    private void ExpandLayer(EnvironmentLayer layer)
    {
        if (!layer.IsCollapsed) return;

        layer.RecalculateOnEntry();
        layer.IsCollapsed = false;
        EmitSignal(SignalName.LayerReady);
    }

    // Collapse — captures whatever CollapsedData needs to remember
    // before the layer's live state goes away (its Instance is about to
    // be destroyed by the caller). Stub until the production/depletion
    // system exists to actually have live state worth capturing.
    private void CollapseLayer(EnvironmentLayer layer)
    {
        // TODO(!architecture): snapshot live entity/system state (rig
        // output rates, storage levels, elapsed context) into
        // layer.CollapsedData here, before Instance is destroyed.
        layer.IsCollapsed = true;
    }
}
