using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    void Save()
    {
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
                string description = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false)
                                                  .FirstOrDefault() is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : value.ToString();

                enumList.Add(new() { Value = value, Display = description });
            }
        }

        return enumList;
    }

    [StswObservableProperty] AppSettings _settings = null!;
    [StswObservableProperty] List<StswComboItem> _shortcutTypes = GetEnumValuesWithDescription<ShortcutType>();
    public List<Key> AvailableKeys { get; } = [.. Enum.GetValues<Key>()
        .Where(k => k != Key.None)
        .OrderBy(k => k.ToString())
        .Prepend(Key.None)];
}
