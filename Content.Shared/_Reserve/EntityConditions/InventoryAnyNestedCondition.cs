using Content.Shared.EntityConditions;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.EntityConditions;

/// <summary>
/// Condition that succeeds if any equipped inventory item passes a nested condition.
/// </summary>
public sealed partial class InventoryAnyNestedCondition : EntityConditionBase<InventoryAnyNestedCondition>
{
    private readonly NestedRecursionGuard _guidebookGuard = new();

    internal readonly NestedRecursionGuard _conditionGuard = new();

    public const int MaxDepth = 8;

    /// <summary>
    /// The nested condition to check inventory items against.
    /// </summary>
    [DataField(required: true)]
    public EntityCondition Condition = default!;

    [DataField]
    public LocId GuidebookText = "entity-condition-guidebook-inventory-nested";

    [DataField]
    public SlotFlags Flags = SlotFlags.WITHOUT_POCKET;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        if (Condition == null)
            return string.Empty;

        if (!_guidebookGuard.CanEnter(MaxDepth))
            return string.Empty;
        _guidebookGuard.Enter();
        try
        {
            var nestedText = Condition.EntityConditionGuidebookText(prototype) ?? string.Empty;
            return Loc.GetString(GuidebookText, ("condition", nestedText));
        }
        finally
        {
            _guidebookGuard.Exit();
        }
    }
}

public sealed partial class InventoryAnyNestedConditionSystem :
    EntityConditionSystem<InventoryComponent, InventoryAnyNestedCondition>
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;


    private const int MaxDepth = 8;

    protected override void Condition(
        Entity<InventoryComponent> ent,
        ref EntityConditionEvent<InventoryAnyNestedCondition> args)
    {
        if (args.Condition.Condition == null)
            return;

        if (!args.Condition._conditionGuard.CanEnter(MaxDepth))
            return;

        args.Condition._conditionGuard.Enter();
        try
        {
            var flags = args.Condition.Flags;
            if (!_inventory.TryGetContainerSlotEnumerator(ent.AsNullable(), out var slots, flags))
                return;

            var condition = args.Condition.Condition;
            while (slots.NextItem(out var item))
            {
                if (_conditions.TryCondition(item, condition))
                {
                    args.Result = true;
                    return;
                }
            }
        }
        finally
        {
            args.Condition._conditionGuard.Exit();
        }
    }
}
