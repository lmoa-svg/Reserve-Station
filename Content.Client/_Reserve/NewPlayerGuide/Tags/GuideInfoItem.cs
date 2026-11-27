using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Self-closing Guidebook tag rendering a title label and permissive rich-text description block.
/// </summary>
[UsedImplicitly]
public sealed class GuideInfoItem : BoxContainer, IDocumentTag
{
    public GuideInfoItem()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        SeparationOverride = 2;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        if (!args.TryGetValue("Title", out var title) || string.IsNullOrWhiteSpace(title))
        {
            Logger.Error("GuideInfoItem tag is missing required \"Title\" attribute.");
            control = null;
            return false;
        }

        if (!args.TryGetValue("Description", out var description) || string.IsNullOrWhiteSpace(description))
        {
            Logger.Error("GuideInfoItem tag is missing required \"Description\" attribute.");
            control = null;
            return false;
        }

        AddChild(new Label
        {
            Text = title,
        });
        AddChild(GuideCVarHelper.CreateSubTextRichLabel(description));

        control = this;
        return true;
    }
}
