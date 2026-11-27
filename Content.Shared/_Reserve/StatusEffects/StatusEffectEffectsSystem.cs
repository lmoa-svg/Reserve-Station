// based on Trauma-Station: Content.Trauma.Shared/StatusEffects/StatusEffectEffectsSystem.cs

using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.StatusEffects;

public sealed partial class StatusEffectEffectsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectEffectsComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<StatusEffectEffectsComponent, StatusEffectComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp, out var status))
        {
            if (status.AppliedTo is not { } target || now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;
            Dirty(uid, comp);

            _effects.ApplyEffects(target, comp.Effects, user: target);
        }
    }

    private void OnMapInit(Entity<StatusEffectEffectsComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateDelay;
        Dirty(ent);
    }
}