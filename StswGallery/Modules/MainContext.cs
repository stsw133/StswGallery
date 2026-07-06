using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace StswGallery;
public partial class MainContext : BaseContext
{
    private CancellationTokenSource? _repeatRefreshCancellationTokenSource;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static readonly Random _random = new();
    private static readonly StringComparer _pathComparer = StringComparer.OrdinalIgnoreCase;

    private static readonly HashSet<string> _supportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp",
        ".dib",
        ".gif",
        ".ico",
        ".jpe",
        ".jfif",
        ".jpeg",
        ".jpg",
        ".png",
        ".tif",
        ".tiff",
        ".webp"
    };

    private static readonly HashSet<string> _supportedMediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // audio
        ".aac",
        ".aiff",
        ".flac",
        ".m4a",
        ".mid",
        ".midi",
        ".mp3",
        ".oga",
        ".ogg",
        ".opus",
        ".wav",
        ".wma",

        // video
        ".3g2",
        ".3gp",
        ".avi",
        ".m2ts",
        ".m4v",
        ".mkv",
        ".mov",
        ".mp4",
        ".mpeg",
        ".mpg",
        ".ogv",
        ".ts",
        ".webm",
        ".wmv"
    };

    private static bool IsSupportedImageFile(string path) => _supportedImageExtensions.Contains(Path.GetExtension(path));
    private static bool IsSupportedMediaFile(string path) => _supportedMediaExtensions.Contains(Path.GetExtension(path));
    private static bool IsSupportedFile(string path) => IsSupportedImageFile(path) || IsSupportedMediaFile(path);

    [StswObservableProperty] ConfigContext _configContext = new();
    [StswObservableProperty] FileInfoModel _currentFile = new();
    [StswObservableProperty] int _currentFileIndex = -1;
    [StswObservableProperty] string? _currentFilePath;
    [StswObservableProperty] List<string> _directoryFiles = [];
    [StswObservableProperty] string? _directoryPath;
    [StswObservableProperty] ImageSource? _imageSource;
    [StswObservableProperty] bool _isDynamicDisplayMode;
    [StswObservableProperty] bool _isConfigOpen;
    [StswObservableProperty] bool _isMediaFile;
    [StswObservableProperty] bool _isSidePanelLocked;
    [StswObservableProperty] Uri? _mediaSource;
    [StswObservableProperty] string _displayModeText = "Static display mode";
    [StswObservableProperty] bool _useMediaPlayer;

    public string FileCounterText => DirectoryFiles.Count == 0 || CurrentFileIndex < 0
        ? "0 / 0"
        : $"{CurrentFileIndex + 1} / {DirectoryFiles.Count}";

    partial void OnCurrentFileIndexChanged(int oldValue, int newValue) => OnPropertyChanged(nameof(FileCounterText));
    partial void OnDirectoryFilesChanged(List<string> oldValue, List<string> newValue) => OnPropertyChanged(nameof(FileCounterText));

    /// Init
    [StswCommand]
    async Task Init()
    {
        App.Current.Exit += OnApplicationExit;

        var startupFilePath = App.StartupFilePath;
        if (!string.IsNullOrEmpty(startupFilePath) && File.Exists(startupFilePath) && IsSupportedFile(startupFilePath))
        {
            var directoryPath = Path.GetDirectoryName(startupFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                DirectoryPath = directoryPath;
                CurrentFilePath = startupFilePath;
                await RefreshAsync();
            }
        }

        StartRepeatRefresh();
    }

    /// StartRepeatRefresh
    void StartRepeatRefresh()
    {
        CancelRepeatRefresh();
        _repeatRefreshCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(App.CancellationToken);
        _ = RepeatRefresh(_repeatRefreshCancellationTokenSource.Token);
    }

    /// CancelRepeatRefresh
    void CancelRepeatRefresh()
    {
        if (_repeatRefreshCancellationTokenSource == null)
            return;

        if (!_repeatRefreshCancellationTokenSource.IsCancellationRequested)
            _repeatRefreshCancellationTokenSource.Cancel();
        _repeatRefreshCancellationTokenSource.Dispose();
        _repeatRefreshCancellationTokenSource = null;
    }

    /// RepeatRefresh
    async Task RepeatRefresh(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RefreshAsync(cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// OnApplicationExit
    void OnApplicationExit(object? sender, ExitEventArgs e)
    {
        Application.Current.Exit -= OnApplicationExit;
        CancelRepeatRefresh();
    }

    /// SelectDirectory
    [StswCommand]
    async Task SelectDirectory()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        DirectoryPath = dialog.SelectedPath;
        CurrentFilePath = null;
        CurrentFileIndex = 0;

        await RefreshAsync();
    }

    /// Refresh
    [StswCommand]
    async Task Refresh() => await RefreshAsync();

    /// RefreshAsync
    async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!cancellationToken.CanBeCanceled)
            cancellationToken = App.CancellationToken;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!Directory.Exists(DirectoryPath))
            {
                DirectoryFiles = [];
                CurrentFileIndex = -1;
                ClearCurrentFile();
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var previousFilePath = CurrentFilePath;
            var previousIndex = CurrentFileIndex;

            var files = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Directory.EnumerateFiles(DirectoryPath)
                    .Where(IsSupportedFile)
                    .OrderBy(x => x, new StswNaturalStringComparer())
                    .ToList();
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            DirectoryFiles = files;

            if (DirectoryFiles.Count == 0)
            {
                CurrentFileIndex = -1;
                ClearCurrentFile();
                return;
            }

            var existingIndex = !string.IsNullOrEmpty(previousFilePath)
                ? files.FindIndex(x => _pathComparer.Equals(x, previousFilePath))
                : -1;

            var nextIndex = existingIndex >= 0
                ? existingIndex
                : Math.Clamp(previousIndex, 0, DirectoryFiles.Count - 1);

            SelectFileAt(nextIndex);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// LockSidePanel
    [StswCommand]
    void LockSidePanel() => IsSidePanelLocked = !IsSidePanelLocked;

    /// Config
    [StswCommand]
    void Config() => StswContentDialog.Show(ConfigContext, "ConfigContentDialog");

    /// ToggleDisplayMode
    [StswCommand]
    void ToggleDisplayMode()
    {
        IsDynamicDisplayMode = !IsDynamicDisplayMode;

        var path = CurrentFilePath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            LoadCurrentFile(path);
        else
            ClearCurrentFile();
    }

    /// RemoveFile
    [StswCommand]
    async Task RemoveFile()
    {
        var path = CurrentFilePath;
        var index = CurrentFileIndex;

        if (string.IsNullOrEmpty(path))
            return;

        if (!File.Exists(path))
        {
            RemoveFileFromList(path);
            SelectFileAt(index);
            return;
        }

        await ReleaseCurrentPreviewAsync();

        try
        {
            StswFn.MoveToRecycleBin(path);
        }
        catch
        {
            await RefreshAsync();
            return;
        }

        if (!File.Exists(path))
        {
            RemoveFileFromList(path);
            SelectFileAt(index);
        }
        else
        {
            await RefreshAsync();
        }
    }

    /// KeyNumber
    [StswCommand]
    async Task KeyNumber(int? num)
    {
        if (num == null || !num.Value.Between(0, 9))
            return;

        var shortcut = AppSettingsService.GetShortcut(num.Value);
        if (shortcut.Type == ShortcutType.None)
            return;

        switch (shortcut.Type)
        {
            case ShortcutType.MoveTo:
                await MoveCurrentFileToAsync(shortcut);
                break;

            case ShortcutType.CopyTo:
                await CopyCurrentFileToAsync(shortcut);
                break;

            case ShortcutType.OpenWith:
                OpenCurrentFileWith(shortcut);
                break;
        }
    }

    /// KeyPress
    [StswCommand]
    async Task KeyPress(KeyEventArgs? e)
    {
        if (e == null || IsConfigOpen)
            return;

        if (e.Key is >= Key.D0 and <= Key.D9)
        {
            await KeyNumber(e.Key - Key.D0);
            return;
        }

        if (e.Key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            await KeyNumber(e.Key - Key.NumPad0);
            return;
        }

        var actionSetting = AppSettingsService.GetActionKeySettings().FirstOrDefault(s => s.Key == e.Key);
        if (actionSetting == null)
            return;

        Func<Task>? action = actionSetting.Action switch
        {
            ActionKeyType.Refresh => Refresh,
            ActionKeyType.SelectDirectory => SelectDirectory,
            ActionKeyType.RemoveFile => RemoveFile,
            ActionKeyType.PreviousFile => () => { PreviousFile(); return Task.CompletedTask; },
            ActionKeyType.NextFile => () => { NextFile(); return Task.CompletedTask; },
            ActionKeyType.FirstFile => () => { FirstFile(); return Task.CompletedTask; },
            ActionKeyType.LastFile => () => { LastFile(); return Task.CompletedTask; },
            ActionKeyType.RandomFile => () => { RandomFile(); return Task.CompletedTask; },
            ActionKeyType.RotateLeft => () => RotateFlipImage(RotateFlipType.Rotate90FlipNone),
            ActionKeyType.RotateRight => () => RotateFlipImage(RotateFlipType.Rotate270FlipNone),
            _ => null
        };
        if (action == null)
            return;

        await action();
    }

    /// RandomFile
    [StswCommand]
    void RandomFile()
    {
        if (DirectoryFiles.Count == 0)
            return;

        SelectFileAt(_random.Next(DirectoryFiles.Count));
    }

    /// RotateFlipImage
    [StswCommand]
    async Task RotateFlipImage(RotateFlipType rotateFlipType)
    {
        var path = CurrentFilePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path) || !IsSupportedImageFile(path))
            return;

        await ReleaseCurrentPreviewAsync();

        var isRotated = false;

        try
        {
            using var memoryStream = new MemoryStream(File.ReadAllBytes(path));
            using var bitmap = Image.FromStream(memoryStream);
            bitmap.RotateFlip(rotateFlipType);
            bitmap.Save(path, bitmap.RawFormat);
            isRotated = true;
        }
        catch
        {
            isRotated = false;
        }

        if (isRotated)
            LoadCurrentFile(path);
        else
            SelectFileAt(CurrentFileIndex);
    }

    /// PreviousFile
    [StswCommand]
    void PreviousFile() => SelectFileAt(CurrentFileIndex - 1);

    /// NextFile
    [StswCommand]
    void NextFile() => SelectFileAt(CurrentFileIndex + 1);

    /// FirstFile
    [StswCommand]
    void FirstFile() => SelectFileAt(0);

    /// LastFile
    [StswCommand]
    void LastFile() => SelectFileAt(DirectoryFiles.Count - 1);

    /// SelectFileAt
    private void SelectFileAt(int index)
    {
        while (DirectoryFiles.Count > 0)
        {
            CurrentFileIndex = Math.Clamp(index, 0, DirectoryFiles.Count - 1);
            var path = DirectoryFiles[CurrentFileIndex];

            if (File.Exists(path))
            {
                LoadCurrentFile(path);
                return;
            }

            var removedIndex = CurrentFileIndex;
            RemoveFileFromList(path);
            index = Math.Clamp(removedIndex, 0, DirectoryFiles.Count - 1);
        }

        CurrentFileIndex = -1;
        ClearCurrentFile();
    }

    /// LoadCurrentFile
    private void LoadCurrentFile(string path)
    {
        if (!File.Exists(path))
        {
            RemoveFileFromList(path);
            SelectFileAt(CurrentFileIndex);
            return;
        }

        CurrentFilePath = path;
        StswApp.StswWindow.Title = Path.GetFileName(path);

        IsMediaFile = IsSupportedMediaFile(path);
        UpdateDisplayMode();

        ImageSource = null;
        MediaSource = null;

        if (UseMediaPlayer)
        {
            MediaSource = new Uri(path, UriKind.Absolute);
        }
        else
        {
            try
            {
                ImageSource = StswFnUI.BytesToBitmapImage(File.ReadAllBytes(path));
            }
            catch
            {
                ImageSource = null;
            }
        }

        try
        {
            var info = new FileInfo(path);
            CurrentFile = new FileInfoModel
            {
                Name = info.Name,
                Extension = info.Extension,
                DirectoryName = info.DirectoryName,
                FullPath = info.FullName,
                Size = info.Length,
                Height = ImageSource?.Height ?? 0,
                Width = ImageSource?.Width ?? 0,
                CreatedTime = info.CreationTime,
                ModifiedTime = info.LastWriteTime,
            };
        }
        catch
        {
            CurrentFile = new FileInfoModel();
        }
    }

    /// ClearCurrentFile
    private void ClearCurrentFile()
    {
        StswApp.StswWindow.Title = string.Empty;

        CurrentFilePath = null;
        ImageSource = null;
        MediaSource = null;
        IsMediaFile = false;
        CurrentFile = new FileInfoModel();

        UpdateDisplayMode();
    }

    /// ReleaseCurrentPreviewAsync
    private async Task ReleaseCurrentPreviewAsync()
    {
        ImageSource = null;
        MediaSource = null;

        // Give media controls a short moment to release file handles before move/delete/overwrite.
        await Task.Delay(50);
    }

    /// RemoveFileFromList
    private bool RemoveFileFromList(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var files = DirectoryFiles
            .Where(x => !_pathComparer.Equals(x, path))
            .ToList();

        if (files.Count == DirectoryFiles.Count)
            return false;

        DirectoryFiles = files;
        return true;
    }

    /// MoveCurrentFileToAsync
    private async Task MoveCurrentFileToAsync(ShortcutSetting shortcut)
    {
        var path = CurrentFilePath;
        var index = CurrentFileIndex;
        var targetDirectory = shortcut.TargetPath;

        if (string.IsNullOrEmpty(path))
            return;

        if (!File.Exists(path))
        {
            RemoveFileFromList(path);
            SelectFileAt(index);
            return;
        }

        if (string.IsNullOrWhiteSpace(targetDirectory))
            return;

        if (!EnsureTargetDirectory(targetDirectory, shortcut.CreateTargetDirectory))
            return;

        var newPath = PrepareTargetPath(path, targetDirectory, shortcut.ConflictMode);
        if (newPath == null || _pathComparer.Equals(path, newPath))
            return;

        await ReleaseCurrentPreviewAsync();

        try
        {
            if (shortcut.ConflictMode == FileConflictMode.Overwrite && File.Exists(newPath))
                File.Move(path, newPath, true);
            else
                File.Move(path, newPath);
        }
        catch
        {
            await RefreshAsync();
            return;
        }

        RemoveFileFromList(path);
        SelectFileAfterOperation(index, shortcut.AfterAction);
    }

    /// CopyCurrentFileToAsync
    private async Task CopyCurrentFileToAsync(ShortcutSetting shortcut)
    {
        var path = CurrentFilePath;
        var index = CurrentFileIndex;
        var targetDirectory = shortcut.TargetPath;

        if (string.IsNullOrEmpty(path))
            return;

        if (!File.Exists(path))
        {
            RemoveFileFromList(path);
            SelectFileAt(index);
            return;
        }

        if (string.IsNullOrWhiteSpace(targetDirectory))
            return;

        if (!EnsureTargetDirectory(targetDirectory, shortcut.CreateTargetDirectory))
            return;

        var newPath = PrepareTargetPath(path, targetDirectory, shortcut.ConflictMode);
        if (newPath == null || _pathComparer.Equals(path, newPath))
            return;

        try
        {
            await Task.Run(() => File.Copy(path, newPath, shortcut.ConflictMode == FileConflictMode.Overwrite), App.CancellationToken);
        }
        catch
        {
            await RefreshAsync();
            return;
        }

        SelectFileAfterOperation(index, shortcut.AfterAction);
    }

    /// OpenCurrentFileWith
    private void OpenCurrentFileWith(ShortcutSetting shortcut)
    {
        var path = CurrentFilePath;
        var executablePath = shortcut.TargetPath;

        if (string.IsNullOrEmpty(path) || !File.Exists(path) || string.IsNullOrWhiteSpace(executablePath))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = BuildArguments(shortcut.Arguments, path),
                UseShellExecute = false
            });
        }
        catch
        {
            // ignored
        }
    }

    /// EnsureTargetDirectory
    private static bool EnsureTargetDirectory(string targetDirectory, bool createTargetDirectory)
    {
        if (Directory.Exists(targetDirectory))
            return true;

        if (!createTargetDirectory)
            return false;

        try
        {
            Directory.CreateDirectory(targetDirectory);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// PrepareTargetPath
    private static string? PrepareTargetPath(string sourcePath, string targetDirectory, FileConflictMode conflictMode)
    {
        var targetPath = Path.Combine(targetDirectory, Path.GetFileName(sourcePath));

        if (!File.Exists(targetPath))
            return targetPath;

        return conflictMode switch
        {
            FileConflictMode.Rename => GetAvailablePath(targetPath),
            FileConflictMode.Overwrite => targetPath,
            FileConflictMode.Skip => null,
            _ => null
        };
    }

    /// GetAvailablePath
    private static string GetAvailablePath(string path)
    {
        if (!File.Exists(path))
            return path;

        var directory = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        for (var i = 1; ; i++)
        {
            var candidate = Path.Combine(directory, $"{name} ({i}){extension}");
            if (!File.Exists(candidate))
                return candidate;
        }
    }

    /// BuildArguments
    private static string BuildArguments(string? argumentsTemplate, string filePath)
    {
        var template = string.IsNullOrWhiteSpace(argumentsTemplate)
            ? "\"{file}\""
            : argumentsTemplate;

        return template
            .Replace("{file}", filePath)
            .Replace("{directory}", Path.GetDirectoryName(filePath) ?? string.Empty)
            .Replace("{name}", Path.GetFileName(filePath))
            .Replace("{nameWithoutExtension}", Path.GetFileNameWithoutExtension(filePath))
            .Replace("{extension}", Path.GetExtension(filePath));
    }

    /// SelectFileAfterOperation
    private void SelectFileAfterOperation(int originalIndex, AfterShortcutAction afterAction)
    {
        if (afterAction == AfterShortcutAction.RefreshOnly)
        {
            _ = RefreshAsync();
            return;
        }

        if (DirectoryFiles.Count == 0)
        {
            CurrentFileIndex = -1;
            ClearCurrentFile();
            return;
        }

        var nextIndex = afterAction switch
        {
            AfterShortcutAction.SelectPrevious => originalIndex - 1,
            AfterShortcutAction.SelectNext => originalIndex,
            AfterShortcutAction.KeepIndex => originalIndex,
            _ => originalIndex
        };

        SelectFileAt(nextIndex);
    }

    /// UpdateDisplayMode
    private void UpdateDisplayMode()
    {
        UseMediaPlayer = !string.IsNullOrEmpty(CurrentFilePath) && (IsDynamicDisplayMode || IsMediaFile);
        DisplayModeText = IsDynamicDisplayMode ? "Dynamic display mode" : "Static display mode";
    }
}
