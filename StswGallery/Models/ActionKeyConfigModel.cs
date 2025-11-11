using System;
using System.Windows.Input;

namespace StswGallery;
public class ActionKeyConfigModel : StswObservableObject
{
    public ActionKeyConfigModel(ActionKeyType action, string displayName)
    {
        Action = action;
        DisplayName = displayName;

        AppSettingsService.SettingsChanged += OnSettingsChanged;
    }

    public ActionKeyType Action { get; }
    public string DisplayName { get; }

    public Key Key
    {
        get => AppSettingsService.GetActionKeySetting(Action).Key;
        set
        {
            var actionKey = AppSettingsService.GetActionKeySetting(Action);
            if (actionKey.Key == value)
                return;

            actionKey.Key = value;
            OnPropertyChanged(nameof(Key));
        }
    }

    /// OnSettingsChanged
    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Key));
    }
}