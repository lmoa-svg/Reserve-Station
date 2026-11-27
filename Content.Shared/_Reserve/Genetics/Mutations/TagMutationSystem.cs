
using Content.Shared.Tag;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Mutations;

public sealed partial class TagMutationSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TagMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<TagMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<TagMutationComponent> ent, ref MutationAddedEvent args)
    {
        var target = args.Target.Owner;
        ent.Comp.ActuallyAdded.Clear();
        ent.Comp.ActuallyRemoved.Clear();

        foreach (var tag in ent.Comp.Added)
        {
            if (!_tag.HasTag(target, tag))
            {
                if (_tag.AddTag(target, tag))
                    ent.Comp.ActuallyAdded.Add(tag);
            }
        }

        foreach (var tag in ent.Comp.Removed)
        {
            if (_tag.HasTag(target, tag))
            {
                if (_tag.RemoveTag(target, tag))
                    ent.Comp.ActuallyRemoved.Add(tag);
            }
        }

        Dirty(ent);
    }

    private void OnRemoved(Entity<TagMutationComponent> ent, ref MutationRemovedEvent args)
    {
        var target = args.Target.Owner;

        foreach (var tag in ent.Comp.ActuallyRemoved)
        {
            _tag.AddTag(target, tag);
        }

        foreach (var tag in ent.Comp.ActuallyAdded)
        {
            var neededByOther = false;
            foreach (var otherMutation in args.Target.Comp.Mutations.Values)
            {
                if (otherMutation == ent.Owner || TerminatingOrDeleted(otherMutation))
                    continue;

                if (TryComp<TagMutationComponent>(otherMutation, out var otherComp) &&
                    otherComp.Added.Contains(tag))
                {
                    neededByOther = true;
                    break;
                }
            }

            if (!neededByOther)
                _tag.RemoveTag(target, tag);
        }

        ent.Comp.ActuallyAdded.Clear();
        ent.Comp.ActuallyRemoved.Clear();
        Dirty(ent);
    }
}
