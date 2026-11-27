using Content.Shared.StatusEffectNew;
using Content.Shared._Reserve.Genetics.Mutations;

namespace Content.Shared._Reserve.Genetics.Abilities;

public sealed partial class StatusEffectsMutationSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectsMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<StatusEffectsMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<StatusEffectsMutationComponent> ent, ref MutationAddedEvent args)
    {
        var target = args.Target.Owner;
        foreach (var effect in ent.Comp.StatusEffects)
        {
            if (_status.HasStatusEffect(target, effect))
                continue;

            if (_status.TryAddStatusEffect(target, effect, out _))
                ent.Comp.AppliedStatusEffects.Add(effect);
        }

        Dirty(ent);
    }

    private void OnRemoved(Entity<StatusEffectsMutationComponent> ent, ref MutationRemovedEvent args)
    {
        var target = args.Target;

        foreach (var effect in ent.Comp.AppliedStatusEffects)
        {
            var requiredByOther = false;
            foreach (var otherMutationUid in target.Comp.Mutations.Values)
            {
                if (otherMutationUid == ent.Owner)
                    continue;

                if (TryComp<StatusEffectsMutationComponent>(otherMutationUid, out var otherComp) &&
                    otherComp.StatusEffects.Contains(effect))
                {
                    requiredByOther = true;
                    break;
                }
            }

            if (!requiredByOther)
                _status.TryRemoveStatusEffect(target.Owner, effect);
        }

        ent.Comp.AppliedStatusEffects.Clear();
        Dirty(ent);
    }
}
