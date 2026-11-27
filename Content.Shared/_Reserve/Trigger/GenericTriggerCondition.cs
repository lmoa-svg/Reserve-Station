// based on Trauma-Station: Content.Trauma.Shared/Trigger/Conditions/GenericTriggerCondition{Component,System}.cs

using Content.Shared.EntityConditions;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Reserve.Trigger;

/// <summary>
/// Uses an <see cref="EntityCondition"/> as a trigger condition.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GenericTriggerConditionSystem))]
public sealed partial class GenericTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField(required: true)]
    public EntityCondition Condition = default!;

    /// <summary>
    /// If true, checks the condition against the user instead of this entity.
    /// If there is no user for a trigger attempt, triggering will be prevented.
    /// </summary>
    [DataField]
    public bool CheckUser;
}

public sealed partial class GenericTriggerConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericTriggerConditionComponent, AttemptTriggerEvent>(OnAttemptTrigger);
    }

    private void OnAttemptTrigger(Entity<GenericTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.Keys.Contains(args.Key))
            return;

        if ((ent.Comp.CheckUser ? args.User : ent.Owner) is not { } target)
        {
            args.Cancelled = true;
            return;
        }

        args.Cancelled |= !_conditions.TryCondition(target, ent.Comp.Condition);
    }
}
