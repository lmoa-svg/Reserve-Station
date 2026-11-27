using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Lays out mutually exclusive <see cref="GuideChoice"/> cards bound to a CVar in equal-width columns.
/// </summary>
[UsedImplicitly]
public sealed class GuideChoiceRow : BoxContainer, IDocumentTag
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private sealed record ChoiceEntry(GuideChoice Choice, object ParsedValue);

    private readonly EqualColumnsContainer _columns;
    private readonly List<ChoiceEntry> _choices = new();
    private readonly Action<object> _onCVarChanged;

    private string _cvarName = string.Empty;
    private bool _reparenting;
    private bool _subscribed;
    private bool _updatingFromConfig;

    public GuideChoiceRow()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        _columns = new EqualColumnsContainer
        {
            HorizontalExpand = true,
            Separation = 8,
        };
        AddChild(_columns);

        _onCVarChanged = OnCVarChanged;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("CVar", out var cvarName) || string.IsNullOrWhiteSpace(cvarName))
        {
            Logger.Error("GuideChoiceRow tag is missing required \"CVar\" attribute.");
            return false;
        }

        if (!_cfg.IsCVarRegistered(cvarName))
        {
            Logger.Error($"GuideChoiceRow references unregistered CVar \"{cvarName}\".");
            return false;
        }

        _cvarName = cvarName;
        control = this;
        return true;
    }

    protected override void ChildAdded(Control newChild)
    {
        base.ChildAdded(newChild);

        if (_reparenting || newChild == _columns)
            return;

        _reparenting = true;
        try
        {
            RemoveChild(newChild);

            if (newChild is not GuideChoice choice)
            {
                Logger.Warning($"GuideChoiceRow ignored unexpected child control \"{newChild.GetType().Name}\".");
                return;
            }

            if (!GuideCVarHelper.TryParseCVarValue(_cfg, _cvarName, choice.ChoiceValue, out var parsedValue))
            {
                Logger.Error($"GuideChoice value \"{choice.ChoiceValue}\" is invalid for CVar \"{_cvarName}\".");
                return;
            }

            var entry = new ChoiceEntry(choice, parsedValue);
            _choices.Add(entry);
            WireChoice(entry);
            _columns.AddChild(choice);

            if (IsInsideTree)
            {
                WireButtonGroup();
                SyncFromValue(_cfg.GetCVar(_cvarName));
            }
        }
        finally
        {
            _reparenting = false;
        }
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        WireButtonGroup();

        if (_subscribed || string.IsNullOrEmpty(_cvarName) || !_cfg.IsCVarRegistered(_cvarName))
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

    private void WireChoice(ChoiceEntry entry)
    {
        entry.Choice.ChoiceButton.OnToggled += args =>
        {
            if (args.Pressed)
                OnChoiceSelected(entry);
            else
                UpdateCardSelection();
        };

        entry.Choice.OnKeyBindDown += args =>
        {
            if (args.Function != EngineKeyFunctions.UIClick || entry.Choice.ChoiceButton.Disabled)
                return;

            if (!entry.Choice.ChoiceButton.Pressed)
            {
                entry.Choice.ChoiceButton.SetClickPressed(true);
                OnChoiceSelected(entry);
            }

            args.Handle();
        };
    }

    private void WireButtonGroup()
    {
        if (_choices.Count == 0)
            return;

        var group = new ButtonGroup(isNoneSetAllowed: false);
        foreach (var entry in _choices)
        {
            entry.Choice.ChoiceButton.Group = group;
        }
    }

    private void OnChoiceSelected(ChoiceEntry entry)
    {
        if (_updatingFromConfig || string.IsNullOrEmpty(_cvarName))
            return;

        if (!GuideCVarHelper.SetCVarIfChanged(_cfg, _cvarName, entry.ParsedValue, this))
            UpdateCardSelection();
    }

    private void OnCVarChanged(object value)
    {
        SyncFromValue(value);
    }

    private void SyncFromValue(object value)
    {
        ChoiceEntry? matched = null;
        foreach (var entry in _choices)
        {
            if (GuideCVarHelper.ValuesEqualIgnoreCase(value, entry.ParsedValue))
            {
                matched = entry;
                break;
            }
        }

        _updatingFromConfig = true;
        try
        {
            if (matched != null)
                matched.Choice.ChoiceButton.Pressed = true;

            foreach (var entry in _choices)
            {
                entry.Choice.SetSelectedVisual(entry == matched);
            }
        }
        finally
        {
            _updatingFromConfig = false;
        }
    }

    private void UpdateCardSelection()
    {
        foreach (var entry in _choices)
        {
            entry.Choice.SetSelectedVisual(entry.Choice.ChoiceButton.Pressed);
        }
    }
}
