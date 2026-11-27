
using Content.Shared.EntityConditions;
using Content.Shared._Reserve.Genetics.Mutations;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.EntityConditions;

/// <summary>
/// Requires that the target mob has 100 or more DNA instability.
/// </summary>
public sealed partial class DnaUnstableCondition : EntityConditionBase<DnaUnstableCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-dna-unstable");
}

public sealed class DnaUnstableConditionSystem : EntityConditionSystem<MutatableComponent, DnaUnstableCondition>
{
    protected override void Condition(Entity<MutatableComponent> ent, ref EntityConditionEvent<DnaUnstableCondition> args)
    {
        args.Result = ent.Comp.TotalInstability >= ent.Comp.MaxInstability;
    }
}
