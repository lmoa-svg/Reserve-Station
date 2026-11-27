using Content.Shared.EntityConditions;
using Content.Shared._Reserve.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.EntityConditions;

/// <summary>
/// Checks the mutated mob against a nested condition.
/// If this condition's target is not a mutation entity it always returns false.
/// </summary>
public sealed partial class MutatedNestedCondition : EntityConditionBase<MutatedNestedCondition>
{
    private readonly NestedRecursionGuard _guidebookGuard = new();

    internal readonly NestedRecursionGuard _conditionGuard = new();

    public const int MaxDepth = 8;

    [DataField(required: true)]
    public EntityCondition Condition = default!;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        if (Condition == null)
            return string.Empty;

        if (!_guidebookGuard.CanEnter(MaxDepth))
            return string.Empty;
        _guidebookGuard.Enter();
        try
        {
            return Condition.EntityConditionGuidebookText(prototype) ?? string.Empty;
        }
        finally
        {
            _guidebookGuard.Exit();
        }
    }
}

public sealed partial class MutatedNestedConditionSystem :
    EntityConditionSystem<MutationComponent, MutatedNestedCondition>
{
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;
    private const int MaxDepth = 8;

    protected override void Condition(
        Entity<MutationComponent> ent,
        ref EntityConditionEvent<MutatedNestedCondition> args)
    {
        if (args.Condition.Condition == null)
            return;

        if (!args.Condition._conditionGuard.CanEnter(MaxDepth))
            return;

        args.Condition._conditionGuard.Enter();
        try
        {
            if (ent.Comp.Target is {} target)
                args.Result = _conditions.TryCondition(target, args.Condition.Condition);
        }
        finally
        {
            args.Condition._conditionGuard.Exit();
        }
    }
}
