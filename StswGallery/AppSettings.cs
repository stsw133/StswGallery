using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace StswGallery;

public class AppSettings
{
    public List<ActionKeySetting> ActionKeys { get; set; } = [];
    public List<ShortcutSetting> Shortcuts { get; set; } = [];
}

public class ActionKeySetting
{
    public ActionKeyType Action { get; set; }
    public Key Key { get; set; }
}

public class ShortcutSetting
{
    public int Number { get; set; }
    public ShortcutType Type { get; set; }
    public string? Value { get; set; }
}

public static class AppSettingsService
{
    private const string AppSettingsFileName = "appsettings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppSettings Current { get; private set; } = new();
    public static event EventHandler? SettingsChanged;

    static AppSettingsService()
    {
        Reload();
    }

    /// Reload
    public static void Reload()
    {
        Current = Load();
        SettingsChanged?.Invoke(null, EventArgs.Empty);
    }

    /// GetShortcut
    public static ShortcutSetting GetShortcut(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        EnsureDefaults(Current);
        return Current.Shortcuts.First(s => s.Number == number);
    }

    /// GetActionKeySetting
    public static ActionKeySetting GetActionKeySetting(ActionKeyType action)
    {
        EnsureDefaults(Current);
        return Current.ActionKeys.First(a => a.Action == action);
    }

    /// GetActionKeySettings
    public static IReadOnlyList<ActionKeySetting> GetActionKeySettings()
    {
        EnsureDefaults(Current);
        return Current.ActionKeys;
    }

    /// Save
    public static void Save()
    {
        EnsureDefaults(Current);

        var basePath = AppContext.BaseDirectory;
        var appFilePath = Path.Combine(basePath, AppSettingsFileName);

        Directory.CreateDirectory(Path.GetDirectoryName(appFilePath)!);

        var json = JsonSerializer.Serialize(Current, SerializerOptions);
        File.WriteAllText(appFilePath, json);

        Reload();
    }

    /// Load
    private static AppSettings Load()
    {
        var basePath = AppContext.BaseDirectory;
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(AppSettingsFileName, optional: true, reloadOnChange: false);

        try
        {
            var configuration = builder.Build();
            var settings = configuration.Get<AppSettings>() ?? new AppSettings();

            EnsureDefaults(settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// EnsureShortcutDefaults
    private static void EnsureShortcutDefaults(AppSettings settings)
    {
        settings.Shortcuts ??= [];

        var shortcutsByNumber = settings.Shortcuts
            .GroupBy(s => s.Number)
            .ToDictionary(g => g.Key, g => g.Last());

        var orderedShortcuts = new List<ShortcutSetting>();
        for (var number = 0; number <= 9; number++)
        {
            if (!shortcutsByNumber.TryGetValue(number, out var shortcut))
            {
                shortcut = new ShortcutSetting
                {
                    Number = number,
                    Type = ShortcutType.MoveTo,
                    Value = string.Empty
                };
            }

            orderedShortcuts.Add(shortcut);
        }

        settings.Shortcuts.Clear();
        settings.Shortcuts.AddRange(orderedShortcuts);
    }

    /// EnsureActionKeyDefaults
    private static void EnsureActionKeyDefaults(AppSettings settings)
    {
        settings.ActionKeys ??= [];

        var actionKeysByAction = settings.ActionKeys
            .GroupBy(a => a.Action)
            .ToDictionary(g => g.Key, g => g.Last());

        var orderedActionKeys = new List<ActionKeySetting>();
        foreach (var action in Enum.GetValues<ActionKeyType>())
        {
            if (!actionKeysByAction.TryGetValue(action, out var actionKey))
            {
                actionKey = new ActionKeySetting
                {
                    Action = action,
                    Key = DefaultActionKeys.TryGetValue(action, out var defaultKey) ? defaultKey : Key.None
                };
            }

            orderedActionKeys.Add(actionKey);
        }

        settings.ActionKeys.Clear();
        settings.ActionKeys.AddRange(orderedActionKeys);
    }

    /// EnsureDefaults
    private static void EnsureDefaults(AppSettings settings)
    {
        EnsureShortcutDefaults(settings);
        EnsureActionKeyDefaults(settings);
    }

    private static readonly Dictionary<ActionKeyType, Key> DefaultActionKeys = new()
    {
        { ActionKeyType.PreviousFile, Key.Left },
        { ActionKeyType.NextFile, Key.Right },
        { ActionKeyType.RotateLeft, Key.Q },
        { ActionKeyType.RotateRight, Key.E },
        { ActionKeyType.RandomFile, Key.Z },
        { ActionKeyType.Refresh, Key.F5 },
        { ActionKeyType.SelectDirectory, Key.F9 },
        { ActionKeyType.RemoveFile, Key.Delete }
    };
}
