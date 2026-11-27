using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared._Reserve.Genetics.Mutations;

namespace Content.Shared._Reserve.Genetics.Abilities;

public sealed partial class MobThresholdMutationSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _threshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobThresholdMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<MobThresholdMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<MobThresholdMutationComponent> ent, ref MutationAddedEvent args)
    {
        var target = args.Target.Owner;
        if (!TryComp<MobStateComponent>(target, out var mob))
            return;

        var state = ent.Comp.Removed;
        if (!mob.AllowedStates.Contains(state))
            return;

        if (!TryComp<MobThresholdsComponent>(target, out var thresholds))
            return;

        if (!_threshold.TryGetThresholdForState(target, state, out var threshold, thresholds))
            return;

        mob.AllowedStates.Remove(state);
        Dirty(target, mob);

        thresholds.Thresholds.Remove(threshold.Value);
        Dirty(target, thresholds);
        _threshold.VerifyThresholds(target, thresholds);

        ent.Comp.OldThreshold = threshold;
        Dirty(ent);
    }

    private void OnRemoved(Entity<MobThresholdMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (ent.Comp.OldThreshold is not {} threshold)
            return;

        ent.Comp.OldThreshold = null;
        Dirty(ent);

        var target = args.Target.Owner;
        if (!TryComp<MobStateComponent>(target, out var mob))
            return;

        var state = ent.Comp.Removed;
        mob.AllowedStates.Add(state);
        Dirty(target, mob);

        if (!TryComp<MobThresholdsComponent>(target, out var thresholds))
            return;

        var dict = thresholds.Thresholds;
        if (!dict.TryGetValue(threshold, out var existingState) || existingState == state)
        {
            dict[threshold] = state;
            Dirty(target, thresholds);
            _threshold.VerifyThresholds(target, thresholds);
        }
    }
}
