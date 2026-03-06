using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace StswGallery;
public partial class ConfigContext : BaseContext
{
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

                enumList.Add(new() { Value = (int)(object)value, Display = description });
            }
        }

        return enumList;
    }

    /// GetEnumDescription
    private static string GetEnumDescription(Enum value)
    {
        var memberInfo = value.GetType().GetMember(value.ToString());
        if (memberInfo.Length > 0)
        {
            if (memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() is DescriptionAttribute descriptionAttribute)
                return descriptionAttribute.Description;
        }

        return value.ToString();
    }



    [StswObservableProperty] List<StswComboItem> _shortcutTypes = GetEnumValuesWithDescription<ShortcutType>();
    [StswObservableProperty] ObservableCollection<ShortcutConfigModel> _shortcuts = [
            new(1, StswIcons.Numeric1Box, StswIcons.Numeric1),
            new(2, StswIcons.Numeric2Box, StswIcons.Numeric2),
            new(3, StswIcons.Numeric3Box, StswIcons.Numeric3),
            new(4, StswIcons.Numeric4Box, StswIcons.Numeric4),
            new(5, StswIcons.Numeric5Box, StswIcons.Numeric5),
            new(6, StswIcons.Numeric6Box, StswIcons.Numeric6),
            new(7, StswIcons.Numeric7Box, StswIcons.Numeric7),
            new(8, StswIcons.Numeric8Box, StswIcons.Numeric8),
            new(9, StswIcons.Numeric9Box, StswIcons.Numeric9),
            new(0, StswIcons.Numeric0Box, StswIcons.Numeric0),
        ];
    [StswObservableProperty] ObservableCollection<ActionKeyConfigModel> _actionKeys = [
            new(ActionKeyType.Refresh, GetEnumDescription(ActionKeyType.Refresh)),
            new(ActionKeyType.SelectDirectory, GetEnumDescription(ActionKeyType.SelectDirectory)),
            new(ActionKeyType.RemoveFile, GetEnumDescription(ActionKeyType.RemoveFile)),
            new(ActionKeyType.PreviousFile, GetEnumDescription(ActionKeyType.PreviousFile)),
            new(ActionKeyType.NextFile, GetEnumDescription(ActionKeyType.NextFile)),
            new(ActionKeyType.FirstFile, GetEnumDescription(ActionKeyType.FirstFile)),
            new(ActionKeyType.LastFile, GetEnumDescription(ActionKeyType.LastFile)),
            new(ActionKeyType.RandomFile, GetEnumDescription(ActionKeyType.RandomFile)),
            new(ActionKeyType.RotateLeft, GetEnumDescription(ActionKeyType.RotateLeft)),
            new(ActionKeyType.RotateRight, GetEnumDescription(ActionKeyType.RotateRight)),
        ];
    public List<Key> AvailableKeys { get; } = [.. Enum.GetValues<Key>()
        .Where(k => k != Key.None)
        .OrderBy(k => k.ToString())
        .Prepend(Key.None)];
}
