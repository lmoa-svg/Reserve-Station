using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Content.Client.Stylesheets;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Choice card descriptor control placed inside <see cref="GuideChoiceRow"/>.
/// </summary>
[UsedImplicitly]
public sealed class GuideChoice : PanelContainer, IDocumentTag
{
    private const float ChoiceImageMaxHeight = 140f;

    [Dependency] private readonly IResourceCache _resources = default!;

    public string ChoiceValue { get; private set; } = string.Empty;
    public Button ChoiceButton { get; }

    public GuideChoice()
    {
        IoCManager.InjectDependencies(this);

        HorizontalExpand = true;
        MouseFilter = MouseFilterMode.Pass;
        StyleClasses.Add(StyleClass.BackgroundPanel);

        ChoiceButton = new Button
        {
            ToggleMode = true,
            MinHeight = 42,
            HorizontalExpand = true,
        };
    }

    public void SetSelectedVisual(bool selected)
    {
        ModulateSelfOverride = selected ? GuideCVarHelper.SelectedCardModulate : null;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Value", out var value))
        {
            Logger.Error("GuideChoice tag is missing required \"Value\" attribute.");
            return false;
        }

        if (!args.TryGetValue("Title", out var title) || string.IsNullOrWhiteSpace(title))
        {
            Logger.Error("GuideChoice tag is missing required \"Title\" attribute.");
            return false;
        }

        if (!args.TryGetValue("Description", out var description))
        {
            Logger.Error("GuideChoice tag is missing required \"Description\" attribute.");
            return false;
        }

        ChoiceValue = value;
        ChoiceButton.Text = title;

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(10, 8, 10, 8),
            SeparationOverride = 6,
        };

        content.AddChild(ChoiceButton);

        if (args.TryGetValue("Image", out var imagePath) && !string.IsNullOrWhiteSpace(imagePath))
        {
            if (_resources.TryGetResource<TextureResource>(imagePath, out var textureResource))
            {
                content.AddChild(new AspectTextureRect
                {
                    Texture = textureResource.Texture,
                    MaxBoundHeight = ChoiceImageMaxHeight,
                });
            }
            else
            {
                Logger.Warning($"GuideChoice image resource not found: \"{imagePath}\".");
            }
        }

        if (!string.IsNullOrEmpty(description))
            content.AddChild(GuideCVarHelper.CreateSubTextRichLabel(description));

        RemoveAllChildren();
        AddChild(content);

        control = this;
        return true;
    }
}
