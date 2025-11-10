using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace StswGallery;
public partial class MainContext : StswObservableObject
{
    private readonly CancellationTokenSource _rewatchCancellationTokenSource = new();

    /// Init
    [StswCommand]
    void Init()
    {
        App.Current.Exit += OnApplicationExit;
        _ = RepeatRefresh(_rewatchCancellationTokenSource.Token);
    }

    /// RepeatRefresh
    async Task RepeatRefresh(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Refresh();
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
        if (!_rewatchCancellationTokenSource.IsCancellationRequested)
            _rewatchCancellationTokenSource.Cancel();
        _rewatchCancellationTokenSource.Dispose();
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
    async Task Refresh()
    {
        if (Directory.Exists(DirectoryPath))
        {
            var files = new List<string>();
            await Task.Run(() => files = [.. Directory.EnumerateFiles(DirectoryPath).OrderBy(x => x, new StswNaturalStringComparer())]);
            DirectoryFiles = files;

            if (!string.IsNullOrEmpty(CurrentFilePath) && files.IndexOf(CurrentFilePath) is int index && index >= 0)
            {
                CurrentFileIndex = index;
                UpdateCurrentFilePath();
            }
        }
    }

    /// Config
    [StswCommand]
    void Config()
    {
        StswContentDialog.Show(ConfigContext, "Config");
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

        var shortcutValue = Properties.Settings.Default[$"Shortcut{num}Value"] as string;
        if (string.IsNullOrEmpty(shortcutValue))
            return;

        /// action: MoveTo
        if ((ShortcutType)Properties.Settings.Default[$"Shortcut{num}Type"] == ShortcutType.MoveTo)
        {
            if (Directory.Exists(shortcutValue) && CurrentFilePath != null)
            {
                var newPath = Path.Combine(shortcutValue, Path.GetFileName(CurrentFilePath));
                if (CurrentFilePath != newPath && !File.Exists(newPath))
                {
                    File.Move(CurrentFilePath, newPath);
                    DirectoryFiles.Remove(CurrentFilePath);

                    UpdateCurrentFilePath();
                    ReadImageFromFile();
                }
            }
        }
    }

    /// KeyPress
    [StswCommand]
    async Task KeyPress(KeyEventArgs? e)
    {
        if (e == null || string.IsNullOrEmpty(DirectoryPath) || IsConfigOpen)
            return;

        Func<Task>? action = e.Key switch
        {
            Key.Left => () => { PreviousFile(); return Task.CompletedTask; },
            Key.Right => () => { NextFile(); return Task.CompletedTask; },
            Key.Z => () => { RandomFile(); return Task.CompletedTask; },
            Key.F5 => Refresh,
            Key.F9 => SelectDirectory,
            Key.Delete => () => { RemoveFile(); return Task.CompletedTask; },
            >= Key.D0 and <= Key.D9 => () => { KeyNumber(e.Key - Key.D0); return Task.CompletedTask; },
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

    /// ReadImageFile
    private void ReadImageFromFile()
    {
        if (File.Exists(CurrentFilePath))
        {
            StswApp.StswWindow.Title = Path.GetFileName(CurrentFilePath);

            try
            {
                ImageSource = StswFnUI.BytesToBitmapImage(File.ReadAllBytes(CurrentFilePath));
            }
            catch
            {
                ImageSource = null;
            }
        }
        else
        {
            StswApp.StswWindow.Title = string.Empty;
            ImageSource = null;
        }
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



    private static readonly Random _random = new();
    [StswObservableProperty] ConfigContext _configContext = new();
    [StswObservableProperty] int _currentFileIndex = -1;
    [StswObservableProperty] string? _currentFilePath;
    [StswObservableProperty] List<string> _directoryFiles = [];
    [StswObservableProperty] string? _directoryPath;
    [StswObservableProperty] ImageSource? _imageSource;
    [StswObservableProperty] bool _isConfigOpen;
}
