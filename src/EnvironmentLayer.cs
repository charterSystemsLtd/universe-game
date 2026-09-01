using Godot;

namespace UniverseGame;

// One layer in the player's current containment chain — e.g. Space,
// then the planet they've landed on, then their ship's cockpit. Tracks
// two genuinely independent things, not one combined state:
//
//   Load/Unload  — physical rendering + the player's position. Governed
//                  by this layer's membership in EnvironmentManager's
//                  stack: Instance is non-null exactly when this layer
//                  is Loaded (on the stack), null when Unloaded (popped
//                  off entirely).
//
//   Collapse/Expand — data/systems liveness. Whether this layer's
//                  contained entities/systems (rigs, storage, NPCs) are
//                  being tracked at real event granularity via the
//                  Scheduled Event Queue (Expanded), or reduced to
//                  passive aggregate math only (Collapsed). Tracked here
//                  as IsCollapsed, independent of whether Instance is
//                  null — a layer can be Loaded and still Collapsed for
//                  a moment (the deliberate "fog" transition while
//                  Expand's catch-up math finishes), and conceptually a
//                  system doesn't need to be Loaded at all to need
//                  Expand — an automated base three planets away stays
//                  Expanded (its scheduled events are real and ticking)
//                  while permanently Unloaded. That off-stack case isn't
//                  reachable yet — nothing outside the stack holds an
//                  EnvironmentLayer reference to call Collapse/Expand on
//                  until the CCM registry (still just a TODO elsewhere
//                  in this file) actually exists to hold one.
//
// Design settled 2026-08-31 across a full conversation with Xander —
// see the "EnvironmentManager architecture settled" decisions-log entry
// in Universe Game.md for the full reasoning trail, including the
// mining-rig worked example and the fog-transition idea this class's
// IsCollapsed default (true) directly enables.
public class EnvironmentLayer
{
    public PackedScene Scene;
    public Node2D Instance;

    // The controllable avatar for THIS layer specifically — e.g. Ship
    // for a Space layer, Character for a Ground layer. Added 2026-08-31
    // once it became clear "one Player node reparented everywhere"
    // didn't hold up: you don't carry your on-foot body into your ship,
    // you carry your ship's body into space. Avatar is instantiated as
    // a child of Instance when this layer Loads (see
    // EnvironmentManager.LoadEnvironment), which means hiding/disabling
    // Instance automatically hides/disables Avatar too — no separate
    // tracking needed, and nothing gets destroyed/recreated on a hide,
    // so context (position, facing, whatever else) is never lost across
    // a Load/Unload the way it would be if avatars were recreated fresh
    // each time.
    //
    // TODO(!architecture): Inventory can no longer just "ride along" as
    // a child of a single persistent Player the way it could under the
    // old one-node model — Ship and Character are genuinely separate
    // nodes now, so an inventory attached to one wouldn't be visible to
    // the other. Real fix, not yet designed: Inventory becomes its own
    // persistent object (owned by EnvironmentManager or similar,
    // independent of any one Avatar), referenced by whichever avatar is
    // currently active rather than physically parented to it. Extend
    // further later to link specific storage objects (e.g. a ship's own
    // cargo hold) to this same system, distinct from the personal
    // Inventory itself.
    public PackedScene AvatarScene;
    public Node2D Avatar;

    // Where a layer's Avatar should be positioned if ever needed
    // outside the "avatar just stays put, hidden" model above — e.g. a
    // future multi-site landing system repositioning within a single
    // layer. Not currently read by EnvironmentManager now that avatars
    // persist as hidden children rather than being reparented, but kept
    // rather than deleted since a real use is still likely once return
    // coordinates (see reminder queue — GitHub Issue #9) are designed.
    //
    // TODO(!architecture): still a bare Vector2, same placeholder status
    // as Ship.cs's own Position-only movement. Once real coordinates
    // exist, a layer returning into Universal Scale needs a real
    // universal coordinate and a layer returning onto a planet surface
    // needs a PlanetPosition — not the same shape, so this likely needs
    // to become something more structured than a single Vector2.
    public Vector2 ReturnPosition;

    // Defaults to true: a freshly-created layer starts fully Collapsed
    // (nothing simulating) until something explicitly Expands it —
    // nothing runs until asked to, not the other way around.
    public bool IsCollapsed = true;

    // TODO(!architecture): CollapsedData — the actual per-layer
    // production/depletion state (mining rig output, storage fill,
    // whatever else Expand needs to catch up on) that IsCollapsed=true
    // currently has nowhere real to live. Placeholder hook only, per the
    // not-yet-built CCM registry (see EnvironmentManager.cs).
    public object CollapsedData;

    // TODO(!architecture): render-detail-independent-of-focus. Right
    // now a Loaded layer is either fully rendered or not rendered at
    // all — there's no notion of a *partial* view into a layer the
    // player isn't physically occupying. Real future need: a player
    // standing inside their ship's cockpit (Loaded, focused) looking out
    // a window at the planet surface outside (a *different* Loaded
    // layer, structurally present but not where the player currently
    // is) should be able to see a limited, cropped view of it through
    // the window glass — without that layer needing full entity-level
    // rendering/simulation just because a sliver of it is visible. Same
    // idea the other direction: monitoring a ship's exterior via a hull
    // camera from inside, or eventually the Star Chart rendering a
    // cropped/simplified view into Universal Scale content while the
    // player's actual position stays wherever it structurally is. This
    // was originally going to be handled by an EnvironmentFocusDepth
    // field/slider, deliberately dropped (2026-08-31) since nothing in
    // the codebase consumes it yet — revisit once a real windowed/
    // partial-view feature is actually being built, rather than
    // speculatively building the control now.
    public void RecalculateOnEntry()
    {
        // TODO(!architecture): apply CollapsedData against elapsed
        // ticks to regenerate terrain/building state before this layer
        // goes live again. No-op until the production/depletion system
        // (Item & Crafting Systems doc, ground-depletion section) and
        // the Scheduled Event Queue it feeds into both exist.
    }
}
