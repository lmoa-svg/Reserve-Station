// based on Trauma-Station: Content.Medical.Shared/EntityEffects/DetachOrgan.cs

using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Detaches this target organ/part from its parent part.
/// </summary>
public sealed partial class DetachOrgan : EntityEffectBase<DetachOrgan>;

public sealed partial class DetachOrganEffectSystem : EntityEffectSystem<TransformComponent, DetachOrgan>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly WoundSystem _wounds = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<DetachOrgan> args)
    {
        if (_net.IsClient)
            return;
        if (TryComp<BodyPartComponent>(ent.Owner, out var part))
        {
            if (part.Body is { } body)
            {
                if (_body.TryGetRootPart(body, out var root) && root.Value.Owner == ent.Owner)
                    return;

                if (TryComp<WoundableComponent>(ent.Owner, out var woundable) &&
                    _body.GetParentPartAndSlotOrNull(ent.Owner) is { } parent)
                {
                    _wounds.AmputateWoundable(parent.Parent, ent.Owner, woundable);
                    return;
                }

                if (part.PartType is BodyPartType.Hand or BodyPartType.Arm)
                    _hands.TryDrop(body, ent.Owner);

                _body.DropSlotContents((ent.Owner, part));
            }

            _body.TryDetachPart(ent.Owner, part);
        }
        else if (TryComp<OrganComponent>(ent.Owner, out var organ))
        {
            _body.RemoveOrgan(ent.Owner, organ);
        }
    }
}