using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Content.Client.Guidebook.Richtext;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

/// <summary>
///     Guidebook slider bound to an int or float CVar, with optional reactive disable-state and subtext notes.
/// </summary>
[UsedImplicitly]
public sealed class GuideCVarSlider : BoxContainer, IDocumentTag
{
    private enum ValueFormat
    {
        Percent,
        Int,
    }

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly Label _titleLabel;
    private readonly Slider _slider;
    private readonly Label _valueLabel;
    private readonly Action<object> _onCVarChanged;
    private readonly Action<bool> _onDisabledChanged;

    private RichTextLabel? _disabledNoteLabel;
    private string _cvarName = string.Empty;
    private string? _disabledByCVar;
    private bool _isIntCVar;
    private ValueFormat _format = ValueFormat.Percent;
    private bool _subscribed;
    private bool _updatingFromConfig;

    public GuideCVarSlider()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        SeparationOverride = 6;

        _titleLabel = new Label
        {
            MinWidth = 180,
            VerticalAlignment = VAlignment.Center,
        };

        _slider = new Slider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };

        _valueLabel = new Label
        {
            MinWidth = 48,
            Align = Label.AlignMode.Right,
            VerticalAlignment = VAlignment.Center,
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
            Children =
            {
                _titleLabel,
                _slider,
                _valueLabel,
            },
        };
        AddChild(row);

        _slider.OnValueChanged += OnSliderValueChanged;

        _onCVarChanged = OnCVarChanged;
        _onDisabledChanged = OnDisabledChanged;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!args.TryGetValue("CVar", out var cvarName) || string.IsNullOrWhiteSpace(cvarName))
        {
            Logger.Error("GuideCVarSlider tag is missing required \"CVar\" attribute.");
            return false;
        }

        if (!_cfg.IsCVarRegistered(cvarName))
        {
            Logger.Error($"GuideCVarSlider references unregistered CVar \"{cvarName}\".");
            return false;
        }

        var cvarType = _cfg.GetCVarType(cvarName);
        if (cvarType != typeof(float) && cvarType != typeof(int))
        {
            Logger.Error($"GuideCVarSlider CVar \"{cvarName}\" must be float or int, got {cvarType.Name}.");
            return false;
        }

        if (!args.TryGetValue("Title", out var title) || string.IsNullOrWhiteSpace(title))
        {
            Logger.Error("GuideCVarSlider tag is missing required \"Title\" attribute.");
            return false;
        }

        var min = 0f;
        if (args.TryGetValue("Min", out var minStr) &&
            !float.TryParse(minStr, NumberStyles.Float, CultureInfo.InvariantCulture, out min))
        {
            Logger.Error($"GuideCVarSlider has invalid Min value \"{minStr}\".");
            return false;
        }

        var max = 1f;
        if (args.TryGetValue("Max", out var maxStr) &&
            !float.TryParse(maxStr, NumberStyles.Float, CultureInfo.InvariantCulture, out max))
        {
            Logger.Error($"GuideCVarSlider has invalid Max value \"{maxStr}\".");
            return false;
        }

        if (min >= max)
        {
            Logger.Error($"GuideCVarSlider requires Min ({min}) < Max ({max}).");
            return false;
        }

        var format = ValueFormat.Percent;
        if (args.TryGetValue("Format", out var formatStr) && !string.IsNullOrWhiteSpace(formatStr))
        {
            if (string.Equals(formatStr, "Percent", StringComparison.OrdinalIgnoreCase))
                format = ValueFormat.Percent;
            else if (string.Equals(formatStr, "Int", StringComparison.OrdinalIgnoreCase))
                format = ValueFormat.Int;
            else
            {
                Logger.Error($"GuideCVarSlider has unsupported Format \"{formatStr}\".");
                return false;
            }
        }

        string? disabledByCVar = null;
        if (args.TryGetValue("DisabledByCVar", out var disabledStr) && !string.IsNullOrWhiteSpace(disabledStr))
        {
            if (!_cfg.IsCVarRegistered(disabledStr) || _cfg.GetCVarType(disabledStr) != typeof(bool))
            {
                Logger.Error($"GuideCVarSlider DisabledByCVar \"{disabledStr}\" is not a registered bool CVar.");
                return false;
            }

            disabledByCVar = disabledStr;
        }

        _updatingFromConfig = true;
        try
        {
            _cvarName = cvarName;
            _isIntCVar = cvarType == typeof(int);
            _format = format;
            _disabledByCVar = disabledByCVar;

            _titleLabel.Text = title;
            _slider.Rounded = _isIntCVar;
            if (min > _slider.MaxValue)
            {
                _slider.MaxValue = max;
                _slider.MinValue = min;
            }
            else
            {
                _slider.MinValue = min;
                _slider.MaxValue = max;
            }

            if (args.TryGetValue("Note", out var note) && !string.IsNullOrEmpty(note))
                AddChild(GuideCVarHelper.CreateSubTextRichLabel(note));

            if (args.TryGetValue("DisabledNote", out var disabledNote) && !string.IsNullOrEmpty(disabledNote))
            {
                _disabledNoteLabel = GuideCVarHelper.CreateSubTextRichLabel(disabledNote);
                _disabledNoteLabel.Visible = false;
                AddChild(_disabledNoteLabel);
            }

            SyncSliderFromValue(_cfg.GetCVar(_cvarName));
            if (_disabledByCVar != null)
                OnDisabledChanged(_cfg.GetCVar<bool>(_disabledByCVar));
        }
        finally
        {
            _updatingFromConfig = false;
        }

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

        if (_disabledByCVar != null)
            _cfg.OnValueChanged<bool>(_disabledByCVar, _onDisabledChanged, invokeImmediately: true);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (!_subscribed)
            return;

        _subscribed = false;
        _cfg.UnsubValueChanged<object>(_cvarName, _onCVarChanged);

        if (_disabledByCVar != null)
            _cfg.UnsubValueChanged<bool>(_disabledByCVar, _onDisabledChanged);
    }

    private void OnSliderValueChanged(Robust.Client.UserInterface.Controls.Range args)
    {
        if (_updatingFromConfig || string.IsNullOrEmpty(_cvarName))
            return;

        _valueLabel.Text = FormatValue(args.Value);
        // Keep int and float branches separately typed so conditional inference does not box int as float.
        object targetValue = _isIntCVar
            ? (int) MathF.Round(args.Value)
            : (object) args.Value;
        GuideCVarHelper.SetCVarIfChanged(_cfg, _cvarName, targetValue, this);
    }

    private void OnCVarChanged(object value)
    {
        SyncSliderFromValue(value);
    }

    private void SyncSliderFromValue(object value)
    {
        var floatValue = value switch
        {
            float f => f,
            int i => i,
            _ => throw new InvalidOperationException($"Unexpected CVar value type {value.GetType().Name} for \"{_cvarName}\"."),
        };

        var wasUpdating = _updatingFromConfig;
        _updatingFromConfig = true;
        try
        {
            _slider.SetValueWithoutEvent(floatValue);
            _valueLabel.Text = FormatValue(_slider.Value);
        }
        finally
        {
            _updatingFromConfig = wasUpdating;
        }
    }

    private void OnDisabledChanged(bool disabled)
    {
        _slider.Disabled = disabled;
        _slider.Modulate = disabled ? Color.White.WithAlpha(0.5f) : Color.White;
        _valueLabel.FontColorOverride = disabled ? Color.DarkGray : null;

        if (_disabledNoteLabel != null)
            _disabledNoteLabel.Visible = disabled;
    }

    private string FormatValue(float value)
    {
        return _format switch
        {
            ValueFormat.Int => $"{(int) MathF.Round(value)}",
            ValueFormat.Percent => $"{MathF.Round(value * 100f)}%",
            _ => $"{MathF.Round(value * 100f)}%",
        };
    }
}
