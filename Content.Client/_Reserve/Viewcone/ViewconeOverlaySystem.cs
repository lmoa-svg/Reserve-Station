using System.Numerics;
using Content.Client._Reserve.Viewcone.Overlays;
using Content.Client.Eye;
using Content.Shared._Reserve.Viewcone;
using Content.Shared._Reserve.Viewcone.Components;
using Content.Shared.CCVar;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Pulling.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client._Reserve.Viewcone;

/// <summary>
/// Handles adding and removing the viewcone overlays, as well as ferrying data between them.
/// Also handles calculating desired view angle for active viewcones so overlays can use it.
/// </summary>
public sealed partial class ViewconeOverlaySystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private ViewconeAngleSystem _angle = default!;
    [Dependency] private SpriteVisibilitySystem _spriteVis = default!;
    [Dependency] private EntityQuery<EyeComponent> _eyeQuery = default!;
    [Dependency] private EntityQuery<MouseRotatorComponent> _rotatorQuery = default!;

    private ViewconeConeOverlay _coneOverlay = default!;
    private ViewconeSetAlphaOverlay _setAlphaOverlay = default!;

    private const float LerpHalfLife = 0.065f;

    // raw grain scale ignoring reduced motion setting
    // reduced motion locks it to 0
    private float _grainScale;
    private bool _reducedMotion;
    private bool _active;
    private bool _disabled;

    public override void Initialize()
    {
        base.Initialize();

        _coneOverlay = new();
        _setAlphaOverlay = new();

        SubscribeLocalEvent<PullableComponent, ViewconeOverrideEvent>(OnPullableOverride);
        SubscribeLocalEvent<ViewconeTailedSegmentComponent, ViewconeOverrideEvent>(OnTailedOverride);
        SubscribeLocalEvent<ViewconeComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ViewconeComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<ViewconeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ViewconeComponent, ShowPopupAttemptEvent>(OnShowPopupAttempt);
        SubscribeLocalEvent<ViewconeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ViewconeOccludableComponent, ComponentInit>(OnOccludableInit);
        SubscribeLocalEvent<ViewconeOccludableComponent, ComponentShutdown>(OnOccludableShutdown);
        SubscribeLocalEvent<ViewconeOccludableComponent, EntParentChangedMessage>(OnOccludableParentChanged);

        Subs.CVar(_cfg, CCVars.VisionGrainScale, SetGrainScale, true);
        Subs.CVar(_cfg, CCVars.DisableVisionCones, SetConesDisabled, true);
        Subs.CVar(_cfg, CCVars.ReducedMotion, SetReducedMotion, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        RemoveOverlays();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var enumerator = AllEntityQuery<LerpingEyeComponent, EyeComponent, ViewconeComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var eye, out var viewcone, out var xform))
        {
            // cached for overlays and popups to use
            viewcone.CurrentConeAngle = _angle.GetAngle((uid, viewcone));

            var eyeAngle = eye.Rotation;
            var (position, rotation) = _xform.GetWorldPositionRotation(xform);
            var playerAngle = rotation;
            var desiredWasNull = viewcone.DesiredViewAngle == null;

            if (_rotatorQuery.HasComp(uid))
            {
                var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
                if (mousePos.MapId != MapId.Nullspace)
                    playerAngle = (mousePos.Position - _xform.GetMapCoordinates(xform).Position).ToAngle() + Angle.FromDegrees(90);

                viewcone.LastMouseRotationAngle = playerAngle;
            }
            else if (viewcone.LastMouseRotationAngle != 0f)
            {
                // if last frame we had a mouse rotation angle, but now we dont,
                // keep the old mouse angle until the real angle changes or they move
                if (MathHelper.CloseToPercent(viewcone.LastWorldRotationAngle, playerAngle, .001d)
                    && viewcone.LastWorldPos == position)
                {
                    playerAngle = viewcone.LastMouseRotationAngle;
                }
                else
                {
                    viewcone.LastMouseRotationAngle = 0f;
                }
            }

            viewcone.LastWorldPos = position;
            viewcone.LastWorldRotationAngle = rotation;
            viewcone.DesiredViewAngle = playerAngle + eyeAngle;

            if (desiredWasNull)
            {
                viewcone.ViewAngle = viewcone.DesiredViewAngle.Value;
                continue;
            }

            // framerate-independent lerp
            viewcone.ViewAngle = Angle.Lerp(viewcone.ViewAngle, viewcone.DesiredViewAngle.Value, 1f - MathF.Pow(2f, -(frameTime / LerpHalfLife)));
        }
    }

    private void SetGrainScale(float scale)
    {
        _grainScale = scale;
        if (!_reducedMotion)
            _coneOverlay.GrainScale = scale;
    }

    private void SetConesDisabled(bool disabled)
    {
        _disabled = disabled;
        if (!_active)
            return;

        if (_disabled)
            RemoveOverlays(setActive: false); // remove unless and until cvar is reenabled
        else
            AddOverlays(); // add them back
    }

    private void SetReducedMotion(bool on)
    {
        _reducedMotion = on;
        _coneOverlay.GrainScale = on
            ? 0f
            : _grainScale;
    }

    /// <summary>
    /// Returns true if a point is inside the vision cone, using world positions.
    /// </summary>
    public bool IsVisible(Entity<ViewconeComponent> ent, Vector2 eyePos, Vector2 pos, EyeComponent? eye = null)
    {
        var dist = pos - eyePos;
        var r = ent.Comp.ConeIgnoreRadius;
        var r2 = r * r;
        if (dist.LengthSquared() < r2)
            return true; // within cone ignore radius so always visible regardless of angle

        var eyeRot = ent.Comp.ViewAngle;
        if (_eyeQuery.Resolve(ent, ref eye, false))
            eyeRot -= eye.Rotation;

        var angleDist = Math.Abs(Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot).Theta);
        return angleDist < MathHelper.DegreesToRadians(ent.Comp.CurrentConeAngle) * 0.5f;
    }

    private void OnPullableOverride(Entity<PullableComponent> ent, ref ViewconeOverrideEvent args)
    {
        if (ent.Comp.Puller != _player.LocalEntity)
            return;

        args.Override = true;
    }

    private void OnTailedOverride(Entity<ViewconeTailedSegmentComponent> ent, ref ViewconeOverrideEvent args)
    {
        if (ent.Comp.Head != _player.LocalEntity)
            return;

        args.Override = true;
    }

    private void OnPlayerAttached(Entity<ViewconeComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlays();
    }

    private void OnPlayerDetached(Entity<ViewconeComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlays();
    }

    private void OnInit(Entity<ViewconeComponent> ent, ref ComponentInit args)
    {
        if (ent.Owner == _player.LocalEntity)
            AddOverlays();
    }

    private void OnShowPopupAttempt(Entity<ViewconeComponent> ent, ref ShowPopupAttemptEvent args)
    {
        args.Cancelled |= !IsVisible(ent, args.ViewerPos, args.WorldPos);
    }

    private void OnShutdown(Entity<ViewconeComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            RemoveOverlays();
    }

    private void AddOverlays()
    {
        if (_disabled)
            return;

        _active = true;

        _overlay.AddOverlay(_coneOverlay);
        _overlay.AddOverlay(_setAlphaOverlay);
    }

    private void RemoveOverlays(bool setActive = true)
    {
        if (setActive) // keep its value if cvar is changed live
            _active = false;

        _overlay.RemoveOverlay(_coneOverlay);
        _overlay.RemoveOverlay(_setAlphaOverlay);

        // hide memories and reset everythings opacity
        var query = EntityQueryEnumerator<ViewconeOccludableComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Memory is { } memory && !TerminatingOrDeleted(memory))
                SetAlpha(memory, 0f);

            SetAlpha(uid, 1f);
            RemCompDeferred<ViewconeOccludedComponent>(uid);
        }
    }

    private void OnOccludableInit(Entity<ViewconeOccludableComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Inverted)
            SetAlpha(ent, 0f); // wait for overlay to maybe show effects next frame
    }

    private void OnOccludableShutdown(Entity<ViewconeOccludableComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Memory is { } memory && !TerminatingOrDeleted(memory))
            Del(memory);
    }

    private void OnOccludableParentChanged(Entity<ViewconeOccludableComponent> ent, ref EntParentChangedMessage args)
    {
        if (ent.Comp.Memory is not { } memory ||
            args.OldMapId == args.Transform.MapUid)
            return;

        // if the map changes for any reason, hide the memory
        // this may happen from leaving PVS or FTLing, etc
        SetAlpha(memory, 0f);
    }

    public void SetAlpha(EntityUid uid, float alpha)
    {
        _spriteVis.UpdateVisibilityModifiers(uid, nameof(ViewconeOccludedComponent), alpha);
    }

    public bool IgnoresViewcone(EntityUid uid)
    {
        var ev = new ViewconeOverrideEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Override;
    }
}
