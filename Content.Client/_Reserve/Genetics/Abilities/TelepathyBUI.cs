using Content.Shared.Administration;
using Content.Client.UserInterface.Controls;
using Content.Shared._Reserve.Genetics.Abilities;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Reserve.Genetics.Abilities;

public sealed partial class TelepathyBUI : BoundUserInterface
{
    private DialogWindow? _window;

    public TelepathyBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var maxLength = EntMan.GetComponentOrNull<MutationTelepathyActionComponent>(Owner)?.MaxLength ?? 30;

        var field = "msg";
        var prompt = Loc.GetString("MutationTelepathy-window-message");
        var placeholder = Loc.GetString("MutationTelepathy-window-placeholder");
        var entries = new List<QuickDialogEntry>
        {
            new(field, QuickDialogEntryType.ShortText, prompt, placeholder)
        };
        var title = Loc.GetString("MutationTelepathy-window-title");

        _window = new(title, entries);

        if (FindLineEdit(_window) is { } lineEdit)
        {
            lineEdit.IsValid = text => text.Length <= maxLength;
        }

        _window.OnClose += Close;
        _window.OnConfirmed += responses =>
        {
            var msg = responses[field].Trim();
            if (msg.Length < 1)
                return;

            if (msg.Length > maxLength)
                msg = msg[..maxLength];

            SendMessage(new TelepathyChosenMessage(msg));
            _window.Close();
        };
    }

    private static LineEdit? FindLineEdit(Control control)
    {
        if (control is LineEdit le)
            return le;

        foreach (var child in control.Children)
        {
            if (FindLineEdit(child) is { } found)
                return found;
        }

        return null;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_window != null)
        {
            _window.OnClose -= Close;
            _window.Orphan();
            _window = null;
        }
    }
}
