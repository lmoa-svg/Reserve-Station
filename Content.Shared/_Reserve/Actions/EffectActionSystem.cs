// based on Trauma-Station: Content.Trauma.Shared/Actions/EffectActionSystem.cs

using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;

namespace Content.Shared._Reserve.Actions;

public sealed partial class EffectActionSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectActionComponent, ActionPerformedEvent>(OnActionPerformed);
        SubscribeLocalEvent<EffectActionComponent, EffectInstantActionEvent>(OnInstantAction);
        SubscribeLocalEvent<EffectActionComponent, EffectTargetActionEvent>(OnTargetAction);
    }

    private void OnActionPerformed(Entity<EffectActionComponent> ent, ref ActionPerformedEvent args)
    {
        if (!ent.Comp.OnPerformed)
            return;

        // EffectInstantActionEvent already applies effects to the performer during OnInstantAction
        if (TryComp<InstantActionComponent>(ent, out var instant) && instant.Event is EffectInstantActionEvent)
            return;

        var user = args.Performer;
        _effects.ApplyEffects(user, ent.Comp.Effects, user: user);
    }

    private void OnInstantAction(Entity<EffectActionComponent> ent, ref EffectInstantActionEvent args)
    {
        var user = args.Performer;
        _effects.ApplyEffects(user, ent.Comp.Effects, user: user);
        args.Handled = true;
    }

    private void OnTargetAction(Entity<EffectActionComponent> ent, ref EffectTargetActionEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        var user = args.Performer;
        _effects.ApplyEffects(args.Target, ent.Comp.Effects, user: user);
        args.Handled = true;
    }
}