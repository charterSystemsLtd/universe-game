using Godot;

namespace UniverseGame;

// Temporary debug UI for the DisplaySettings autoload - sliders for
// internal resolution width/height, a fullscreen checkbox. Deliberately
// built as a thin, self-contained layer over DisplaySettings rather than
// holding any real logic itself: every control here just reads/calls the
// Autoload directly, so relocating these controls into a real settings
// menu later is just moving this scene's contents into a new one - the
// underlying DisplaySettings system doesn't change at all.
//
// A CanvasLayer root (rather than a plain Control) keeps this UI fixed on
// screen in its own render layer, unaffected by the game world's camera
// or the low internal-resolution scaling applied to gameplay content.
public partial class DisplaySettingsUI : CanvasLayer
{
    private DisplaySettings _settings;
    private HSlider _widthSlider;
    private HSlider _heightSlider;
    private CheckBox _fullscreenCheck;
    private Label _resolutionLabel;

    public override void _Ready()
    {
        // GetNode with an absolute "/root/..." path reaches the Autoload
        // directly - Autoloads live under the scene tree's root, outside
        // whatever scene is currently active, which is exactly why
        // they're reachable from anywhere without being passed in.
        _settings = GetNode<DisplaySettings>("/root/DisplaySettings");

        _widthSlider = GetNode<HSlider>("Panel/VBoxContainer/WidthSlider");
        _heightSlider = GetNode<HSlider>("Panel/VBoxContainer/HeightSlider");
        _fullscreenCheck = GetNode<CheckBox>("Panel/VBoxContainer/FullscreenCheck");
        _resolutionLabel = GetNode<Label>("Panel/VBoxContainer/ResolutionLabel");

        _widthSlider.Value = _settings.InternalWidth;
        _heightSlider.Value = _settings.InternalHeight;
        _fullscreenCheck.ButtonPressed = _settings.Fullscreen;
        UpdateLabel();

        _widthSlider.ValueChanged += _ => OnResolutionChanged();
        _heightSlider.ValueChanged += _ => OnResolutionChanged();
        _fullscreenCheck.Toggled += OnFullscreenToggled;
    }

    private void OnResolutionChanged()
    {
        int width = (int)_widthSlider.Value;
        int height = (int)_heightSlider.Value;
        _settings.ApplyInternalResolution(width, height);
        UpdateLabel();
    }

    private void OnFullscreenToggled(bool pressed)
    {
        _settings.ApplyFullscreen(pressed);
    }

    private void UpdateLabel()
    {
        _resolutionLabel.Text = $"Internal resolution: {_settings.InternalWidth} x {_settings.InternalHeight}";
    }
}
