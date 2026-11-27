// based on Trauma-Station: Content.Medical.Shared/EntityEffects/Gib.cs

using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Gibbing.Components;
using Content.Shared.Gibbing.Events;
using Content.Shared.Gibbing.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Gibs the target mob or other (organs bodyparts etc).
/// Whatever it is will be deleted afterwards, be careful.
/// </summary>
public sealed partial class Gib : EntityEffectBase<Gib>
{
    [DataField]
    public bool DropGiblets = true;
}

public sealed partial class GibEffectSystem : EntityEffectSystem<TransformComponent, Gib>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Gib> args)
    {
        if (_net.IsClient)
            return;
        var contents = args.Effect.DropGiblets ? GibContentsOption.Drop : GibContentsOption.Skip;

        if (TryComp<BodyComponent>(ent.Owner, out var body))
        {
            _body.GibBody(ent.Owner, gibOrgans: true, body: body, contents: contents);
            return;
        }

        if (TryComp<BodyPartComponent>(ent.Owner, out var part) && part.Body != null)
        {
            _body.TryDetachPart(ent.Owner, part);
        }

        var gibbable = (ent.Owner, CompOrNull<GibbableComponent>(ent.Owner));
        _gibbing.TryGibEntity(ent.AsNullable(), gibbable, GibType.Gib, contents, out _);
    }
}