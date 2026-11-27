using Content.Server.Body.Components;
using Content.Shared._Reserve.Genetics.Abilities;
using Content.Shared._Reserve.Genetics.Mutations;

namespace Content.Server._Reserve.Genetics.Abilities;

/// <summary>
/// Multiplies the fields of the mutated mob's <see cref="ThermalRegulatorComponent"/> while a
/// <see cref="ThermalRegulatorMutationComponent"/> mutation is active, and puts the original values back afterwards.
/// </summary>
public sealed partial class ThermalRegulatorMutationSystem : EntitySystem
{
    /// <summary>
    /// The original thermal regulator values of the mob each mutation was applied to.
    /// Stored so removing a mutation restores the base values instead of trying to undo the multiplication.
    /// </summary>
    private readonly Dictionary<EntityUid, ThermalRegulatorValues> _applied = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalRegulatorMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<ThermalRegulatorMutationComponent, MutationRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<ThermalRegulatorMutationComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAdded(Entity<ThermalRegulatorMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (_applied.ContainsKey(ent.Owner))
            return; // already applied, don't stack on itself

        if (!TryComp<ThermalRegulatorComponent>(args.Target.Owner, out var regulator))
            return; // mobs without a thermal regulator are unaffected

        _applied[ent.Owner] = new ThermalRegulatorValues(regulator);

        regulator.ShiveringHeatRegulation *= ent.Comp.Shivering;
        regulator.SweatHeatRegulation *= ent.Comp.Sweating;
        regulator.MetabolismHeat *= ent.Comp.Metabolism;
        regulator.ImplicitHeatRegulation *= ent.Comp.Regulation;
        Dirty(args.Target.Owner, regulator);
    }

    private void OnRemoved(Entity<ThermalRegulatorMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (!_applied.Remove(ent.Owner, out var values))
            return;

        Restore(args.Target.Owner, values);
    }

    private void OnShutdown(Entity<ThermalRegulatorMutationComponent> ent, ref ComponentShutdown args)
    {
        if (!_applied.Remove(ent.Owner, out var values))
            return;

        // The mutation entity was deleted without being removed (terminating, containers being cleaned, ...).
        // Restore the mob it was applied to so the dictionary doesn't leak and the mob isn't left modified.
        if (TryComp<MutationComponent>(ent.Owner, out var mutation) && mutation.Target is { } target)
            Restore(target, values);
    }

    private void Restore(EntityUid target, ThermalRegulatorValues values)
    {
        if (!TryComp<ThermalRegulatorComponent>(target, out var regulator))
            return;

        regulator.ShiveringHeatRegulation = values.Shivering;
        regulator.SweatHeatRegulation = values.Sweating;
        regulator.MetabolismHeat = values.Metabolism;
        regulator.ImplicitHeatRegulation = values.Regulation;
        Dirty(target, regulator);
    }

    /// <summary>
    /// Snapshot of every field a thermal mutation can change.
    /// </summary>
    private readonly record struct ThermalRegulatorValues(
        float Shivering,
        float Sweating,
        float Metabolism,
        float Regulation)
    {
        public ThermalRegulatorValues(ThermalRegulatorComponent regulator)
            : this(regulator.ShiveringHeatRegulation,
                regulator.SweatHeatRegulation,
                regulator.MetabolismHeat,
                regulator.ImplicitHeatRegulation)
        {
        }
    }
}
