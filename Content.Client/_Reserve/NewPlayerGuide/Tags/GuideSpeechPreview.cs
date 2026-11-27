using System.Diagnostics.CodeAnalysis;
using Content.Client._Reserve.Chat.UI;
using Content.Client.Guidebook.Richtext;
using Content.Goobstation.Common.CCVar;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Guidebook wrapper for <see cref="SpeechPreviewControl"/> that refreshes reactively on speech CVar changes.
/// </summary>
[UsedImplicitly]
public sealed class GuideSpeechPreview : BoxContainer, IDocumentTag
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly SpeechPreviewControl _preview = new();
    private readonly Action<bool> _onBoolChanged;
    private readonly Action<int> _onIntChanged;
    private readonly Action<float> _onFloatChanged;

    private bool _subscribed;

    public GuideSpeechPreview()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        AddChild(_preview);

        _onBoolChanged = _ => _preview.RefreshFromConfig();
        _onIntChanged = _ => _preview.RefreshFromConfig();
        _onFloatChanged = _ => _preview.RefreshFromConfig();
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = this;
        return true;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (!_subscribed)
        {
            _subscribed = true;
            _cfg.OnValueChanged(GoobCVars.ChatRunechat, _onBoolChanged);
            _cfg.OnValueChanged(GoobCVars.ChatRunechatFontSize, _onIntChanged);
            _cfg.OnValueChanged(GoobCVars.ChatRunechatOutlineScale, _onFloatChanged);
            _cfg.OnValueChanged(CCVars.ChatEnableFancyBubbles, _onBoolChanged);
            _cfg.OnValueChanged(CCVars.ChatFancyNameBackground, _onBoolChanged);
        }

        _preview.RefreshFromConfig();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (!_subscribed)
            return;

        _subscribed = false;
        _cfg.UnsubValueChanged(GoobCVars.ChatRunechat, _onBoolChanged);
        _cfg.UnsubValueChanged(GoobCVars.ChatRunechatFontSize, _onIntChanged);
        _cfg.UnsubValueChanged(GoobCVars.ChatRunechatOutlineScale, _onFloatChanged);
        _cfg.UnsubValueChanged(CCVars.ChatEnableFancyBubbles, _onBoolChanged);
        _cfg.UnsubValueChanged(CCVars.ChatFancyNameBackground, _onBoolChanged);
    }
}
