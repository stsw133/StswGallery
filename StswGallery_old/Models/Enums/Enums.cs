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

/// ActionKeyType
public enum ActionKeyType
{
    [Description("Refresh")]
    Refresh = 0,

    [Description("Select directory")]
    SelectDirectory = 1,

    [Description("Remove file")]
    RemoveFile = 2,

    [Description("Previous file")]
    PreviousFile = 3,

    [Description("Next file")]
    NextFile = 4,
    
    [Description("First file")]
    FirstFile = 5,

    [Description("Last file")]
    LastFile = 6,

    [Description("Random file")]
    RandomFile = 7,

    [Description("Rotate left")]
    RotateLeft = 8,

    [Description("Rotate right")]
    RotateRight = 9,
}
