using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Colorspace;
using Content.Client.UserInterface.ControlExtensions;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client._Reserve.NewPlayerGuide.Tags;

internal static class GuideCVarHelper
{
    public static readonly Color SelectedCardModulate =
        ReserveRedStylesheet.SecondaryPalette.Background.OkBlend(ReserveRedStylesheet.HighlightPalette.Base, 0.35f);

    /// <summary>
    ///     Notifies the nearest ancestor implementing <see cref="INewPlayerGuideHost"/> that a setting changed.
    /// </summary>
    public static void NotifyHostSettingChanged(Control source)
    {
        if (source.TryGetParentHandler<INewPlayerGuideHost>(out var host))
            host.NotifySettingChanged();
    }

    /// <summary>
    ///     Creates a small subtext <see cref="RichTextLabel"/> matching NewPlayerGuide card styling.
    /// </summary>
    public static RichTextLabel CreateSubTextRichLabel(string markupText)
    {
        var label = new RichTextLabel
        {
            // LabelSheetlet's LabelSubText rule only targets Label, so use FontSmall (10pt) with explicit DarkGray.
            StyleClasses = { StyleClass.FontSmall },
            HorizontalExpand = true,
        };
        label.SetMessage(FormattedMessage.FromMarkupPermissive(markupText), tagsAllowed: null, defaultColor: Color.DarkGray);
        return label;
    }

    /// <summary>
    ///     Parses a string value into the registered CLR type of <paramref name="cvarName"/>.
    /// </summary>
    public static bool TryParseCVarValue(
        IConfigurationManager cfg,
        string cvarName,
        string rawValue,
        [NotNullWhen(true)] out object? parsed)
    {
        parsed = null;
        if (!cfg.IsCVarRegistered(cvarName))
            return false;

        var type = cfg.GetCVarType(cvarName);
        if (type == typeof(string))
        {
            parsed = rawValue;
            return true;
        }

        if (type == typeof(bool))
        {
            if (bool.TryParse(rawValue, out var boolValue))
            {
                parsed = boolValue;
                return true;
            }

            return false;
        }

        if (type == typeof(int))
        {
            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                parsed = intValue;
                return true;
            }

            return false;
        }

        if (type == typeof(float))
        {
            if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
            {
                parsed = floatValue;
                return true;
            }

            return false;
        }

        if (type == typeof(double))
        {
            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
            {
                parsed = doubleValue;
                return true;
            }

            return false;
        }

        if (type == typeof(long))
        {
            if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
            {
                parsed = longValue;
                return true;
            }

            return false;
        }

        if (type == typeof(ushort))
        {
            if (ushort.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ushortValue))
            {
                parsed = ushortValue;
                return true;
            }

            return false;
        }

        if (type.IsEnum && Enum.TryParse(type, rawValue, ignoreCase: true, out var enumValue) && enumValue != null)
        {
            parsed = enumValue;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Compares two CVar values for equality, treating strings case-insensitively for section/choice matching.
    /// </summary>
    public static bool ValuesEqualIgnoreCase(object? a, object? b)
    {
        if (a is null || b is null)
            return ReferenceEquals(a, b);

        if (a is string sa && b is string sb)
            return string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase);

        if (a is float fa && b is float fb)
            return MathF.Abs(fa - fb) <= 1e-5f;

        if (a is double da && b is double db)
            return Math.Abs(da - db) <= 1e-6;

        return Equals(a, b);
    }

    /// <summary>
    ///     Compares two CVar values strictly to determine whether writing would change the stored value.
    /// </summary>
    public static bool ValuesEqualExact(object? a, object? b)
    {
        if (a is null || b is null)
            return ReferenceEquals(a, b);

        if (a is float fa && b is float fb)
            return MathF.Abs(fa - fb) <= 1e-5f;

        if (a is double da && b is double db)
            return Math.Abs(da - db) <= 1e-6;

        return Equals(a, b);
    }

    /// <summary>
    ///     Writes <paramref name="newValue"/> to <paramref name="cvarName"/> and notifies the guide host if it changed.
    /// </summary>
    public static bool SetCVarIfChanged(
        IConfigurationManager cfg,
        string cvarName,
        object newValue,
        Control source)
    {
        var current = cfg.GetCVar(cvarName);
        if (ValuesEqualExact(current, newValue))
            return false;

        cfg.SetCVar(cvarName, newValue);
        NotifyHostSettingChanged(source);
        return true;
    }
}

/// <summary>
///     <see cref="TextureRect"/> that preserves aspect ratio and fits within optional max bounds and available width.
/// </summary>
internal sealed class AspectTextureRect : TextureRect
{
    public float? MaxBoundWidth { get; set; }
    public float? MaxBoundHeight { get; set; }

    public AspectTextureRect()
    {
        Stretch = StretchMode.KeepAspectCentered;
        HorizontalAlignment = HAlignment.Center;
        VerticalAlignment = VAlignment.Center;
        CanShrink = true;
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var texture = Texture;
        if (texture == null)
            return Vector2.Zero;

        var texSize = TextureSizeTarget;
        if (texSize.X <= 0f || texSize.Y <= 0f)
            return Vector2.Zero;

        var limitW = MaxBoundWidth ?? texSize.X;
        var limitH = MaxBoundHeight ?? texSize.Y;

        if (float.IsFinite(availableSize.X))
            limitW = MathF.Min(limitW, availableSize.X);

        if (float.IsFinite(availableSize.Y))
            limitH = MathF.Min(limitH, availableSize.Y);

        var scale = MathF.Min(1f, MathF.Min(limitW / texSize.X, limitH / texSize.Y));
        scale = MathF.Max(0f, scale);

        return new Vector2(texSize.X * scale, texSize.Y * scale);
    }
}
