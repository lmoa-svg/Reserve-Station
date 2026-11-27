
using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Standing;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Makes the target drop the items they are holding.
/// </summary>
public sealed partial class DropItems : EntityEffectBase<DropItems>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-drop-items", ("chance", Probability));
}

public sealed class DropItemsEffectSystem : EntityEffectSystem<HandsComponent, DropItems>
{
    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<DropItems> args)
    {
        var ev = new DropHandItemsEvent();
        RaiseLocalEvent(ent, ref ev);
    }
}
