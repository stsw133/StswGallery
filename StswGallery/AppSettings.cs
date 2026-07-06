using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace StswGallery;

public partial class AppSettings : StswObservableObject
{
    [StswObservableProperty] List<ActionKeySetting> _actionKeys = [];
    [StswObservableProperty] List<ShortcutSetting> _shortcuts = [];
    [StswObservableProperty] bool _useDefaultStretchDirection;
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
    public string? TargetPath { get; set; }
    public string? Arguments { get; set; }
    public FileConflictMode ConflictMode { get; set; } = FileConflictMode.Rename;
    public AfterShortcutAction AfterAction { get; set; } = AfterShortcutAction.SelectNext;
    public bool CreateTargetDirectory { get; set; } = true;

    /// <summary>
    /// Backward compatibility for old appsettings.json files that used Value as the shortcut target.
    /// This property is read by ConfigurationBinder, migrated to TargetPath, and not written back by JsonSerializer.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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

    public static AppSettings Current { get; } = new();
    public static event EventHandler? SettingsChanged;

    static AppSettingsService()
    {
        Reload();
    }

    /// Reload
    public static void Reload()
    {
        var loaded = Load();
        Current.ActionKeys = loaded.ActionKeys;
        Current.Shortcuts = loaded.Shortcuts;
        Current.UseDefaultStretchDirection = loaded.UseDefaultStretchDirection;
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
            var settings = new AppSettings();
            EnsureDefaults(settings);
            return settings;
        }
    }

    /// EnsureShortcutDefaults
    private static void EnsureShortcutDefaults(AppSettings settings)
    {
        settings.Shortcuts ??= [];

        var shortcutsByNumber = settings.Shortcuts
            .Where(s => s.Number is >= 0 and <= 9)
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
                    Type = ShortcutType.None,
                    TargetPath = string.Empty,
                    Arguments = string.Empty,
                    ConflictMode = FileConflictMode.Rename,
                    AfterAction = AfterShortcutAction.SelectNext,
                    CreateTargetDirectory = true
                };
            }
            else
            {
                shortcut.Number = number;

                if (shortcut.TargetPath == null && shortcut.Value != null)
                    shortcut.TargetPath = shortcut.Value;

                shortcut.Value = null;
                shortcut.TargetPath ??= string.Empty;
                shortcut.Arguments ??= string.Empty;
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
        { ActionKeyType.FirstFile, Key.Home },
        { ActionKeyType.LastFile, Key.End },
        { ActionKeyType.RotateLeft, Key.Q },
        { ActionKeyType.RotateRight, Key.E },
        { ActionKeyType.RandomFile, Key.Z },
        { ActionKeyType.Refresh, Key.F5 },
        { ActionKeyType.SelectDirectory, Key.F9 },
        { ActionKeyType.RemoveFile, Key.Delete }
    };
}
