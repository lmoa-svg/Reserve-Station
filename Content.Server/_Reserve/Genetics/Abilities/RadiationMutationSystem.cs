using Content.Server.Radiation.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radiation.Components;
using Content.Shared._Reserve.Genetics.Abilities;
using Content.Shared._Reserve.Genetics.Mutations;

namespace Content.Server._Reserve.Genetics.Abilities;

/// <summary>
/// Turns the radiation source of a <see cref="RadiationMutationComponent"/> mutation off while its host is dead.
/// The source lives on the mutation entity itself, so relaying mob state changes is enough to toggle it.
/// </summary>
public sealed partial class RadiationMutationSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly RadiationSystem _radiation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadiationMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<RadiationMutationComponent, MutationRemovedEvent>(OnRemoved);

        // Relayed to the mutation entity by MutationRelaySystem.
        SubscribeLocalEvent<RadiationMutationComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnAdded(Entity<RadiationMutationComponent> ent, ref MutationAddedEvent args)
    {
        SetEnabled(ent, !_mob.IsDead(args.Target.Owner));
    }

    private void OnRemoved(Entity<RadiationMutationComponent> ent, ref MutationRemovedEvent args)
    {
        // Mutations can be moved to another body instead of being deleted, don't keep it disabled for the next one.
        SetEnabled(ent, true);
    }

    private void OnMobStateChanged(Entity<RadiationMutationComponent> ent, ref MobStateChangedEvent args)
    {
        SetEnabled(ent, !_mob.IsDead(args.Target));
    }

    private void SetEnabled(Entity<RadiationMutationComponent> ent, bool enabled)
    {
        if (!TryComp<RadiationSourceComponent>(ent.Owner, out var source) || source.Enabled == enabled)
            return;

        _radiation.SetSourceEnabled((ent.Owner, source), enabled);
    }
}
