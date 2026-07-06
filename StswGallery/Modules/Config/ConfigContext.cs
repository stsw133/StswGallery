using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StswGallery;
public partial class ConfigContext : BaseContext
{
    public ConfigContext()
    {
        Settings = AppSettingsService.Current;
    }

    /// Save
    [StswCommand]
    async Task Save()
    {
        var validationMessage = ValidateSettings();
        if (!string.IsNullOrEmpty(validationMessage))
        {
            await StswMessageDialog.Show(new InvalidOperationException(validationMessage), "Invalid configuration");
            return;
        }

        AppSettingsService.Save();
        StswContentDialog.Close("ConfigContentDialog");
    }

    /// Cancel
    [StswCommand]
    void Cancel()
    {
        AppSettingsService.Reload();
        StswContentDialog.Close("ConfigContentDialog");
    }

    /// ValidateSettings
    private string? ValidateSettings()
    {
        var duplicatedKeys = Settings.ActionKeys
            .Where(x => x.Key != Key.None)
            .GroupBy(x => x.Key)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.ToString())
            .ToList();

        if (duplicatedKeys.Count > 0)
            return $"Duplicated application shortcut keys: {string.Join(", ", duplicatedKeys)}";

        foreach (var shortcut in Settings.Shortcuts.Where(x => x.Type != ShortcutType.None))
        {
            if (string.IsNullOrWhiteSpace(shortcut.TargetPath))
                return $"Shortcut {shortcut.Number} has no target path.";

            if (shortcut.Type is ShortcutType.MoveTo or ShortcutType.CopyTo)
            {
                if (!Directory.Exists(shortcut.TargetPath) && !shortcut.CreateTargetDirectory)
                    return $"Shortcut {shortcut.Number} target directory does not exist: {shortcut.TargetPath}";
            }
        }

        return null;
    }

    /// GetEnumValuesWithDescription
    private static List<StswComboItem> GetEnumValuesWithDescription<T>() where T : Enum
    {
        var enumType = typeof(T);

        if (!enumType.IsEnum)
            throw new ArgumentException("T must be an enumerated type");

        var values = Enum.GetValues(enumType).Cast<T>();

        var enumList = new List<StswComboItem>();

        foreach (var value in values)
        {
            var memberInfo = enumType.GetMember(value.ToString());
            if (memberInfo.Length > 0)
            {
                var description = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .FirstOrDefault() is DescriptionAttribute descriptionAttribute
                        ? descriptionAttribute.Description
                        : value.ToString();

                enumList.Add(new() { Value = value, Display = description });
            }
        }

        return enumList;
    }

    [StswObservableProperty] AppSettings _settings = null!;
    [StswObservableProperty] List<StswComboItem> _shortcutTypes = GetEnumValuesWithDescription<ShortcutType>();
    [StswObservableProperty] List<StswComboItem> _fileConflictModes = GetEnumValuesWithDescription<FileConflictMode>();
    [StswObservableProperty] List<StswComboItem> _afterShortcutActions = GetEnumValuesWithDescription<AfterShortcutAction>();

    public List<Key> AvailableKeys { get; } = [.. Enum.GetValues<Key>()
        .Where(k => k != Key.None)
        .OrderBy(k => k.ToString())
        .Prepend(Key.None)];
}
