using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace StswGallery;
internal class MainContext : StswObservableObject
{
    public StswAsyncCommand SelectDirectoryCommand { get; }
    public StswAsyncCommand RefreshCommand { get; }
    public StswCommand ConfigCommand { get; }
    public StswCommand RemoveFileCommand { get; }
    public StswCommand<int?> KeyNumberCommand { get; }
    public StswCommand<KeyEventArgs?> KeyPressCommand { get; }
    public StswCommand RandomFileCommand { get; }
    public StswCommand PreviousFileCommand { get; }
    public StswCommand NextFileCommand { get; }

    public MainContext()
    {
        SelectDirectoryCommand = new(SelectDirectory);
        RefreshCommand = new(Refresh);
        ConfigCommand = new(Config);
        RemoveFileCommand = new(RemoveFile);
        KeyNumberCommand = new(KeyNumber);
        KeyPressCommand = new(KeyPress);
        RandomFileCommand = new(RandomFile);
        PreviousFileCommand = new(PreviousFile);
        NextFileCommand = new(NextFile);

        Task.Run(RewatchList);
    }



    /// Command: rewatch list
    private async Task RewatchList()
    {
        while (true)
        {
            await Refresh();
            await Task.Delay(new TimeSpan(0, 0, 15));
        }
    }

    /// Command: select directory
    private async Task SelectDirectory()
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

    /// Command: refresh
    private async Task Refresh()
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

    /// Command: config
    private void Config()
    {
        StswContentDialog.Show(ConfigContext, "Config");
    }

    /// Command: remove file
    private void RemoveFile()
    {
        if (File.Exists(CurrentFilePath))
        {
            StswFn.MoveToRecycleBin(CurrentFilePath);
            DirectoryFiles.Remove(CurrentFilePath);

            UpdateCurrentFilePath();
            ReadImageFromFile();
        }
    }

    /// Command: key number
    private void KeyNumber(int? num)
    {
        if (num == null || !num.Between(0, 9))
            return;

        var shortcutValue = Properties.Settings.Default[$"Shortcut{num}Value"].ToString();
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

    /// Command: key press
    private void KeyPress(KeyEventArgs? e)
    {
        if (e == null || string.IsNullOrEmpty(DirectoryPath) || IsConfigOpen)
            return;

        Action? action = e.Key switch
        {
            Key.Left => PreviousFile,
            Key.Right => NextFile,
            Key.Z => RandomFile,
            Key.F5 => () => Task.Run(Refresh),
            Key.F9 => () => Task.Run(SelectDirectory),
            Key.Delete => RemoveFile,
            >= Key.D0 and <= Key.D9 => () => KeyNumber(e.Key - Key.D0),
            _ => null
        };
        action?.Invoke();
    }

    /// Command: random file
    private void RandomFile()
    {
        CurrentFileIndex = new Random().Next(DirectoryFiles.Count - 1);
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    /// Command: previous file
    private void PreviousFile()
    {
        CurrentFileIndex--;
        UpdateCurrentFilePath();
        ReadImageFromFile();
    }

    /// Command: next file
    private void NextFile()
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



    /// ConfigContext
    public ConfigContext ConfigContext
    {
        get => _configContext;
        set => SetProperty(ref _configContext, value);
    }
    private ConfigContext _configContext = new();

    /// CurrentFileIndex
    public int CurrentFileIndex
    {
        get => _currentFileIndex;
        set => SetProperty(ref _currentFileIndex, value);
    }
    private int _currentFileIndex = -1;

    /// CurrentFilePath
    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set => SetProperty(ref _currentFilePath, value);
    }
    private string? _currentFilePath;

    /// DirectoryFiles
    public List<string> DirectoryFiles
    {
        get => _directoryFiles;
        set => SetProperty(ref _directoryFiles, value);
    }
    private List<string> _directoryFiles = [];

    /// DirectoryPath
    public string? DirectoryPath
    {
        get => _directoryPath;
        set => SetProperty(ref _directoryPath, value);
    }
    private string? _directoryPath;

    /// ImageSource
    public ImageSource? ImageSource
    {
        get => _imageSource;
        set => SetProperty(ref _imageSource, value);
    }
    private ImageSource? _imageSource;

    /// IsConfigOpen
    public bool IsConfigOpen
    {
        get => _isConfigOpen;
        set => SetProperty(ref _isConfigOpen, value);
    }
    private bool _isConfigOpen;
}
