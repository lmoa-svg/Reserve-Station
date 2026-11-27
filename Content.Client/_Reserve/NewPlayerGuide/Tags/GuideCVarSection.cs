using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Vertical container whose <see cref="Control.Visible"/> tracks whether a CVar equals a target value.
/// </summary>
[UsedImplicitly]
public sealed class GuideCVarSection : BoxContainer, IDocumentTag
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly Action<object> _onCVarChanged;

    private string _cvarName = string.Empty;
    private object _targetValue = string.Empty;
    private bool _subscribed;

    public GuideCVarSection()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        SeparationOverride = 8;

        _onCVarChanged = OnCVarChanged;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!args.TryGetValue("CVar", out var cvarName) || string.IsNullOrWhiteSpace(cvarName))
        {
            Logger.Error("GuideCVarSection tag is missing required \"CVar\" attribute.");
            return false;
        }

        if (!_cfg.IsCVarRegistered(cvarName))
        {
            Logger.Error($"GuideCVarSection references unregistered CVar \"{cvarName}\".");
            return false;
        }

        if (!args.TryGetValue("Equals", out var equalsValue))
        {
            Logger.Error("GuideCVarSection tag is missing required \"Equals\" attribute.");
            return false;
        }

        if (!GuideCVarHelper.TryParseCVarValue(_cfg, cvarName, equalsValue, out var parsedValue))
        {
            Logger.Error($"GuideCVarSection Equals value \"{equalsValue}\" is invalid for CVar \"{cvarName}\".");
            return false;
        }

        _cvarName = cvarName;
        _targetValue = parsedValue;
        OnCVarChanged(_cfg.GetCVar(_cvarName));

        control = this;
        return true;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (_subscribed || string.IsNullOrEmpty(_cvarName))
            return;

        _subscribed = true;
        _cfg.OnValueChanged<object>(_cvarName, _onCVarChanged, invokeImmediately: true);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (!_subscribed)
            return;

        _subscribed = false;
        _cfg.UnsubValueChanged<object>(_cvarName, _onCVarChanged);
    }

    private void OnCVarChanged(object value)
    {
        Visible = GuideCVarHelper.ValuesEqualIgnoreCase(value, _targetValue);
    }
}
