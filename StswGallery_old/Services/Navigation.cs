using System;

namespace StswGallery;
public partial class Navigation(Func<Type, BaseContext> contextFactory, IDialogFactory dialogFactory) : StswObservableObject, INavigation
{
    [StswObservableProperty] private BaseContext? _currentContext;

    /// NavigateTo
    public BaseContext NavigateTo<T>() where T : BaseContext
    {
        var context = contextFactory.Invoke(typeof(T));
        dialogFactory.ClearDialogs();
        CurrentContext = context;
        return (T)context;
    }
}
