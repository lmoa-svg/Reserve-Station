using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Content.Client.Guidebook.Richtext;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Guidebook tag that loads a <see cref="TextureResource"/> and renders it centered with aspect ratio preserved.
/// </summary>
[UsedImplicitly]
public sealed class GuideImage : BoxContainer, IDocumentTag
{
    [Dependency] private readonly IResourceCache _resources = default!;

    private readonly AspectTextureRect _textureRect;

    public GuideImage()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Horizontal;
        HorizontalExpand = true;
        Align = AlignMode.Center;

        _textureRect = new AspectTextureRect();
        AddChild(_textureRect);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Path", out var path) || string.IsNullOrWhiteSpace(path))
        {
            Logger.Error("GuideImage tag is missing required \"Path\" attribute.");
            return false;
        }

        float? maxWidth = null;
        if (args.TryGetValue("MaxWidth", out var maxWidthStr))
        {
            if (!float.TryParse(maxWidthStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedWidth) ||
                parsedWidth <= 0f)
            {
                Logger.Error($"GuideImage tag has invalid MaxWidth \"{maxWidthStr}\".");
                return false;
            }

            maxWidth = parsedWidth;
        }

        float? maxHeight = null;
        if (args.TryGetValue("MaxHeight", out var maxHeightStr))
        {
            if (!float.TryParse(maxHeightStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHeight) ||
                parsedHeight <= 0f)
            {
                Logger.Error($"GuideImage tag has invalid MaxHeight \"{maxHeightStr}\".");
                return false;
            }

            maxHeight = parsedHeight;
        }

        if (!_resources.TryGetResource<TextureResource>(path, out var textureResource))
        {
            Logger.Error($"GuideImage tag could not load texture \"{path}\".");
            return false;
        }

        _textureRect.Texture = textureResource.Texture;
        _textureRect.MaxBoundWidth = maxWidth;
        _textureRect.MaxBoundHeight = maxHeight;

        control = this;
        return true;
    }
}
