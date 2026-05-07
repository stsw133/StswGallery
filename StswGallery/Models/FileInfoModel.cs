using System;

namespace StswGallery;
public partial class FileInfoModel : StswObservableObject
{
    [StswObservableProperty] string _name = string.Empty;
    [StswObservableProperty] string _extension = string.Empty;
    [StswObservableProperty] string? _directoryName;
    [StswObservableProperty] string _fullPath = string.Empty;
    [StswObservableProperty] long _size;
    public string SizeText => StswFn.FormatByteSize(Size);
    [StswObservableProperty] double _height;
    [StswObservableProperty] double _width;
	public string DimensionText => $"{Width:0} x {Height:0}";
	[StswObservableProperty] DateTime _createdTime;
    [StswObservableProperty] DateTime _modifiedTime;
}
