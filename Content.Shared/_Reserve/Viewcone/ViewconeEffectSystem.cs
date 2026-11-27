using Content.Shared._Reserve.Movement;
using Content.Shared._Reserve.Viewcone.Components;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.Viewcone;

/// <summary>
/// Handles footsteps, melee attacks, and speech creating out-of-vision effects.
/// Provides API for spawning viewcone effects and making sure source gets set correctly.
/// </summary>
public sealed partial class ViewconeEffectSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    public static readonly EntProtoId TalkEffect = "ViewconeEffectTalk";

    private bool _disabled;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ViewconeFootstepsEffectComponent, FootStepEvent>(OnFootStep);
        SubscribeLocalEvent<ViewconeMeleeEffectComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<EntitySpokeEvent>(OnSpoke);

        Subs.CVar(_cfg, CCVars.DisableVisionEffects, x => _disabled = x, true);
    }

    private void OnFootStep(Entity<ViewconeFootstepsEffectComponent> ent, ref FootStepEvent args)
    {
        // Silent shoes suppress the viewcone footstep effect
        var ev = new CanSpawnFootstepsEvent();
        RaiseLocalEvent(ent.Owner, ref ev);
        if (ev.Cancelled)
            return;

        SpawnEffect(ent, ent.Comp.Effect, args.WorldAngle);
    }

    private void OnMeleeAttack(Entity<ViewconeMeleeEffectComponent> ent, ref MeleeAttackEvent args)
    {
        SpawnEffect(ent, ent.Comp.Effect);
    }

    private void OnSpoke(EntitySpokeEvent args)
    {
        // whispering is too quiet to get a fix on
        if (!args.IsWhisper)
            SpawnEffect(args.Source, TalkEffect);
    }

    /// <summary>
    /// Spawns the given effect entity at the player source, and sets relevant variables.
    /// </summary>
    /// <param name="source">The entity that originated the effect.</param>
    /// <param name="effect">The prototype ID of an effect entity to spawn (see viewcone_effects.yml).</param>
    /// <param name="angleOverride">The local rotation to set the effect to, instead of the parent rotation.</param>
    public void SpawnEffect(EntityUid source, [ForbidLiteral] EntProtoId effect, Angle? angleOverride = null)
    {
        if (_disabled || !_timing.IsFirstTimePredicted)
            return;

        var ent = PredictedSpawnNextToOrDrop(effect, source);
        var viewconeEffect = EnsureComp<ViewconeOccludableComponent>(ent);
        viewconeEffect.Inverted = true; // it's always visible
        viewconeEffect.Source = source;
        Dirty(ent, viewconeEffect);

        // set rotation
        _xform.SetLocalRotation(ent, angleOverride ?? Transform(source).LocalRotation);

        // also ensure this in case somehow something without it gets here.
        EnsureComp<TimedDespawnComponent>(ent);
    }
}
