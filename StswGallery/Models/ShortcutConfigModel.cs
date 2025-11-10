using System.Windows.Media;

namespace StswGallery;
public class ShortcutConfigModel(int number, Geometry configIcon, Geometry buttonIcon) : StswObservableObject
{
    public int Number { get; } = number;
    public Geometry ConfigIcon { get; } = configIcon;
    public Geometry ButtonIcon { get; } = buttonIcon;

    public int Type
    {
        get => (int)Properties.Settings.Default[$"Shortcut{Number}Type"];
        set
        {
            var current = (int)Properties.Settings.Default[$"Shortcut{Number}Type"];
            if (current == value)
                return;

            Properties.Settings.Default[$"Shortcut{Number}Type"] = value;
            OnPropertyChanged(nameof(Type));
        }
    }

    public string? Value
    {
        get => Properties.Settings.Default[$"Shortcut{Number}Value"] as string;
        set
        {
            var current = Properties.Settings.Default[$"Shortcut{Number}Value"] as string;
            if (current == value)
                return;

            Properties.Settings.Default[$"Shortcut{Number}Value"] = value;
            OnPropertyChanged(nameof(Value));
        }
    }
}
