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
	private static readonly Random _random = new();
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
    [StswObservableProperty] string? _mediaSource;
    [StswObservableProperty] string _displayModeText = "Tryb statyczny";
    [StswObservableProperty] bool _useMediaPlayer;

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
            await RefreshAsync(cancellationToken);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
            catch (TaskCanceledException)
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
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DirectoryPath = dialog.SelectedPath;
            await Refresh();
            CurrentFileIndex = 0;
            UpdateCurrentFilePath();
            ReadImageFromFile();
        }
    }

    /// Refresh
    [StswCommand]
    async Task Refresh() => await RefreshAsync();

    /// RefreshAsync
    async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!cancellationToken.CanBeCanceled)
            cancellationToken = App.CancellationToken;

        if (Directory.Exists(DirectoryPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var previousFilePath = CurrentFilePath;
            var files = new List<string>();
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                files = [.. Directory.EnumerateFiles(DirectoryPath)
                    .Where(IsSupportedFile)
                    .OrderBy(x => x, new StswNaturalStringComparer())];
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            DirectoryFiles = files;

            var existingIndex = !string.IsNullOrEmpty(previousFilePath)
                ? files.FindIndex(x => string.Equals(x, previousFilePath, StringComparison.OrdinalIgnoreCase))
                : -1;

            if (existingIndex >= 0)
                CurrentFileIndex = existingIndex;
            else if (DirectoryFiles.Count > 0)
                CurrentFileIndex = DirectoryFiles.Count - 1;
            else
                CurrentFileIndex = -1;

            UpdateCurrentFilePath();

            if (!string.Equals(CurrentFilePath, previousFilePath, StringComparison.OrdinalIgnoreCase))
                ReadImageFromFile();
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
        UpdateDisplayMode();
        ReadImageFromFile();
    }

    /// RemoveFile
    [StswCommand]
    void RemoveFile()
    {
        if (File.Exists(CurrentFilePath))
        {
            StswFn.MoveToRecycleBin(CurrentFilePath);
            DirectoryFiles.Remove(CurrentFilePath);

            UpdateCurrentFilePath();
            ReadImageFromFile();
        }
    }

    /// KeyNumber
    [StswCommand]
    void KeyNumber(int? num)
    {
        if (num == null || !num.Between(0, 9))
            return;

        var shortcut = AppSettingsService.GetShortcut(num.Value);
        if (shortcut == null)
            return;

        var shortcutType = shortcut.Type;
        if (shortcut.Type == ShortcutType.None)
            return;

        var shortcutValue = shortcut.Value;
        if (string.IsNullOrWhiteSpace(shortcutValue))
            return;

        switch (shortcutType)
        {
            case ShortcutType.None:
                break;

            case ShortcutType.MoveTo:
                if (string.IsNullOrEmpty(shortcutValue) || !Directory.Exists(shortcutValue) || CurrentFilePath == null)
                    break;

                var newPath = Path.Combine(shortcutValue, Path.GetFileName(CurrentFilePath));
                if (CurrentFilePath != newPath && !File.Exists(newPath))
                {
                    File.Move(CurrentFilePath, newPath);
                    DirectoryFiles.Remove(CurrentFilePath);

                    UpdateCurrentFilePath();
                    ReadImageFromFile();
                }

                break;

            case ShortcutType.OpenWith:
                if (string.IsNullOrEmpty(shortcutValue) || string.IsNullOrEmpty(CurrentFilePath) || !File.Exists(CurrentFilePath))
                    break;

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = shortcutValue,
                        Arguments = $"\"{CurrentFilePath}\""
                    });
                }
                catch
                {
                    // ignored
                }

                break;

            default:
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
            KeyNumber(e.Key - Key.D0);
            return;
        }

        if (e.Key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            KeyNumber(e.Key - Key.NumPad0);
            return;
        }

        var actionSetting = AppSettingsService.GetActionKeySettings().FirstOrDefault(s => s.Key == e.Key);
        if (actionSetting == null)
            return;

        Func<Task>? action = actionSetting.Action switch
        {
            ActionKeyType.Refresh => Refresh,
            ActionKeyType.SelectDirectory => SelectDirectory,
            ActionKeyType.RemoveFile => () => { RemoveFile(); return Task.CompletedTask; },
            ActionKeyType.PreviousFile => () => { PreviousFile(); return Task.CompletedTask; },
            ActionKeyType.NextFile => () => { NextFile(); return Task.CompletedTask; },
            ActionKeyType.FirstFile => () => { FirstFile(); return Task.CompletedTask; },
            ActionKeyType.LastFile => () => { LastFile(); return Task.CompletedTask; },
            ActionKeyType.RandomFile => () => { RandomFile(); return Task.CompletedTask; },
            ActionKeyType.RotateLeft => () => { RotateFlipImage(RotateFlipType.Rotate90FlipNone); return Task.CompletedTask; },
            ActionKeyType.RotateRight => () => { RotateFlipImage(RotateFlipType.Rotate270FlipNone); return Task.CompletedTask; },
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

        CurrentFileIndex = _random.Next(DirectoryFiles.Count);
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

	/// RotateFlipImage
	[StswCommand]
	void RotateFlipImage(RotateFlipType rotateFlipType)
    {
        if (!File.Exists(CurrentFilePath) || !IsSupportedImageFile(CurrentFilePath))
            return;

        var isRotated = false;

        try
        {
            using var memoryStream = new MemoryStream(File.ReadAllBytes(CurrentFilePath));
            using var bitmap = Image.FromStream(memoryStream);
            bitmap.RotateFlip(rotateFlipType);
            bitmap.Save(CurrentFilePath, bitmap.RawFormat);
            isRotated = true;
        }
        catch
        {
            isRotated = false;
        }

        if (isRotated)
            ReadImageFromFile();
    }

	/// PreviousFile
	[StswCommand]
    void PreviousFile()
    {
        CurrentFileIndex--;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    /// NextFile
    [StswCommand]
    void NextFile()
    {
        CurrentFileIndex++;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    /// FirstFile
    [StswCommand]
    void FirstFile()
    {
        CurrentFileIndex = 0;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    /// LastFile
    [StswCommand]
    void LastFile()
    {
        CurrentFileIndex = DirectoryFiles.Count - 1;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    /// ReadImageFile
    private void ReadImageFromFile()
    {
        if (File.Exists(CurrentFilePath))
        {
            StswApp.StswWindow.Title = Path.GetFileName(CurrentFilePath);
            IsMediaFile = IsSupportedMediaFile(CurrentFilePath);
            UpdateDisplayMode();

            if (UseMediaPlayer)
            {
                ImageSource = null;
                MediaSource = CurrentFilePath;
            }
            else
            {
                MediaSource = null;

                try
                {
                    ImageSource = StswFnUI.BytesToBitmapImage(File.ReadAllBytes(CurrentFilePath));
                }
                catch
                {
                    ImageSource = null;
                }
            }

            var info = new FileInfo(CurrentFilePath);
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
        else
        {
            StswApp.StswWindow.Title = string.Empty;
            ImageSource = null;
            IsMediaFile = false;
            MediaSource = null;
            CurrentFile = new FileInfoModel();
            UpdateDisplayMode();
        }
    }

    /// UpdateDisplayMode
    private void UpdateDisplayMode()
    {
        UseMediaPlayer = IsDynamicDisplayMode || IsMediaFile;
        DisplayModeText = IsDynamicDisplayMode ? "Tryb dynamiczny" : "Tryb statyczny";
    }

    /// UpdateCurrentFilePath
    private void UpdateCurrentFilePath()
    {
        if (DirectoryFiles.Count == 0)
        {
            CurrentFilePath = null;
            return;
        }

        CurrentFileIndex = Math.Clamp(CurrentFileIndex, 0, DirectoryFiles.Count - 1);
        CurrentFilePath = DirectoryFiles.ElementAtOrDefault(CurrentFileIndex);
    }
}
