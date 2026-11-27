using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Guidebook checkbox bound to a bool CVar, optionally enabled/disabled by another bool CVar.
/// </summary>
[UsedImplicitly]
public sealed class GuideCVarCheckBox : CheckBox, IDocumentTag
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly Action<bool> _onCVarChanged;
    private readonly Action<bool> _onEnabledChanged;

    private string _cvarName = string.Empty;
    private string? _enabledByCVar;
    private bool _subscribed;
    private bool _updatingFromConfig;

    public GuideCVarCheckBox()
    {
        IoCManager.InjectDependencies(this);

        HorizontalExpand = true;
        OnToggled += OnCheckBoxToggled;

        _onCVarChanged = OnCVarChanged;
        _onEnabledChanged = OnEnabledChanged;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!args.TryGetValue("CVar", out var cvarName) || string.IsNullOrWhiteSpace(cvarName))
        {
            Logger.Error("GuideCVarCheckBox tag is missing required \"CVar\" attribute.");
            return false;
        }

        if (!_cfg.IsCVarRegistered(cvarName) || _cfg.GetCVarType(cvarName) != typeof(bool))
        {
            Logger.Error($"GuideCVarCheckBox CVar \"{cvarName}\" is not a registered bool CVar.");
            return false;
        }

        if (!args.TryGetValue("Text", out var text) || string.IsNullOrWhiteSpace(text))
        {
            Logger.Error("GuideCVarCheckBox tag is missing required \"Text\" attribute.");
            return false;
        }

        string? enabledByCVar = null;
        if (args.TryGetValue("EnabledByCVar", out var enabledStr) && !string.IsNullOrWhiteSpace(enabledStr))
        {
            if (!_cfg.IsCVarRegistered(enabledStr) || _cfg.GetCVarType(enabledStr) != typeof(bool))
            {
                Logger.Error($"GuideCVarCheckBox EnabledByCVar \"{enabledStr}\" is not a registered bool CVar.");
                return false;
            }

            enabledByCVar = enabledStr;
        }

        _cvarName = cvarName;
        _enabledByCVar = enabledByCVar;
        Text = text;

        OnCVarChanged(_cfg.GetCVar<bool>(_cvarName));
        if (_enabledByCVar != null)
            OnEnabledChanged(_cfg.GetCVar<bool>(_enabledByCVar));

        control = this;
        return true;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (_subscribed || string.IsNullOrEmpty(_cvarName))
            return;

        _subscribed = true;
        _cfg.OnValueChanged(_cvarName, _onCVarChanged, invokeImmediately: true);

        if (_enabledByCVar != null)
            _cfg.OnValueChanged(_enabledByCVar, _onEnabledChanged, invokeImmediately: true);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (!_subscribed)
            return;

        _subscribed = false;
        _cfg.UnsubValueChanged(_cvarName, _onCVarChanged);

        if (_enabledByCVar != null)
            _cfg.UnsubValueChanged(_enabledByCVar, _onEnabledChanged);
    }

    private void OnCheckBoxToggled(ButtonToggledEventArgs args)
    {
        if (_updatingFromConfig || string.IsNullOrEmpty(_cvarName))
            return;

        GuideCVarHelper.SetCVarIfChanged(_cfg, _cvarName, args.Pressed, this);
    }

    private void OnCVarChanged(bool value)
    {
        _updatingFromConfig = true;
        try
        {
            Pressed = value;
        }
        finally
        {
            _updatingFromConfig = false;
        }
    }

    private void OnEnabledChanged(bool enabled)
    {
        Disabled = !enabled;
    }
}
