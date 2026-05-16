using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace StswGallery;
public class ActionKeyTypeDescriptionConverter : IValueConverter
{
    public static ActionKeyTypeDescriptionConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ActionKeyType action)
            return value?.ToString() ?? string.Empty;

        var memberInfo = typeof(ActionKeyType).GetMember(action.ToString());
        if (memberInfo.Length == 0)
            return action.ToString();

        return memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() is DescriptionAttribute descriptionAttribute
            ? descriptionAttribute.Description
            : action.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException("ActionKeyTypeDescriptionConverter supports one-way conversion only.");
}
