using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace StswGallery;

public partial class MainContext : StswObservableObject
{
    private static readonly Random Random = new();
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".dib", ".gif", ".ico", ".jpe", ".jfif", ".jpeg", ".jpg", ".png", ".tif", ".tiff", ".webp"
    };

    private readonly CancellationTokenSource _cts = new();
    private string? _directoryPath;
    private string? _currentFilePath;
    private Bitmap? _currentImage;
    private int _currentFileIndex = -1;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? DirectoryPath
    {
        get => _directoryPath;
        set => Set(ref _directoryPath, value);
    }

    public Bitmap? CurrentImage
    {
        get => _currentImage;
        set
        {
            Set(ref _currentImage, value);
            OnPropertyChanged(nameof(IsImageMissing));
        }
    }

    public bool IsImageMissing => CurrentImage is null;

    public List<string> DirectoryFiles { get; private set; } = [];

    public MainContext()
    {
        AppSettingsService.Reload();
        _ = RepeatRefreshAsync();
    }

    public async Task SelectDirectoryAsync(Window owner)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Wybierz folder ze zdjęciami",
            AllowMultiple = false
        });

        var selected = folders.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(selected))
            return;

        DirectoryPath = selected;
        await RefreshAsync();
        _currentFileIndex = 0;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    public async Task HandleKeyPressAsync(Key key, Window owner)
    {
        if (key is >= Key.D0 and <= Key.D9)
        {
            KeyNumber(key - Key.D0);
            return;
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            KeyNumber(key - Key.NumPad0);
            return;
        }

        switch (AppSettingsService.MapAction(key))
        {
            case ActionKeyType.Refresh:
                await RefreshAsync();
                break;
            case ActionKeyType.SelectDirectory:
                await SelectDirectoryAsync(owner);
                break;
            case ActionKeyType.RemoveFile:
                RemoveFile();
                break;
            case ActionKeyType.PreviousFile:
                PreviousFile();
                break;
            case ActionKeyType.NextFile:
                NextFile();
                break;
            case ActionKeyType.FirstFile:
                FirstFile();
                break;
            case ActionKeyType.LastFile:
                LastFile();
                break;
            case ActionKeyType.RandomFile:
                RandomFile();
                break;
            case ActionKeyType.RotateLeft:
                RotateLeft();
                break;
            case ActionKeyType.RotateRight:
                RotateRight();
                break;
        }
    }

    public async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(DirectoryPath) || !Directory.Exists(DirectoryPath))
            return;

        var previous = _currentFilePath;
        var files = await Task.Run(() => Directory.EnumerateFiles(DirectoryPath)
            .Where(IsSupportedImageFile)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList());

        DirectoryFiles = files;

        var existingIndex = !string.IsNullOrWhiteSpace(previous)
            ? files.FindIndex(x => string.Equals(x, previous, StringComparison.OrdinalIgnoreCase))
            : -1;

        if (existingIndex >= 0)
            _currentFileIndex = existingIndex;
        else if (DirectoryFiles.Count > 0)
            _currentFileIndex = DirectoryFiles.Count - 1;
        else
            _currentFileIndex = -1;

        UpdateCurrentFilePath();

        if (!string.Equals(previous, _currentFilePath, StringComparison.OrdinalIgnoreCase))
            ReadImageFromFile();
    }

    public void RemoveFile()
    {
        if (!File.Exists(_currentFilePath))
            return;

        File.Delete(_currentFilePath!);
        DirectoryFiles.Remove(_currentFilePath!);

        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    public void KeyNumber(int? number)
    {
        if (number is null or < 0 or > 9)
            return;

        var shortcut = AppSettingsService.GetShortcut(number.Value);
        if (shortcut is null || shortcut.Type == ShortcutType.None || string.IsNullOrWhiteSpace(shortcut.Value))
            return;

        if (shortcut.Type == ShortcutType.MoveTo)
        {
            if (!Directory.Exists(shortcut.Value) || _currentFilePath is null)
                return;

            var newPath = Path.Combine(shortcut.Value, Path.GetFileName(_currentFilePath));
            if (_currentFilePath != newPath && !File.Exists(newPath))
            {
                File.Move(_currentFilePath, newPath);
                DirectoryFiles.Remove(_currentFilePath);
                UpdateCurrentFilePath();
                ReadImageFromFile();
            }
        }

        if (shortcut.Type == ShortcutType.OpenWith)
        {
            if (!File.Exists(_currentFilePath) || string.IsNullOrWhiteSpace(shortcut.Value))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = shortcut.Value,
                Arguments = $"\"{_currentFilePath}\""
            });
        }
    }

    public void PreviousFile()
    {
        _currentFileIndex--;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    public void NextFile()
    {
        _currentFileIndex++;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    public void FirstFile()
    {
        _currentFileIndex = 0;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    public void LastFile()
    {
        _currentFileIndex = DirectoryFiles.Count - 1;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    public void RandomFile()
    {
        if (DirectoryFiles.Count == 0)
            return;

        _currentFileIndex = Random.Next(DirectoryFiles.Count);
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    public void RotateLeft() => RotateImage(clockwise: false);
    public void RotateRight() => RotateImage(clockwise: true);

    private async Task RepeatRefreshAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            await RefreshAsync();
            await Task.Delay(TimeSpan.FromSeconds(15), _cts.Token);
        }
    }

    private void RotateImage(bool clockwise)
    {
        if (!File.Exists(_currentFilePath))
            return;

        try
        {
            using var image = SixLabors.ImageSharp.Image.Load(_currentFilePath!);

            image.Mutate(x =>
            {
                if (clockwise)
                    x.Rotate(90);
                else
                    x.Rotate(270);
            });

            image.Save(_currentFilePath!);
            ReadImageFromFile();
        }
        catch
        {
            CurrentImage = null;
        }
    }

    private void UpdateCurrentFilePath()
    {
        if (DirectoryFiles.Count == 0)
        {
            _currentFilePath = null;
            return;
        }

        _currentFileIndex = Math.Clamp(_currentFileIndex, 0, DirectoryFiles.Count - 1);
        _currentFilePath = DirectoryFiles[_currentFileIndex];
    }

    private void ReadImageFromFile()
    {
        if (!File.Exists(_currentFilePath))
        {
            CurrentImage = null;
            return;
        }

        try
        {
            using var fs = File.OpenRead(_currentFilePath!);
            CurrentImage = new Bitmap(fs);
        }
        catch
        {
            CurrentImage = null;
        }
    }

    private static bool IsSupportedImageFile(string path) => SupportedExtensions.Contains(Path.GetExtension(path));

    private void Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(property);
    }

    [RelayCommand] async Task OnSelectDirectory() => await SelectDirectoryAsync(App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
    [RelayCommand] async Task OnRefresh() => await RefreshAsync();
    [RelayCommand] void OnRemoveFile() => RemoveFile();
    [RelayCommand] void OnRotateLeft() => RotateLeft();
    [RelayCommand] void OnRotateRight() => RotateRight();
    [RelayCommand] void OnPrevious() => PreviousFile();
    [RelayCommand] void OnNext() => NextFile();
    [RelayCommand] void OnRandom() => RandomFile();
    async void OnKeyDown(object? sender, KeyEventArgs e) => await HandleKeyPressAsync(e.Key, App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
}
