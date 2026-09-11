using Godot;

namespace UniverseGame;

// The core Item type — roadmap-v2 step 3 (character inventory + a
// handful of basic items). Follows the shape already decided in the
// Item & Crafting Systems design doc: "composition over deep inheritance"
// - a lean core holding only what's universally true of every item, with
// capability interfaces (IPlaceable, ICraftable, IEquippable, etc.)
// attached later, per item, only where they apply. None of those
// capabilities are needed yet for the first two items (a mining tool and
// a crafting tool are both just "exist, sit in a slot, aren't stackable")
// - they get added as real capabilities are actually built, not
// speculatively now.
//
// A [GlobalClass] Resource rather than a plain C# class - this is
// deliberately a real Godot data asset (a `.tres` file per item, created/
// edited in the Inspector), not something instantiated in code. That
// makes items data-driven the way the design doc's "composition
// reference... from the Unified Compositional Model registry" intends,
// even though that registry link isn't wired up yet (see CompositionId
// below) - the shape is ready for it without needing a rewrite later.
[GlobalClass]
public partial class Item : Resource
{
    // Stable internal identity, independent of DisplayName (which is
    // allowed to change - flavor text, localization, renames - without
    // breaking anything that references this item by Id, e.g. save data
    // or recipe definitions once those exist).
    [Export] public string Id = "";

    [Export] public string DisplayName = "";
    [Export(PropertyHint.MultilineText)] public string Description = "";

    // Inventory icon - left null for now (no item art exists yet, same
    // "placeholder now, real art later" status as most of this project's
    // visuals). Inventory slot rendering (once a real UI exists) needs to
    // handle a null Icon gracefully rather than assuming one's always set.
    [Export] public Texture2D Icon;

    // Per-item stack behavior - Xander's own framing: "we'll decide item
    // stacking as we create new items," i.e. this lives on the item
    // itself, not as a rule Inventory hardcodes. MaxStackSize = 1 is the
    // correct default for tools (mining tool, crafting tool) - a
    // reasonable material/resource item would override this upward once
    // one exists.
    [Export] public int MaxStackSize = 1;

    // TODO(!architecture): CompositionId - which element/compound from
    // the Unified Compositional Model registry this item derives from
    // (Item & Crafting Systems doc: "composition reference... lets
    // density, flammability, hardness, etc. come from the same registry
    // driving planet chemistry"). Not added yet because that registry
    // doesn't exist in code yet either - real field to add once it does,
    // not before (an empty/unused field on every item now would just be
    // dead weight until there's something on the other end to point at).
}
