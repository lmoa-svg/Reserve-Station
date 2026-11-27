
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Mutations;

public sealed class ComponentMutationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComponentMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<ComponentMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<ComponentMutationComponent> ent, ref MutationAddedEvent args)
    {
        ent.Comp.ActuallyAdded.Clear();
        ent.Comp.ActuallyRemoved.Clear();

        if (ent.Comp.Added is {} added)
        {
            var toAdd = new ComponentRegistry();
            foreach (var (name, entry) in added)
            {
                var type = Factory.TryGetRegistration(name, out var reg) ? reg.Type : entry.Component.GetType();
                if (!HasComp(args.Target, type))
                {
                    toAdd[name] = entry;
                    ent.Comp.ActuallyAdded.Add(name);
                }
            }

            if (toAdd.Count > 0)
                EntityManager.AddComponents(args.Target, toAdd);
        }

        if (ent.Comp.Removed is {} removed)
        {
            var toRemove = new ComponentRegistry();
            foreach (var (name, entry) in removed)
            {
                var type = Factory.TryGetRegistration(name, out var reg) ? reg.Type : entry.Component.GetType();
                if (HasComp(args.Target, type))
                {
                    toRemove[name] = entry;
                    ent.Comp.ActuallyRemoved.Add(name);
                }
            }

            if (toRemove.Count > 0)
                EntityManager.RemoveComponents(args.Target, toRemove);
        }

        Dirty(ent);
    }

    private void OnRemoved(Entity<ComponentMutationComponent> ent, ref MutationRemovedEvent args)
    {
        // removed components get readded first incase that mattered
        if (ent.Comp.Removed is {} removed)
        {
            var toRestore = new ComponentRegistry();
            foreach (var name in ent.Comp.ActuallyRemoved)
            {
                if (removed.TryGetValue(name, out var entry))
                    toRestore[name] = entry;
            }

            if (toRestore.Count > 0)
                EntityManager.AddComponents(args.Target, toRestore);
        }

        if (ent.Comp.Added is {} added)
        {
            var toRemove = new ComponentRegistry();
            foreach (var name in ent.Comp.ActuallyAdded)
            {
                var neededByOther = false;
                foreach (var otherMutation in args.Target.Comp.Mutations.Values)
                {
                    if (otherMutation == ent.Owner || TerminatingOrDeleted(otherMutation))
                        continue;

                    if (TryComp<ComponentMutationComponent>(otherMutation, out var otherComp) &&
                        otherComp.Added != null &&
                        otherComp.Added.ContainsKey(name))
                    {
                        neededByOther = true;
                        break;
                    }
                }

                if (!neededByOther && added.TryGetValue(name, out var entry))
                {
                    toRemove[name] = entry;
                }
            }

            if (toRemove.Count > 0)
                EntityManager.RemoveComponents(args.Target, toRemove);
        }

        ent.Comp.ActuallyAdded.Clear();
        ent.Comp.ActuallyRemoved.Clear();
        Dirty(ent);
    }
}
