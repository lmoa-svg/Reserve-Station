using Content.Shared.EntityEffects;
using Content.Shared.Mind.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Deletes the target entity, be careful with this...
/// </summary>
public sealed partial class Delete : EntityEffectBase<Delete>
{
    [DataField]
    public bool Queued;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-delete-entity", ("chance", Probability));
}

public sealed class DeleteEffectSystem : EntityEffectSystem<MetaDataComponent, Delete>
{
    [Dependency] private readonly INetManager _net = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<Delete> args)
    {
        if (_net.IsClient)
            return;

        if (TerminatingOrDeleted(ent, ent.Comp))
            return;

        if (HasComp<ActorComponent>(ent) || (TryComp<MindContainerComponent>(ent, out var mind) && mind.HasMind))
        {
            Log.Warning($"Attempted to delete player entity {ToPrettyString(ent.Owner)} " +
                "via Delete entity effect. Aborting.");
            return;
        }

        QueueDel(ent.Owner);
    }
}
