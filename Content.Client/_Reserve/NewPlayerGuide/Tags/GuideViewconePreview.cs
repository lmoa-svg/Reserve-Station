using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Guidebook wrapper for <see cref="ViewconePreviewControl"/>.
/// </summary>
[UsedImplicitly]
public sealed class GuideViewconePreview : BoxContainer, IDocumentTag
{
    public GuideViewconePreview()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        AddChild(new ViewconePreviewControl());
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = this;
        return true;
    }
}
