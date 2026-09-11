using Godot;
using System.Collections.Generic;

namespace UniverseGame;

// The actual player-facing way to view the Item/Inventory system built
// earlier today - until now, Inventory only existed as an underlying data
// structure with no way to actually see it in play. Bound to
// "open_inventory" (E, see project.godot), toggles a simple text-only
// grid display of every inventory Character.GetAccessibleInventories()
// currently returns - the player's own, always, plus whatever
// environment inventory is currently reachable (nothing has one yet -
// this will start showing a second section automatically the moment a
// Ship/storage-unit/etc. gets an Inventory child added, no UI changes
// needed).
//
// Deliberately text-only (item name + quantity, no icons) - every Item's
// Icon is still null (no item art exists yet), same "function before
// polish" status as most of this project's current UI. Slot cells are
// built procedurally each refresh rather than hand-authored in the
// .tscn, since inventory size varies (30 for the player, something
// different for whatever gets an Inventory next) - hardcoding 30 Panel/
// Label pairs in the scene file would silently stop matching reality the
// moment a differently-sized inventory needs showing.
public partial class InventoryUI : CanvasLayer
{
    private VBoxContainer _sectionsContainer;

    public override void _Ready()
    {
        _sectionsContainer = GetNode<VBoxContainer>("Panel/ScrollContainer/VBoxContainer");
        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("open_inventory") || @event.IsEcho()) return;

        Visible = !Visible;
        if (Visible) Refresh();
    }

    // Also refresh while already open and an inventory changes underneath
    // us (e.g. picking something up mid-browse) - subscribed/unsubscribed
    // per-open in Refresh rather than once in _Ready, since which
    // Inventory instances exist to listen to changes each time the
    // current avatar/environment does.
    private void Refresh()
    {
        foreach (Node child in _sectionsContainer.GetChildren())
        {
            child.QueueFree();
        }

        var environmentManager = GetNodeOrNull<EnvironmentManager>("/root/EnvironmentManager");
        Character character = environmentManager?.CurrentAvatar as Character;
        if (character == null)
        {
            _sectionsContainer.AddChild(new Label { Text = "No character to show an inventory for." });
            return;
        }

        List<Inventory> inventories = character.GetAccessibleInventories();
        for (int i = 0; i < inventories.Count; i++)
        {
            Inventory inventory = inventories[i];
            string title = i == 0 ? "Inventory" : $"{inventory.GetParent().Name}'s Inventory";
            BuildSection(title, inventory);
        }
    }

    private void BuildSection(string title, Inventory inventory)
    {
        _sectionsContainer.AddChild(new Label { Text = title });

        var grid = new GridContainer { Columns = inventory.Columns };
        _sectionsContainer.AddChild(grid);

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            ItemStack stack = inventory.GetSlot(i);

            var slotPanel = new Panel();
            slotPanel.CustomMinimumSize = new Vector2(48, 48);

            var slotLabel = new Label
            {
                Text = stack == null ? "" : $"{stack.Item.DisplayName}\nx{stack.Quantity}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            slotLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            slotPanel.AddChild(slotLabel);

            grid.AddChild(slotPanel);
        }
    }
}
