// based on Trauma-Station: Content.Trauma.Shared/StatusEffects/StatusEffectEffectsApplySystem.cs

using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.StatusEffects;

public sealed partial class StatusEffectEffectsApplySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectEffectsApplyComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<StatusEffectEffectsApplyComponent, StatusEffectRemovedEvent>(OnRemoval);
    }

    private void OnApplied(Entity<StatusEffectEffectsApplyComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.EffectsOnApply is not { } effectsOnApply ||
            _timing.ApplyingState)
            return;

        _effects.ApplyEffects(args.Target, effectsOnApply);
    }

    private void OnRemoval(Entity<StatusEffectEffectsApplyComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.EffectsOnRemoval is not { } effectsOnRemoval ||
            _timing.ApplyingState ||
            TerminatingOrDeleted(args.Target))
            return;

        _effects.ApplyEffects(args.Target, effectsOnRemoval);
    }
}