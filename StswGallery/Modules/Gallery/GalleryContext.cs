using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace StswGallery;

internal class GalleryContext : StswObservableObject
{
    public ICommand KeyLeftCommand { get; set; }
    public ICommand KeyRightCommand { get; set; }
    public ICommand KeyRefreshCommand { get; set; }
    public ICommand KeyNumberCommand { get; set; }
    public ICommand SelectDirectoryCommand { get; set; }

    public GalleryContext()
    {
        KeyLeftCommand = new StswCommand(KeyLeft);
        KeyRightCommand = new StswCommand(KeyRight);
        KeyRefreshCommand = new StswCommand(KeyRefresh);
        KeyNumberCommand = new StswCommand<object?>(KeyNumber);
        SelectDirectoryCommand = new StswCommand(SelectDirectory);
    }

    #region Events & methods
    /// Command: key left
    private void KeyLeft()
    {
        if (DirectoryPath == null)
            return;

        string? filePath = null;
        if (DirectoryPath != null && DirectoryFilesCounter > 0)
        {
            DirectoryFilesCounter--;
            filePath = Directory.EnumerateFiles(DirectoryPath).Skip(DirectoryFilesCounter).First();
        }

        if (filePath != null)
            ImageSource = File.ReadAllBytes(filePath).ToBitmapImage();
    }

    /// Command: key right
    private void KeyRight()
    {
        if (DirectoryPath == null)
            return;

        string? filePath = null;
        if (DirectoryPath != null && DirectoryFilesCounter < Directory.EnumerateFiles(DirectoryPath).Count() - 1)
        {
            DirectoryFilesCounter++;
            filePath = Directory.EnumerateFiles(DirectoryPath).Skip(DirectoryFilesCounter).First();
        }

        if (filePath != null)
            ImageSource = File.ReadAllBytes(filePath).ToBitmapImage();
    }

    /// Command: refresh
    private void KeyRefresh()
    {
        if (DirectoryPath == null)
            return;

        string? filePath = null;
        if (DirectoryPath != null && DirectoryFilesCounter < Directory.EnumerateFiles(DirectoryPath).Count())
            filePath = Directory.EnumerateFiles(DirectoryPath).Skip(DirectoryFilesCounter).First();
        else
            KeyLeft();

        if (filePath != null)
            ImageSource = File.ReadAllBytes(filePath).ToBitmapImage();
    }

    /// Command: key `number`
    private void KeyNumber(object? number)
    {
        if (DirectoryPath == null)
            return;

        //var currentFile = Directory.EnumerateFiles(DirectoryPath).Skip(DirectoryFilesCounter).First();
        //var newPath = Path.Combine(@$"", Path.GetFileName(currentFile));
        //File.Move(currentFile, newPath);
        KeyRefresh();
    }

    /// Command: select directory
    private void SelectDirectory()
    {
        var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            DirectoryPath = dialog.SelectedPath;

            DirectoryFilesCounter = 0;
            var filePath = Directory.EnumerateFiles(DirectoryPath).Skip(DirectoryFilesCounter).First();

            if (filePath != null)
                ImageSource = File.ReadAllBytes(filePath).ToBitmapImage();
        }
    }
    #endregion

    #region Properties
    /// DirectoryFilesCounter
    private int directoryFilesCounter;
    public int DirectoryFilesCounter
    {
        get => directoryFilesCounter;
        set => SetProperty(ref directoryFilesCounter, value);
    }

    /// DirectoryPath
    private string? directoryPath;
    public string? DirectoryPath
    {
        get => directoryPath;
        set => SetProperty(ref directoryPath, value);
    }

    /// ImageSource
    private ImageSource? imageSource;
    public ImageSource? ImageSource
    {
        get => imageSource;
        set => SetProperty(ref imageSource, value);
    }
    #endregion
}
