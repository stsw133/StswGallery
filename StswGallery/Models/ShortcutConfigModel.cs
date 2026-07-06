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

    public string? TargetPath
    {
        get => AppSettingsService.GetShortcut(Number).TargetPath;
        set
        {
            var shortcut = AppSettingsService.GetShortcut(Number);
            if (shortcut.TargetPath == value)
                return;

            shortcut.TargetPath = value;
            OnPropertyChanged(nameof(TargetPath));
        }
    }

    public string? Arguments
    {
        get => AppSettingsService.GetShortcut(Number).Arguments;
        set
        {
            var shortcut = AppSettingsService.GetShortcut(Number);
            if (shortcut.Arguments == value)
                return;

            shortcut.Arguments = value;
            OnPropertyChanged(nameof(Arguments));
        }
    }

    /// OnSettingsChanged
    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Type));
        OnPropertyChanged(nameof(TargetPath));
        OnPropertyChanged(nameof(Arguments));
    }
}
