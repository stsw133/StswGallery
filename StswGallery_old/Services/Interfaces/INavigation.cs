namespace StswGallery;
public interface INavigation
{
    BaseContext? CurrentContext { get; }
    BaseContext NavigateTo<T>() where T : BaseContext;
}
