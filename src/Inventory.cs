using Godot;

namespace UniverseGame;

// A generic, reusable inventory component - roadmap-v2 step 3. Built as a
// plain attachable Node rather than something baked into Character
// specifically, per Xander's own framing: "Inventories may be assigned to
// other items, not just the player... may be assigned to other items" -
// a storage unit, a barrel, a ship's cargo hold all need the exact same
// slot/stacking mechanism the player's own inventory uses, just with a
// different SlotCount and a different owner. Attach this node to
// anything that needs an inventory; nothing here assumes it's on a
// Character.
//
// Slot count is expressed as Rows x Columns (3x10 = 30 for the player,
// per Xander's spec) rather than a single flat SlotCount int - keeps the
// grid shape available for whenever a real UI gets built (it'll want to
// lay slots out in a grid, not a flat list), without having to derive
// rows/columns back out of a plain count later.
//
// Per-slot stacking is entirely item-driven, not hardcoded here - "an
// inventory slot can contain an amount of items specified by an
// undefined item's own property tags" (Xander's own framing, matches the
// design doc's existing "stack behavior lives on Item" decision). This
// class only ever reads Item.MaxStackSize; it has no opinion of its own
// about how any particular item stacks.
public partial class Inventory : Node
{
    [Export] public int Rows = 3;
    [Export] public int Columns = 10;

    public int SlotCount => Rows * Columns;

    // Null entry = empty slot. Fixed-size array sized from Rows/Columns
    // in _Ready, once those Inspector-set values are actually available -
    // building it in a field initializer would run before Rows/Columns
    // are read from the scene, and would lock in the wrong (default)
    // size.
    private ItemStack[] _slots;

    // Fired whenever slot contents change (add/remove, from any source) -
    // nothing consumes this yet since no inventory UI exists, but it's
    // the correct hook for one to subscribe to later rather than a UI
    // having to poll every frame.
    [Signal] public delegate void InventoryChangedEventHandler();

    public override void _Ready()
    {
        _slots = new ItemStack[SlotCount];
    }

    public ItemStack GetSlot(int index) => _slots[index];

    // Attempts to add `quantity` of `item`, filling existing partial
    // stacks of the same item first (so picking up 3 more mining tools
    // when a slot already has 2/5 tops that slot up before opening a new
    // one), then spilling into empty slots, each capped at the item's own
    // MaxStackSize. Returns how much could NOT be placed (0 = fully
    // absorbed, >0 means the inventory is full and the caller needs to
    // decide what happens to the remainder - drop it, block the pickup,
    // etc. - not this method's concern).
    public int TryAddItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0) return quantity;

        // Pass 1: top up existing stacks of the same item.
        for (int i = 0; i < _slots.Length && quantity > 0; i++)
        {
            ItemStack stack = _slots[i];
            if (stack == null || stack.Item != item) continue;

            int canAbsorb = Mathf.Min(quantity, stack.RemainingCapacity);
            stack.Quantity += canAbsorb;
            quantity -= canAbsorb;
        }

        // Pass 2: spill remaining quantity into empty slots as new
        // stacks.
        for (int i = 0; i < _slots.Length && quantity > 0; i++)
        {
            if (_slots[i] != null) continue;

            int newStackAmount = Mathf.Min(quantity, item.MaxStackSize);
            _slots[i] = new ItemStack(item, newStackAmount);
            quantity -= newStackAmount;
        }

        EmitSignal(SignalName.InventoryChanged);
        return quantity;
    }

    // Removes up to `quantity` of `item` from wherever it's actually
    // sitting (may span multiple slots) - the general-purpose removal a
    // future crafting/consuming system needs ("spend 3 wood," not "spend
    // whatever's in slot 7"). Returns how much was actually removed,
    // which may be less than requested if the inventory doesn't hold
    // that much.
    public int RemoveItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0) return 0;

        int removed = 0;
        for (int i = 0; i < _slots.Length && removed < quantity; i++)
        {
            ItemStack stack = _slots[i];
            if (stack == null || stack.Item != item) continue;

            int takeFromSlot = Mathf.Min(quantity - removed, stack.Quantity);
            stack.Quantity -= takeFromSlot;
            removed += takeFromSlot;

            if (stack.Quantity <= 0) _slots[i] = null;
        }

        if (removed > 0) EmitSignal(SignalName.InventoryChanged);
        return removed;
    }

    // Slot-specific removal - the other real use case ("drop/move
    // whatever's physically in this exact slot," e.g. drag-and-drop in a
    // future UI, independent of which item happens to be there).
    public void RemoveFromSlot(int index, int quantity)
    {
        ItemStack stack = _slots[index];
        if (stack == null) return;

        stack.Quantity -= quantity;
        if (stack.Quantity <= 0) _slots[index] = null;

        EmitSignal(SignalName.InventoryChanged);
    }

    // How many of `item` this inventory currently holds in total, summed
    // across every slot it's spread across - needed for crafting-style
    // "do I have enough" checks once those exist.
    public int CountItem(Item item)
    {
        int total = 0;
        foreach (ItemStack stack in _slots)
        {
            if (stack != null && stack.Item == item) total += stack.Quantity;
        }
        return total;
    }
}
