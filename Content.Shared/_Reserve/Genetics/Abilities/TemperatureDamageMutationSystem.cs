using Content.Shared.Temperature.Components;
using Content.Shared._Reserve.Genetics.Mutations;

namespace Content.Shared._Reserve.Genetics.Abilities;

public sealed partial class TemperatureDamageMutationSystem : EntitySystem
{
    [Dependency] private readonly EntityQuery<TemperatureDamageComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureDamageMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<TemperatureDamageMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<TemperatureDamageMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        var oldCold = comp.ColdDamageThreshold;
        var oldHeat = comp.HeatDamageThreshold;

        var newCold = MathF.Max(0f, oldCold + ent.Comp.ColdOffset);
        var newHeat = MathF.Max(0f, oldHeat + ent.Comp.HeatOffset);

        if (newCold >= newHeat)
        {
            if (ent.Comp.ColdOffset > 0 && ent.Comp.HeatOffset <= 0)
                newCold = MathF.Max(0f, newHeat - 1f);
            else if (ent.Comp.HeatOffset < 0 && ent.Comp.ColdOffset >= 0)
                newHeat = newCold + 1f;
            else
            {
                var mid = (newCold + newHeat) / 2f;
                newCold = MathF.Max(0f, mid - 0.5f);
                newHeat = newCold + 1f;
            }
        }

        var appliedCold = newCold - oldCold;
        var appliedHeat = newHeat - oldHeat;

        comp.ColdDamageThreshold = newCold;
        comp.HeatDamageThreshold = newHeat;
        Dirty(args.Target, comp);

        ent.Comp.AppliedColdOffset = appliedCold;
        ent.Comp.AppliedHeatOffset = appliedHeat;
        Dirty(ent);
    }

    private void OnRemoved(Entity<TemperatureDamageMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        comp.ColdDamageThreshold -= ent.Comp.AppliedColdOffset;
        comp.HeatDamageThreshold -= ent.Comp.AppliedHeatOffset;

        if (comp.ColdDamageThreshold >= comp.HeatDamageThreshold)
        {
            comp.ColdDamageThreshold = MathF.Max(0f, comp.HeatDamageThreshold - 1f);
        }

        Dirty(args.Target, comp);

        ent.Comp.AppliedColdOffset = 0f;
        ent.Comp.AppliedHeatOffset = 0f;
        Dirty(ent);
    }
}
