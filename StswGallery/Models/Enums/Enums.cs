using System.ComponentModel;

namespace StswGallery;

/// ShortcutType
public enum ShortcutType
{
    [Description("None")]
    None = 0,

    [Description("Move to folder")]
    MoveTo = 1,

    [Description("Copy to folder")]
    CopyTo = 2,

    [Description("Open with")]
    OpenWith = 3
}

/// FileConflictMode
public enum FileConflictMode
{
    [Description("Rename")]
    Rename = 0,

    [Description("Skip")]
    Skip = 1,

    [Description("Overwrite")]
    Overwrite = 2
}

/// AfterShortcutAction
public enum AfterShortcutAction
{
    [Description("Select next")]
    SelectNext = 0,

    [Description("Select previous")]
    SelectPrevious = 1,

    [Description("Keep index")]
    KeepIndex = 2,

    [Description("Refresh only")]
    RefreshOnly = 3
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
