using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Polymorph;
using Content.Shared._Reserve.Genetics.Abilities;
using Content.Shared._Reserve.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Server._Reserve.Genetics.Abilities;

/// <summary>
/// Polymorphs a mob when a <see cref="PolymorphMutationComponent"/> mutation is added to it,
/// and reverts it back once the mutation is removed.
/// </summary>
public sealed partial class PolymorphMutationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<PolymorphMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<PolymorphMutationComponent> ent, ref MutationAddedEvent args)
    {
        // Mutations are moved onto the new body when polymorphing or added automatically (e.g. default mutations),
        // so do not re-trigger polymorphing.
        if (args.Automatic || ent.Comp.Worked)
            return;

        var target = args.Target.Owner;
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
            return;

        // The mutation does nothing on species it has no form for.
        if (!ent.Comp.Prototypes.TryGetValue(humanoid.Species, out var polymorph))
            return;

        // Already in the resulting form, nothing to do.
        if (IsSameForm(target, polymorph))
            return;

        if (_polymorph.PolymorphEntity(target, polymorph) is null)
            return;

        ent.Comp.Worked = true;
        Dirty(ent);
    }

    private void OnRemoved(Entity<PolymorphMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (args.Automatic)
            return;

        var target = args.Target.Owner;

        if (ent.Comp.Worked)
        {
            // Clear it before reverting, the revert moves every mutation to the original body
            // which would otherwise recurse back into this handler.
            ent.Comp.Worked = false;
            Dirty(ent);

            // Undo the polymorph and get the body the mutation was applied to back.
            if (TryComp<PolymorphedEntityComponent>(target, out var polymorphed) &&
                _polymorph.Revert((target, polymorphed)) is not null)
                return;
        }

        if (TerminatingOrDeleted(target))
            return;

        // Either the mutation started active without a prior polymorph (e.g. roundstart monkeys/kobolds)
        // or the polymorph ended on its own, so use the revert form for the current species instead.
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
            return;

        ProtoId<PolymorphPrototype>? revert = null;
        if (ent.Comp.Reverts.TryGetValue(humanoid.Species, out var proto))
            revert = proto;
        else if (ent.Comp.Fallback is { } fallback)
            revert = fallback;

        if (revert is not { } revertProto || IsSameForm(target, revertProto))
            return;

        _polymorph.PolymorphEntity(target, revertProto);
    }

    /// <summary>
    /// Returns true if the entity is already the entity prototype the polymorph would turn it into.
    /// </summary>
    private bool IsSameForm(EntityUid uid, ProtoId<PolymorphPrototype> polymorph)
    {
        if (!_proto.TryIndex(polymorph, out var proto) || proto.Configuration.Entity is not { } entity)
            return false;

        return Prototype(uid)?.ID == entity.Id;
    }
}
