// based on Trauma-Station: Content.Trauma.Shared/Actions/ConjureActionSystem.cs

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Shared._Reserve.Actions;

public sealed partial class ConjureActionSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConjureActionComponent, ConjureActionEvent>(OnAction);
    }

    private void OnAction(Entity<ConjureActionComponent> ent, ref ConjureActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.Performer;
        var spawned = PredictedSpawnAtPosition(ent.Comp.Spawn, Transform(user).Coordinates);
        _hands.TryPickupAnyHand(user, spawned, animate: false);

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} conjured {ToPrettyString(spawned):spawned} " +
            $"using {ToPrettyString(ent):action}");
    }
}