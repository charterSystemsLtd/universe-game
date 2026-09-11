namespace UniverseGame;

// A runtime quantity of a single Item sitting in one Inventory slot.
// Deliberately a plain C# class, not a Godot Resource/Node - unlike Item
// (a design-time data asset, one .tres per item type), an ItemStack is
// pure runtime state that comes and goes constantly as items are picked
// up/dropped/consumed. Giving it Godot object overhead would be waste for
// something this short-lived and simple.
public class ItemStack
{
    public Item Item;
    public int Quantity;

    public ItemStack(Item item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    // How much more of this stack's own Item type it could still absorb
    // before hitting the Item's own MaxStackSize - Inventory uses this to
    // decide how a new incoming quantity splits across an existing
    // partial stack vs. spilling into a fresh slot.
    public int RemainingCapacity => Item.MaxStackSize - Quantity;
}
