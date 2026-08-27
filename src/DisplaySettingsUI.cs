using Godot;

namespace UniverseGame;

// Resolution-preset picker UI over the DisplaySettings Autoload.
// Deliberately a dropdown (OptionButton) over a preset list, not a
// continuous slider - a live-adjusted slider risks the resolution
// changing out from under the slider's own container mid-drag (the
// container is itself subject to the same content-scale transform it's
// controlling), which is exactly the kind of self-referential weirdness
// a fixed preset list avoids entirely.
//
// Deliberately thin: every control here just reads/calls the Autoload
// directly, no logic of its own - relocating this into a real settings
// menu later is just moving this scene's contents, the underlying
// DisplaySettings system doesn't change at all.
//
// Fullscreen checkbox removed 2026-08-27 - wasn't actually doing anything
// observable, root cause not diagnosed yet. DisplaySettings.ApplyFullscreen()
// still exists and works as a method; just nothing in this UI calls it
// right now. Real fix (a working toggle) deferred - see project reminders.
public partial class DisplaySettingsUI : CanvasLayer
{
    private DisplaySettings _settings;
    private OptionButton _resolutionOption;

    public override void _Ready()
    {
        _settings = GetNode<DisplaySettings>("/root/DisplaySettings");

        _resolutionOption = GetNode<OptionButton>("Panel/VBoxContainer/ResolutionOption");

        _resolutionOption.Clear();
        int selectedIndex = 0;
        for (int i = 0; i < DisplaySettings.ResolutionPresets.Count; i++)
        {
            Vector2I preset = DisplaySettings.ResolutionPresets[i];
            _resolutionOption.AddItem($"{preset.X} x {preset.Y}");
            if (preset == _settings.InGameResolution)
            {
                selectedIndex = i;
            }
        }
        _resolutionOption.Selected = selectedIndex;

        _resolutionOption.ItemSelected += OnResolutionSelected;
    }

    private void OnResolutionSelected(long index)
    {
        Vector2I preset = DisplaySettings.ResolutionPresets[(int)index];
        _settings.ApplyInGameResolution(preset);
    }
}
