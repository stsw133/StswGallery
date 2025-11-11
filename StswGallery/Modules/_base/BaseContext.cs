using System;

namespace StswGallery;
public class BaseContext : StswObservableObject, IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
