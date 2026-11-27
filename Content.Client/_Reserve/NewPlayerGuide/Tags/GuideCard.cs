using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Content.Client.Stylesheets;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Styled background panel container for Guidebook documents that routes parsed child tags into a padded vertical box.
/// </summary>
[UsedImplicitly]
public sealed class GuideCard : PanelContainer, IDocumentTag
{
    private readonly BoxContainer _contentBox;
    private bool _reparenting;

    public GuideCard()
    {
        HorizontalExpand = true;
        MouseFilter = MouseFilterMode.Ignore;
        StyleClasses.Add(StyleClass.BackgroundPanel);

        _contentBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(10, 8, 10, 8),
            SeparationOverride = 8,
        };

        AddChild(_contentBox);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        if (args.TryGetValue("Title", out var title) && !string.IsNullOrEmpty(title))
        {
            _contentBox.AddChild(new Label
            {
                Text = title,
                StyleClasses = { StyleClass.LabelSubText },
            });
        }

        control = this;
        return true;
    }

    protected override void ChildAdded(Control newChild)
    {
        base.ChildAdded(newChild);

        if (_reparenting || newChild == _contentBox)
            return;

        _reparenting = true;
        try
        {
            RemoveChild(newChild);
            _contentBox.AddChild(newChild);
        }
        finally
        {
            _reparenting = false;
        }
    }
}
