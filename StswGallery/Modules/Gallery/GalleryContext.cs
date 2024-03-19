using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace StswGallery;
internal class GalleryContext : StswObservableObject
{
    public ICommand SelectDirectoryCommand { get; set; }
    public ICommand RefreshCommand { get; set; }
    public ICommand ConfigCommand { get; set; }
    public ICommand RemoveFileCommand { get; set; }
    public ICommand KeyNumberCommand { get; set; }
    public ICommand RandomFileCommand { get; set; }
    public ICommand PreviousFileCommand { get; set; }
    public ICommand NextFileCommand { get; set; }

    public GalleryContext()
    {
        SelectDirectoryCommand = new StswCommand(SelectDirectory);
        RefreshCommand = new StswCommand(Refresh);
        ConfigCommand = new StswCommand(Config);
        RemoveFileCommand = new StswCommand(RemoveFile);
        KeyNumberCommand = new StswCommand<object?>(KeyNumber);
        RandomFileCommand = new StswCommand(RandomFile);
        PreviousFileCommand = new StswCommand(PreviousFile);
        NextFileCommand = new StswCommand(NextFile);
    }

    #region Events & methods
    /// Command: select directory
    private void SelectDirectory()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DirectoryPath = dialog.SelectedPath;

            var filesList = Directory.EnumerateFiles(DirectoryPath).ToList();

            DirectoryFilesCounter = 0;
            ReadImageFromFile(filesList);
        }
    }

    /// Command: refresh
    private void Refresh()
    {
        if (DirectoryPath == null)
            return;

        var filesList = Directory.EnumerateFiles(DirectoryPath).ToList();

        if (DirectoryFilesCounter < filesList.Count)
            ReadImageFromFile(filesList);
        else if (filesList.Count == 0)
            ImageSource = null;
        else
            PreviousFile();
    }

    /// Command: config
    private void Config()
    {
        StswContentDialog.Show(ConfigContext, "Config");
    }

    /// Command: remove file
    private void RemoveFile()
    {
        if (DirectoryPath == null)
            return;

        var filesList = Directory.EnumerateFiles(DirectoryPath);

        var currentFile = filesList.Skip(DirectoryFilesCounter).FirstOrDefault();
        if (currentFile != null)
            StswFn.MoveToRecycleBin(currentFile);

        Refresh();
    }

    /// Command: key `number`
    private void KeyNumber(object? number)
    {
        if (DirectoryPath == null)
            return;

        var filesList = Directory.EnumerateFiles(DirectoryPath);

        var currentFile = filesList.Skip(DirectoryFilesCounter).FirstOrDefault();
        if (currentFile != null)
        {
            /// action: MoveTo
            if ((ShortcutType)Properties.Settings.Default[$"Shortcut{number}Type"] == ShortcutType.MoveTo)
            {
                var newPath = Path.Combine(Properties.Settings.Default[$"Shortcut{number}Value"].ToString()!, Path.GetFileName(currentFile));
                if (currentFile != newPath && !File.Exists(newPath))
                    File.Move(currentFile, newPath);
                else
                    NextFile();
            }
        }
        Refresh();
    }

    /// Command: random file
    private void RandomFile()
    {
        if (DirectoryPath == null)
            return;

        var filesList = Directory.EnumerateFiles(DirectoryPath).ToList();

        DirectoryFilesCounter = new Random().Next(filesList.Count - 1);
        ReadImageFromFile(filesList);
    }

    /// Command: previous file
    private void PreviousFile()
    {
        if (DirectoryPath == null)
            return;

        var filesList = Directory.EnumerateFiles(DirectoryPath).ToList();

        if (DirectoryFilesCounter > 0)
        {
            DirectoryFilesCounter--;
            ReadImageFromFile(filesList);
        }
    }

    /// Command: next file
    private void NextFile()
    {
        if (DirectoryPath == null)
            return;

        var filesList = Directory.EnumerateFiles(DirectoryPath).ToList();

        if (DirectoryFilesCounter < filesList.Count - 1)
        {
            DirectoryFilesCounter++;
            ReadImageFromFile(filesList);
        }
    }

    /// ReadImageFile
    private void ReadImageFromFile(List<string> filesList)
    {
        var filePath = filesList.Skip(DirectoryFilesCounter).FirstOrDefault();
        if (filePath != null)
        {
            try
            {
                ImageSource = File.ReadAllBytes(filePath).ToBitmapImage();
            }
            catch
            {
                ImageSource = null;
            }
        }
        StswApp.StswWindow.Title = Path.GetFileName(filePath);
    }
    #endregion

    /// ConfigContext
    public ConfigContext ConfigContext
    {
        get => _configContext;
        set => SetProperty(ref _configContext, value);
    }
    private ConfigContext _configContext = new();

    /// DirectoryFilesCounter
    public int DirectoryFilesCounter
    {
        get => _directoryFilesCounter;
        set => SetProperty(ref _directoryFilesCounter, value);
    }
    private int _directoryFilesCounter;

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
}
