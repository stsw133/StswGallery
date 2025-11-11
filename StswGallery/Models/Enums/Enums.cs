using System.ComponentModel;

namespace StswGallery;

/// ShortcutType
public enum ShortcutType
{
    [Description("None")]
    None = 0,

    [Description("Move to")]
    MoveTo = 1,

    [Description("Open with")]
    OpenWith = 2
}
