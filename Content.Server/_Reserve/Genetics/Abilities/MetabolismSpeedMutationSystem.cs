using Content.Server.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared._Reserve.Genetics.Abilities;
using Content.Shared._Reserve.Genetics.Mutations;

namespace Content.Server._Reserve.Genetics.Abilities;

public sealed partial class MetabolismSpeedMutationSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly EntityQuery<MetabolizerComponent> _query = default!;

    private readonly Dictionary<EntityUid, Dictionary<EntityUid, float>> _appliedModifiers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetabolismSpeedMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<MetabolismSpeedMutationComponent, MutationRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<MetabolismSpeedMutationComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<MetabolismSpeedMutationComponent> ent, ref ComponentShutdown args)
    {
        RestoreModifiers(ent.Owner);
    }

    private void RestoreModifiers(EntityUid owner)
    {
        if (_appliedModifiers.Remove(owner, out var applied))
        {
            foreach (var (targetUid, delta) in applied)
            {
                if (!_query.TryComp(targetUid, out var comp))
                    continue;

                comp.UpdateIntervalMultiplier = MathF.Max(0.1f, comp.UpdateIntervalMultiplier - delta);
                Dirty(targetUid, comp);
            }
        }
    }

    private void OnAdded(Entity<MetabolismSpeedMutationComponent> ent, ref MutationAddedEvent args)
    {
        var uid = args.Target.Owner;
        var applied = new Dictionary<EntityUid, float>();
        _appliedModifiers[ent.Owner] = applied;

        // Speeding up metabolism means decreasing the update interval.
        // Some mobs like dragon have metabolizer on the mob itself, not organs.
        if (_query.TryComp(uid, out var mobComp))
        {
            ApplyBonus(ent, applied, uid, mobComp);
        }

        foreach (var organ in _body.GetBodyOrganEntityComps<MetabolizerComponent>(uid))
        {
            ApplyBonus(ent, applied, organ.Owner, organ.Comp1);
        }
    }

    private void ApplyBonus(
        Entity<MetabolismSpeedMutationComponent> ent,
        Dictionary<EntityUid, float> applied,
        EntityUid targetUid,
        MetabolizerComponent comp)
    {
        var oldMultiplier = comp.UpdateIntervalMultiplier;
        var newMultiplier = MathF.Max(0.1f, oldMultiplier - ent.Comp.Bonus);
        var delta = newMultiplier - oldMultiplier;

        comp.UpdateIntervalMultiplier = newMultiplier;
        Dirty(targetUid, comp);

        applied[targetUid] = delta;
    }

    private void OnRemoved(Entity<MetabolismSpeedMutationComponent> ent, ref MutationRemovedEvent args)
    {
        RestoreModifiers(ent.Owner);
    }
}
