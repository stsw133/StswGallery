using Avalonia.Input;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StswGallery;

public enum ShortcutType { None, MoveTo, OpenWith }

public enum ActionKeyType
{
    Refresh,
    SelectDirectory,
    RemoveFile,
    PreviousFile,
    NextFile,
    FirstFile,
    LastFile,
    RandomFile,
    RotateLeft,
    RotateRight
}

public sealed class ActionKeySetting
{
    public ActionKeyType Action { get; set; }
    public string Key { get; set; } = string.Empty;

    public Key ToAvaloniaKey() => Enum.TryParse<Key>(Key, true, out var parsed) ? parsed : Avalonia.Input.Key.None;
}

public sealed class ShortcutSetting
{
    public int Number { get; set; }
    public ShortcutType Type { get; set; }
    public string? Value { get; set; }
}

public sealed class AppSettings
{
    public List<ActionKeySetting> ActionKeys { get; set; } = [];
    public List<ShortcutSetting> Shortcuts { get; set; } = [];
}

public static class AppSettingsService
{
    public static AppSettings Current { get; private set; } = new();

    public static void Reload()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

        var loaded = builder.Build().Get<AppSettings>() ?? new AppSettings();
        EnsureDefaults(loaded);
        Current = loaded;
    }

    public static ShortcutSetting? GetShortcut(int number) => Current.Shortcuts.FirstOrDefault(x => x.Number == number);

    public static ActionKeyType? MapAction(Key key)
    {
        var action = Current.ActionKeys.FirstOrDefault(x => x.ToAvaloniaKey() == key);
        return action?.Action;
    }

    private static void EnsureDefaults(AppSettings settings)
    {
        settings.Shortcuts ??= [];
        settings.ActionKeys ??= [];

        for (var i = 0; i <= 9; i++)
        {
            if (settings.Shortcuts.All(x => x.Number != i))
                settings.Shortcuts.Add(new ShortcutSetting { Number = i, Type = ShortcutType.None, Value = string.Empty });
        }

        foreach (var action in Enum.GetValues<ActionKeyType>())
        {
            if (settings.ActionKeys.Any(x => x.Action == action))
                continue;

            settings.ActionKeys.Add(new ActionKeySetting
            {
                Action = action,
                Key = action switch
                {
                    ActionKeyType.PreviousFile => nameof(Key.Left),
                    ActionKeyType.NextFile => nameof(Key.Right),
                    ActionKeyType.FirstFile => nameof(Key.Home),
                    ActionKeyType.LastFile => nameof(Key.End),
                    ActionKeyType.RandomFile => nameof(Key.Z),
                    ActionKeyType.RotateLeft => nameof(Key.Q),
                    ActionKeyType.RotateRight => nameof(Key.E),
                    ActionKeyType.Refresh => nameof(Key.F5),
                    ActionKeyType.SelectDirectory => nameof(Key.F9),
                    ActionKeyType.RemoveFile => nameof(Key.Delete),
                    _ => nameof(Key.None)
                }
            });
        }
    }
}
