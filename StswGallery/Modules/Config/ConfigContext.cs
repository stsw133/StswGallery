using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace StswGallery;
internal class ConfigContext : StswObservableObject
{
    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }

    public ConfigContext()
    {
        SaveCommand = new StswCommand(Save);
        CancelCommand = new StswCommand(Cancel);
    }

    #region Events & methods
    /// Command: save
    private void Save()
    {
        Properties.Settings.Default.Save();
        StswContentDialog.Close("Config");
    }

    /// Command: cancel
    private void Cancel()
    {
        Properties.Settings.Default.Reload();
        StswContentDialog.Close("Config");
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
    #endregion

    /// ShortcutTypes
    public List<StswComboItem> ShortcutTypes
    {
        get => _shortcutTypes;
        set => SetProperty(ref _shortcutTypes, value);
    }
    private List<StswComboItem> _shortcutTypes = GetEnumValuesWithDescription<ShortcutType>();
}
