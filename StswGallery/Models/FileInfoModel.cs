using System;

namespace StswGallery;
public partial class FileInfoModel : StswObservableObject
{
    [StswObservableProperty] string _name = string.Empty;
    [StswObservableProperty] string _extension = string.Empty;
    [StswObservableProperty] string? _directoryName;
    [StswObservableProperty] string _fullPath = string.Empty;
    [StswObservableProperty] long _size;
    [StswObservableProperty] double _height;
    [StswObservableProperty] double _width;
    [StswObservableProperty] DateTime _createdTime;
    [StswObservableProperty] DateTime _modifiedTime;

    public string SizeText => StswFn.FormatByteSize(Size);
    public string DimensionText => Width > 0 && Height > 0 ? $"{Width:0} x {Height:0}" : string.Empty;

    partial void OnSizeChanged(long oldValue, long newValue) => OnPropertyChanged(nameof(SizeText));
    partial void OnWidthChanged(double oldValue, double newValue) => OnPropertyChanged(nameof(DimensionText));
    partial void OnHeightChanged(double oldValue, double newValue) => OnPropertyChanged(nameof(DimensionText));
}
