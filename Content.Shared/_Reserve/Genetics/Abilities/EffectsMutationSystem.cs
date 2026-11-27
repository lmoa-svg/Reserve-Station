using Content.Shared.EntityEffects;
using Content.Shared._Reserve.Genetics.Mutations;
using Robust.Shared.Network;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Handles running effects for <see cref="EffectsMutationComponent"/>.
/// </summary>
public sealed partial class EffectsMutationSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnAdded(Entity<EffectsMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Automatic && ent.Comp.IgnoreAutomatic)
            return;

        _effects.ApplyEffects(args.Target, ent.Comp.Added, user: args.User);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<EffectsMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Automatic && ent.Comp.IgnoreAutomatic)
            return;

        _effects.ApplyEffects(args.Target, ent.Comp.Removed, user: args.User);
    }
}
