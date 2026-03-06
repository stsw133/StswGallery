using System;
using System.Windows.Media;

namespace StswGallery;
public class ShortcutConfigModel : StswObservableObject
{
    public ShortcutConfigModel(int number, Geometry configIcon, Geometry buttonIcon)
    {
        Number = number;
        ConfigIcon = configIcon;
        ButtonIcon = buttonIcon;

        AppSettingsService.SettingsChanged += OnSettingsChanged;
    }

    public int Number { get; }
    public Geometry ConfigIcon { get; }
    public Geometry ButtonIcon { get; }

    public int Type
    {
        get
        {
            var shortcut = AppSettingsService.GetShortcut(Number);
            return (int)shortcut.Type;
        }
        set
        {
            var shortcut = AppSettingsService.GetShortcut(Number);
            if ((int)shortcut.Type == value)
                return;

            shortcut.Type = (ShortcutType)value;
            OnPropertyChanged(nameof(Type));
        }
    }

    public string? Value
    {
        get => AppSettingsService.GetShortcut(Number).Value;
        set
        {
            var shortcut = AppSettingsService.GetShortcut(Number);
            if (shortcut.Value == value)
                return;

            shortcut.Value = value;
            OnPropertyChanged(nameof(Value));
        }
    }

    /// OnSettingsChanged
    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Type));
        OnPropertyChanged(nameof(Value));
    }
}
