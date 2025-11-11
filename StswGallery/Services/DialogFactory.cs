using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StswGallery;
public class DialogFactory(Func<Type, string, StswObservableDialog> dialogFactory) : IDialogFactory
{
    private readonly Stack<string> _openedDialogIdentifiers = new();

    /// DisplayDialog
    public Task<object?> DisplayDialog<T>(string identifier) where T : StswObservableDialog => DisplayDialog<T>(identifier, out _);

    /// DisplayDialog
    public Task<object?> DisplayDialog<T>(string identifier, out T context) where T : StswObservableDialog
    {
        context = (T)dialogFactory.Invoke(typeof(T), identifier);
        _openedDialogIdentifiers.Push(identifier);
        var dialogTask = StswContentDialog.Show(context, identifier);

        dialogTask.ContinueWith(_ => {
            if (_openedDialogIdentifiers.TryPeek(out var ident) && ident == identifier)
                _openedDialogIdentifiers.Pop();
        });

        return dialogTask;
    }

    /// ClearDialogs
    public void ClearDialogs()
    {
        while (_openedDialogIdentifiers.Count != 0)
        {
            var identifier = _openedDialogIdentifiers.Pop();
            StswContentDialog.Close(identifier);
        }
    }
}
