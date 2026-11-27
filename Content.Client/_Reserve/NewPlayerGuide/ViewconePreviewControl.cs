using System.Numerics;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Reserve.NewPlayerGuide;

/// <summary>
///     Renders a pre-composited miniature station corridor processed through the <c>Viewcone</c> shader
///     so players can preview the out-of-vision graininess setting directly in the guide.
/// </summary>
public sealed class ViewconePreviewControl : Control
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceCache _resources = default!;

    private const float PreviewMinHeight = 192f;
    private const float BaseTilePixels = 48f;
    private const string CorridorTexturePath = "/Textures/_Reserve/NewPlayerGuide/corridor_viewcone.png";
    private static readonly ProtoId<ShaderPrototype> ViewconeShaderId = "Viewcone";

    private static readonly Color BackgroundColor = Color.FromHex("#111318");
    private static readonly Color BorderColor = Color.FromHex("#2f333d");

    private const float ConeAngleDegrees = 270f;
    private const float ConeFeatherDegrees = 24f;
    private const float ConeIgnoreRadius = (1.35f - 0.2f) * 50f;
    private const float ConeIgnoreFeather = 0.2f * 200f;
    private const float PreviewIgnoreRadiusTiles = 1.15f;

    // ViewAngle = +pi/2 aligns the viewcone center with +X (East) in viewcone.swsl.
    private const float EastViewAngleRadians = MathF.PI * 0.5f;

    private Texture? _corridorTexture;
    private Texture? _edgeColumnTexture;
    private ShaderInstance? _viewconeShader;
    private IRenderTexture? _sceneTarget;
    private IRenderTexture? _effectTarget;

    private float _grainScale;
    private bool _assetsLoaded;
    private bool _subscribed;

    public ViewconePreviewControl()
    {
        IoCManager.InjectDependencies(this);

        HorizontalExpand = true;
        MinHeight = PreviewMinHeight;
        RectClipContent = true;
    }

    /// <summary>
    ///     Updates the preview from the current vision grain and reduced motion CVars.
    /// </summary>
    public void RefreshFromConfig()
    {
        var grainScale = Math.Clamp(_cfg.GetCVar(CCVars.VisionGrainScale), 0f, 1f);
        var reducedMotion = _cfg.GetCVar(CCVars.ReducedMotion);
        SetState(reducedMotion ? 0f : grainScale);
    }

    /// <summary>
    ///     Sets an explicit grain scale in [0, 1] for the shader preview.
    /// </summary>
    public void SetState(float grainScale)
    {
        _grainScale = Math.Clamp(grainScale, 0f, 1f);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        if (_subscribed)
            return;

        _subscribed = true;
        _cfg.OnValueChanged(CCVars.VisionGrainScale, OnGrainScaleChanged, invokeImmediately: true);
        _cfg.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        if (_subscribed)
        {
            _subscribed = false;
            _cfg.UnsubValueChanged(CCVars.VisionGrainScale, OnGrainScaleChanged);
            _cfg.UnsubValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged);
        }

        DisposeRenderTargets();
        _viewconeShader?.Dispose();
        _viewconeShader = null;
        _corridorTexture = null;
        _edgeColumnTexture = null;
        _assetsLoaded = false;
    }

    private void OnGrainScaleChanged(float _) => RefreshFromConfig();
    private void OnReducedMotionChanged(bool _) => RefreshFromConfig();

    protected override void Draw(IRenderHandle renderHandle)
    {
        var targetSize = PixelSize;
        if (targetSize.X <= 0 || targetSize.Y <= 0)
            return;

        if (!_assetsLoaded)
        {
            _assetsLoaded = true;
            LoadAssets();
        }

        EnsureRenderTargets(targetSize);
        if (_sceneTarget == null || _corridorTexture == null)
            return;

        var screen = renderHandle.DrawingHandleScreen;
        var outerTransform = screen.GetTransform();
        var outerModulate = screen.Modulate;

        var scale = targetSize.Y / (float) _corridorTexture.Height;

        renderHandle.RenderInRenderTarget(_sceneTarget, () =>
        {
            renderHandle.SetScissor(null);
            screen.SetTransform(Matrix3x2.Identity);
            screen.Modulate = Color.White;
            screen.UseShader(null);
            DrawCorridorScene(screen, targetSize, scale);
        }, BackgroundColor);

        var resultTexture = _sceneTarget.Texture;
        if (_viewconeShader != null && _effectTarget != null)
        {
            ConfigureShader(_sceneTarget.Texture, targetSize, scale);

            renderHandle.RenderInRenderTarget(_effectTarget, () =>
            {
                renderHandle.SetScissor(null);
                screen.SetTransform(Matrix3x2.Identity);
                screen.Modulate = Color.White;
                screen.UseShader(_viewconeShader);
                screen.DrawRect(PixelSizeBox, Color.White);
                screen.UseShader(null);
            }, Color.Transparent);

            resultTexture = _effectTarget.Texture;
        }

        screen.SetTransform(outerTransform);
        screen.Modulate = outerModulate;
        screen.UseShader(null);
        screen.DrawTextureRect(resultTexture, PixelSizeBox);
        DrawInnerBorder(screen, PixelSizeBox, BorderColor, MathF.Max(1f, MathF.Round(UIScale)));
    }

    private void LoadAssets()
    {
        if (_resources.TryGetResource<TextureResource>(CorridorTexturePath, out var res))
        {
            _corridorTexture = res.Texture;
            if (res.Texture.Width >= BaseTilePixels)
            {
                _edgeColumnTexture = new AtlasTexture(
                    res.Texture,
                    UIBox2.FromDimensions(0f, 0f, BaseTilePixels, res.Texture.Height));
            }
        }

        if (_proto.TryIndex(ViewconeShaderId, out var shaderProto))
            _viewconeShader = shaderProto.InstanceUnique();
    }

    private void DrawCorridorScene(DrawingHandleScreen screen, Vector2i targetSize, float scale)
    {
        if (_corridorTexture == null)
            return;

        var drawW = _corridorTexture.Width * scale;
        var drawH = _corridorTexture.Height * scale;
        var left = (targetSize.X - drawW) * 0.5f;
        var top = (targetSize.Y - drawH) * 0.5f;
        var bottom = top + drawH;

        if (_edgeColumnTexture != null)
        {
            var colW = BaseTilePixels * scale;
            for (var x = left; x > 0f; x -= colW)
                screen.DrawTextureRect(_edgeColumnTexture, new UIBox2(x - colW, top, x, bottom));

            for (var x = left + drawW; x < targetSize.X; x += colW)
                screen.DrawTextureRect(_edgeColumnTexture, new UIBox2(x, top, x + colW, bottom));
        }

        screen.DrawTextureRect(_corridorTexture, new UIBox2(left, top, left + drawW, bottom));
    }

    private void EnsureRenderTargets(Vector2i size)
    {
        if (_sceneTarget != null && _sceneTarget.Size == size &&
            _effectTarget != null && _effectTarget.Size == size)
        {
            return;
        }

        DisposeRenderTargets();

        var format = new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb);
        var sample = new TextureSampleParameters { Filter = false };
        _sceneTarget = _clyde.CreateRenderTarget(size, format, sample, $"{nameof(ViewconePreviewControl)}-scene");
        _effectTarget = _clyde.CreateRenderTarget(size, format, sample, $"{nameof(ViewconePreviewControl)}-effect");
    }

    private void DisposeRenderTargets()
    {
        _sceneTarget?.Dispose();
        _sceneTarget = null;
        _effectTarget?.Dispose();
        _effectTarget = null;
    }

    private void ConfigureShader(Texture sceneTexture, Vector2i targetSize, float scale)
    {
        if (_viewconeShader == null)
            return;

        var tileSize = BaseTilePixels * scale;
        var desiredInnerPx = PreviewIgnoreRadiusTiles * tileSize;
        var zoom = (ConeIgnoreRadius / MathF.Pow(ConeIgnoreFeather, 0.85f))
                   * (ConeIgnoreFeather * 0.001f * targetSize.Y)
                   / desiredInnerPx;

        _viewconeShader.SetParameter("SCREEN_TEXTURE", sceneTexture);
        _viewconeShader.SetParameter("Zoom", zoom);
        _viewconeShader.SetParameter("ViewAngle", EastViewAngleRadians);
        _viewconeShader.SetParameter("ConeAngle", MathHelper.DegreesToRadians(ConeAngleDegrees));
        _viewconeShader.SetParameter("ConeFeather", MathHelper.DegreesToRadians(ConeFeatherDegrees));
        _viewconeShader.SetParameter("ConeIgnoreRadius", ConeIgnoreRadius);
        _viewconeShader.SetParameter("ConeIgnoreFeather", ConeIgnoreFeather);
        _viewconeShader.SetParameter("GrainScale", _grainScale);
        _viewconeShader.SetParameter("Offset", Vector2.Zero);
    }

    private static void DrawInnerBorder(DrawingHandleScreen screen, UIBox2 box, Color color, float thickness)
    {
        if (thickness <= 0f || box.Width <= 0f || box.Height <= 0f)
            return;

        var t = MathF.Min(thickness, MathF.Min(box.Width, box.Height) * 0.5f);
        screen.DrawRect(new UIBox2(box.Left, box.Top, box.Right, box.Top + t), color);
        screen.DrawRect(new UIBox2(box.Left, box.Bottom - t, box.Right, box.Bottom), color);
        screen.DrawRect(new UIBox2(box.Left, box.Top + t, box.Left + t, box.Bottom - t), color);
        screen.DrawRect(new UIBox2(box.Right - t, box.Top + t, box.Right, box.Bottom - t), color);
    }
}
