using System.Numerics;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Input;
using Robust.Shared.Noise;
using Robust.Shared.Timing;

namespace Content.Client._Reserve.NewPlayerGuide;

/// <summary>
///     Renders a looping fist-punch demo inside a miniature station corridor so players can preview
///     how <see cref="CCVars.ScreenShakeIntensity"/> affects hit impact camera shake.
/// </summary>
public sealed class ScreenShakePreviewControl : Control
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resources = default!;

    private const float PreviewMinHeight = 192f;
    private const float BaseTilePixels = 48f;

    private const string CorridorBgPath = "/Textures/_Reserve/NewPlayerGuide/corridor_screenshake_bg.png";
    private const string CaptainTexturePath = "/Textures/_Reserve/NewPlayerGuide/character_captain.png";
    private const string FitnessRsiPath = "/Textures/_Goobstation/Structures/Specific/fitness.rsi";
    private const string ArcsRsiPath = "/Textures/Effects/arcs.rsi";

    private static readonly Color BackgroundColor = Color.FromHex("#111318");
    private static readonly Color BorderColor = Color.FromHex("#2f333d");

    private const float CycleDurationSeconds = 1.25f;
    private const float LungeDurationSeconds = 0.10f;
    private const float ImpactStartSeconds = 0.04f;
    private const float PunchEffectDurationSeconds = 0.30f;
    private const float BagWobbleDurationSeconds = 0.85f;

    // Matches MeleeUserShake in SharedMeleeWeaponSystem.Goob.cs (trauma: 0.08, decay: 1.0, freq: 0.009)
    // and ESScreenshakeSystem maxAngleDegrees = 20f.
    private const float ShakeTrauma = 0.08f;
    private const float ShakeDecayRate = 1.0f;
    private const float ShakeFrequency = 0.009f;
    private const float ShakeMaxAngleDegrees = 20f;

    private readonly FastNoiseLite _shakeNoise = new(67 + 420);

    private IRenderTexture? _sceneTarget;
    private bool _assetsLoaded;
    private bool _subscribed;

    private float _intensity = 1f;
    private float _cycleTime;
    private int _punchCount;

    private Texture? _bgTexture;
    private Texture? _edgeColumnTexture;
    private Texture? _wallCapTexture;
    private Texture? _captainTexture;
    private Texture? _punchingBagIdle;
    private RsiAnimation? _punchingBagHitAnim;
    private Texture? _fistTexture;
    private RsiAnimation? _punchBurstAnim;

    public ScreenShakePreviewControl()
    {
        IoCManager.InjectDependencies(this);

        HorizontalExpand = true;
        MinHeight = PreviewMinHeight;
        RectClipContent = true;
        MouseFilter = MouseFilterMode.Pass;
        DefaultCursorShape = CursorShape.Hand;

        _shakeNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _shakeNoise.SetFrequency(ShakeFrequency);
    }

    /// <summary>
    ///     Updates the preview from the current screen shake intensity and reduced motion CVars.
    /// </summary>
    public void RefreshFromConfig()
    {
        var intensity = Math.Clamp(_cfg.GetCVar(CCVars.ScreenShakeIntensity), 0f, 1f);
        var reducedMotion = _cfg.GetCVar(CCVars.ReducedMotion);
        SetState(reducedMotion ? 0f : intensity);
    }

    /// <summary>
    ///     Sets an explicit screen shake intensity in [0, 1] for the preview.
    /// </summary>
    public void SetState(float intensity)
    {
        _intensity = Math.Clamp(intensity, 0f, 1f);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _cycleTime = 0f;
        if (_subscribed)
            return;

        _subscribed = true;
        _cfg.OnValueChanged(CCVars.ScreenShakeIntensity, OnIntensityChanged, invokeImmediately: true);
        _cfg.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        if (_subscribed)
        {
            _subscribed = false;
            _cfg.UnsubValueChanged(CCVars.ScreenShakeIntensity, OnIntensityChanged);
            _cfg.UnsubValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged);
        }

        _sceneTarget?.Dispose();
        _sceneTarget = null;
        _bgTexture = null;
        _edgeColumnTexture = null;
        _wallCapTexture = null;
        _captainTexture = null;
        _punchingBagIdle = null;
        _punchingBagHitAnim = null;
        _fistTexture = null;
        _punchBurstAnim = null;
        _assetsLoaded = false;
    }

    private void OnIntensityChanged(float _) => RefreshFromConfig();
    private void OnReducedMotionChanged(bool _) => RefreshFromConfig();

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _cycleTime += args.DeltaSeconds;
        if (_cycleTime >= CycleDurationSeconds)
        {
            _cycleTime %= CycleDurationSeconds;
            _punchCount++;
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _cycleTime = 0f;
        _punchCount++;
        args.Handle();
    }

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

        EnsureRenderTarget(targetSize);
        if (_sceneTarget == null)
            return;

        var screen = renderHandle.DrawingHandleScreen;
        var outerTransform = screen.GetTransform();
        var outerModulate = screen.Modulate;

        renderHandle.RenderInRenderTarget(_sceneTarget, () =>
        {
            renderHandle.SetScissor(null);
            screen.SetTransform(Matrix3x2.Identity);
            screen.Modulate = Color.White;
            screen.UseShader(null);
            DrawWorld(screen, targetSize);
        }, BackgroundColor);

        screen.SetTransform(outerTransform);
        screen.Modulate = outerModulate;
        screen.UseShader(null);
        screen.DrawTextureRect(_sceneTarget.Texture, PixelSizeBox);
        DrawInnerBorder(screen, PixelSizeBox, BorderColor, MathF.Max(1f, MathF.Round(UIScale)));
    }

    private void LoadAssets()
    {
        if (_resources.TryGetResource<TextureResource>(CorridorBgPath, out var bgRes))
        {
            _bgTexture = bgRes.Texture;
            if (bgRes.Texture.Width >= BaseTilePixels && bgRes.Texture.Height >= 12)
            {
                _edgeColumnTexture = new AtlasTexture(
                    bgRes.Texture,
                    UIBox2.FromDimensions(0f, 0f, BaseTilePixels, bgRes.Texture.Height));
                _wallCapTexture = new AtlasTexture(
                    bgRes.Texture,
                    UIBox2.FromDimensions(0f, 0f, BaseTilePixels, 12f));
            }
        }

        if (_resources.TryGetResource<TextureResource>(CaptainTexturePath, out var captainRes))
            _captainTexture = captainRes.Texture;

        _punchingBagIdle = LoadRsiFrame(FitnessRsiPath, "punchingbagsyndie");
        _punchingBagHitAnim = LoadRsiAnimation(FitnessRsiPath, "punchingbagsyndie2");
        _fistTexture = LoadRsiFrame(ArcsRsiPath, "fist");
        _punchBurstAnim = LoadRsiAnimation(ArcsRsiPath, "punch");
    }

    private void EnsureRenderTarget(Vector2i size)
    {
        if (_sceneTarget != null && _sceneTarget.Size == size)
            return;

        _sceneTarget?.Dispose();
        _sceneTarget = null;

        var format = new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb);
        var sample = new TextureSampleParameters { Filter = false };
        _sceneTarget = _clyde.CreateRenderTarget(size, format, sample, $"{nameof(ScreenShakePreviewControl)}-scene");
    }

    private void DrawWorld(DrawingHandleScreen screen, Vector2i targetSize)
    {
        var scale = _bgTexture != null
            ? targetSize.Y / (float) _bgTexture.Height
            : UIScale;
        var tileSize = BaseTilePixels * scale;
        var center = (Vector2) targetSize * 0.5f;

        var cameraTransform = ComputeCameraTransform(center, tileSize);
        screen.SetTransform(cameraTransform);

        DrawBackground(screen, targetSize, scale, tileSize);

        var impactElapsed = _cycleTime - ImpactStartSeconds;
        var bagRecoil = impactElapsed is >= 0f and < 0.25f
            ? MathF.Sin(impactElapsed / 0.25f * MathF.PI) * 0.08f * tileSize
            : 0f;
        var bagCenter = center + new Vector2(0.55f * tileSize + bagRecoil, -0.04f * tileSize);
        var bagTexture = impactElapsed is >= 0f and < BagWobbleDurationSeconds && _punchingBagHitAnim != null
            ? _punchingBagHitAnim.GetFrameAt(impactElapsed)
            : _punchingBagIdle;
        DrawCenteredSprite(screen, bagTexture, bagCenter, tileSize);

        var lungeOffset = ComputeLungeOffset(_cycleTime, tileSize);
        var attackerCenter = center + new Vector2(-0.55f * tileSize + lungeOffset, 0f);
        DrawCenteredSprite(screen, _captainTexture, attackerCenter, tileSize);

        if (impactElapsed is >= 0f and < PunchEffectDurationSeconds)
        {
            var fade = 1f - impactElapsed / PunchEffectDurationSeconds;
            var impactColor = Color.White.WithAlpha(fade);

            if (_punchBurstAnim != null)
            {
                var burstFrame = _punchBurstAnim.GetFrameAt(impactElapsed);
                DrawCenteredSprite(screen, burstFrame, bagCenter, tileSize * 0.9f, impactColor);
            }

            if (_fistTexture != null)
            {
                var fistCenter = Vector2.Lerp(attackerCenter, bagCenter, 0.72f);
                var fistSize = tileSize * 0.65f;
                var fistHalf = fistSize * 0.5f;
                var fistTransform = Matrix3x2.CreateRotation(MathF.PI * 0.5f)
                                    * Matrix3x2.CreateTranslation(fistCenter)
                                    * cameraTransform;
                screen.SetTransform(fistTransform);
                screen.DrawTextureRect(
                    _fistTexture,
                    UIBox2.FromDimensions(-fistHalf, -fistHalf, fistSize, fistSize),
                    modulate: impactColor);
                screen.SetTransform(cameraTransform);
            }
        }
    }

    private void DrawBackground(DrawingHandleScreen screen, Vector2i targetSize, float scale, float tileSize)
    {
        if (_bgTexture == null)
            return;

        var drawW = _bgTexture.Width * scale;
        var drawH = _bgTexture.Height * scale;
        var left = (targetSize.X - drawW) * 0.5f;
        var top = (targetSize.Y - drawH) * 0.5f;
        var bottom = top + drawH;

        var extraWidth = tileSize * 2f;
        if (_edgeColumnTexture != null)
        {
            for (var x = left; x > -extraWidth; x -= tileSize)
                screen.DrawTextureRect(_edgeColumnTexture, new UIBox2(x - tileSize, top, x, bottom));

            for (var x = left + drawW; x < targetSize.X + extraWidth; x += tileSize)
                screen.DrawTextureRect(_edgeColumnTexture, new UIBox2(x, top, x + tileSize, bottom));
        }

        screen.DrawTextureRect(_bgTexture, new UIBox2(left, top, left + drawW, bottom));

        // Extend top and bottom wall fill slightly past the viewport so camera tilt never exposes gaps.
        if (_wallCapTexture != null)
        {
            var capH = tileSize * 0.5f;
            var spanLeft = MathF.Min(left, -extraWidth);
            var spanRight = MathF.Max(left + drawW, targetSize.X + extraWidth);
            for (var x = spanLeft; x < spanRight; x += tileSize)
            {
                screen.DrawTextureRect(_wallCapTexture, new UIBox2(x, top - capH, x + tileSize, top));
                screen.DrawTextureRect(_wallCapTexture, new UIBox2(x, bottom, x + tileSize, bottom + capH));
            }
        }
    }

    private Matrix3x2 ComputeCameraTransform(Vector2 center, float tileSize)
    {
        if (_intensity <= 0f)
            return Matrix3x2.Identity;

        var impactElapsed = _cycleTime - ImpactStartSeconds;
        if (impactElapsed < 0f)
            return Matrix3x2.Identity;

        var trauma = ShakeTrauma - impactElapsed * impactElapsed * ShakeDecayRate;
        if (trauma <= 0f)
            return Matrix3x2.Identity;

        var sampleX = impactElapsed * 1000f;
        var sampleY = (_punchCount % 17) * 173f;
        var noise = _shakeNoise.GetNoise(sampleX, sampleY);
        var osc = MathF.Cos(impactElapsed * 44f + (_punchCount % 2) * MathF.PI);
        var shakeWave = osc * (0.8f + 0.2f * noise);
        var angle = MathHelper.DegreesToRadians(ShakeMaxAngleDegrees * trauma * shakeWave * _intensity);

        // Subtle directional camera kick on fist impact scaled by ScreenShakeIntensity
        var recoilEnvelope = impactElapsed < 0.16f ? 1f - impactElapsed / 0.16f : 0f;
        var recoilOffset = new Vector2(-0.055f * tileSize, 0.025f * tileSize * shakeWave) * recoilEnvelope * _intensity;

        return Matrix3x2.CreateTranslation(-center)
               * Matrix3x2.CreateRotation(angle)
               * Matrix3x2.CreateTranslation(center + recoilOffset);
    }

    private static float ComputeLungeOffset(float t, float tileSize)
    {
        if (t is < 0f or > LungeDurationSeconds)
            return 0f;

        var peakTime = LungeDurationSeconds * 0.4f;
        var endReturnTime = LungeDurationSeconds * 0.7f;
        var maxOffset = 0.16f * tileSize;

        if (t <= peakTime)
            return maxOffset * (t / peakTime);

        if (t <= endReturnTime)
            return maxOffset * (1f - (t - peakTime) / (endReturnTime - peakTime));

        return 0f;
    }

    private Texture? LoadRsiFrame(string path, string stateId, RsiDirection dir = RsiDirection.South)
    {
        if (!_resources.TryGetResource<RSIResource>(path, out var res))
            return null;

        if (!res.RSI.TryGetState(stateId, out var state))
            return null;

        return state.RsiDirections == RsiDirectionType.Dir1
            ? state.Frame0
            : state.GetFrame(dir, 0);
    }

    private RsiAnimation? LoadRsiAnimation(string path, string stateId, RsiDirection dir = RsiDirection.South)
    {
        if (!_resources.TryGetResource<RSIResource>(path, out var res))
            return null;

        if (!res.RSI.TryGetState(stateId, out var state))
            return null;

        var frames = state.RsiDirections == RsiDirectionType.Dir1
            ? state.GetFrames(RsiDirection.South)
            : state.GetFrames(dir);
        return frames.Length == 0 ? null : new RsiAnimation(frames, state.Delays);
    }

    private static void DrawCenteredSprite(
        DrawingHandleScreen screen,
        Texture? texture,
        Vector2 center,
        float size,
        Color? modulate = null)
    {
        if (texture == null)
            return;

        var half = size * 0.5f;
        screen.DrawTextureRect(
            texture,
            UIBox2.FromDimensions(center.X - half, center.Y - half, size, size),
            modulate: modulate);
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

    private sealed record RsiAnimation(Texture[] Frames, float[] Delays)
    {
        public Texture GetFrameAt(float elapsedSeconds)
        {
            var t = MathF.Max(0f, elapsedSeconds);
            var accumulated = 0f;
            for (var i = 0; i < Frames.Length; i++)
            {
                var delay = i < Delays.Length ? Delays[i] : 0.1f;
                accumulated += delay;
                if (t < accumulated)
                    return Frames[i];
            }

            return Frames[^1];
        }
    }
}
