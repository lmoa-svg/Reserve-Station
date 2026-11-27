using Content.Shared._Reserve.Movement;
using Content.Shared.Trigger.Systems;

namespace Content.Shared._Reserve.Trigger.Triggers;

public sealed partial class TriggerOnWalkSystem : EntitySystem
{
    [Dependency] private TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnWalkComponent, FootStepEvent>(OnFootStep);
    }

    private void OnFootStep(Entity<TriggerOnWalkComponent> ent, ref FootStepEvent args)
    {
        _trigger.Trigger(ent, args.Mob, ent.Comp.KeyOut);
    }
}
