using System.Threading.Tasks;

namespace StswGallery;
public interface IDialogFactory
{
    Task<object?> DisplayDialog<T>(string dialogIdentifier) where T : StswObservableDialog;
    Task<object?> DisplayDialog<T>(string identifier, out T context) where T : StswObservableDialog;
    void ClearDialogs();
}
