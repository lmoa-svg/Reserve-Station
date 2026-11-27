
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Mutations;

public sealed partial class ActionMutationSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<ActionMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<ActionMutationComponent> ent, ref MutationAddedEvent args)
    {
        // action shitcode spawns a clientside action
        if (_net.IsClient)
            return;

        _actions.AddAction(args.Target.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, container: ent.Owner);
        Dirty(ent);
    }

    private void OnRemoved(Entity<ActionMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.ActionEntity is {} action)
        {
            _actions.RemoveProvidedAction(args.Target.Owner, ent.Owner, action);
            ent.Comp.ActionEntity = null;
            Dirty(ent);
        }
    }

    public Entity<ActionComponent>? GetAction(Entity<ActionMutationComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        return _actions.GetAction(ent.Comp.ActionEntity);
    }
}
