// based on Trauma-Station: Content.Trauma.Shared/Trigger/Effects/MutationActionOnTrigger{Component,System}.cs

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Content.Shared._Reserve.Genetics.Mutations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._Reserve.Trigger;

/// <summary>
/// Mutation component to perform a <c>ActionMutationComponent</c> instant action
/// when this mutation entity gets triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MutationActionOnTriggerSystem))]
[AutoGenerateComponentState]
public sealed partial class MutationActionOnTriggerComponent : BaseXOnTriggerComponent;

public sealed partial class MutationActionOnTriggerSystem : XOnTriggerSystem<MutationActionOnTriggerComponent>
{
    [Dependency] private readonly ActionMutationSystem _actionMutation = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    protected override void OnTrigger(
        Entity<MutationActionOnTriggerComponent> ent,
        EntityUid target,
        ref TriggerEvent args)
    {
        if (_mutation.GetMutationTarget(ent.Owner) is not { } user
            || _actionMutation.GetAction(ent.Owner) is not { } action)
            return;

        if (!TryComp<ActionComponent>(action, out var actionComp) ||
            !actionComp.Enabled ||
            _actions.IsCooldownActive(actionComp))
            return;

        _actions.PerformAction(user, action);
        args.Handled = true;
    }
}