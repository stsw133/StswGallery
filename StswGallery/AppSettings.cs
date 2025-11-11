using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StswGallery;

public class AppSettings
{
    public List<ShortcutSetting> Shortcuts { get; set; } = [];
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
    private const string UserSettingsFileName = "appsettings.user.json";

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
        EnsureShortcutDefaults(Current);
        return Current.Shortcuts.First(s => s.Number == number);
    }

    /// Save
    public static void Save()
    {
        EnsureShortcutDefaults(Current);

        var basePath = AppContext.BaseDirectory;
        var userFilePath = Path.Combine(basePath, UserSettingsFileName);

        Directory.CreateDirectory(Path.GetDirectoryName(userFilePath)!);

        var json = JsonSerializer.Serialize(Current, SerializerOptions);
        File.WriteAllText(userFilePath, json);

        Reload();
    }

    /// Load
    private static AppSettings Load()
    {
        var basePath = AppContext.BaseDirectory;
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(AppSettingsFileName, optional: false, reloadOnChange: false)
            .AddJsonFile(UserSettingsFileName, optional: true, reloadOnChange: false);

        var configuration = builder.Build();
        var settings = configuration.Get<AppSettings>() ?? new AppSettings();

        EnsureShortcutDefaults(settings);
        return settings;
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
}
