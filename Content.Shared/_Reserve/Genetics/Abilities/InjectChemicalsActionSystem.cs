using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared._Reserve.Genetics.Mutations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.Genetics.Abilities;

public sealed partial class InjectChemicalsActionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectChemicalsActionComponent, InjectChemicalsActionEvent>(OnAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Server-authoritative timer so comedown popups and bloodstream changes are neither duplicated nor missed on replay ticks.
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<InjectChemicalsActionComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextComedown is not {} next || now < next)
                continue;

            Comedown((uid, comp));
        }
    }

    private void OnAction(Entity<InjectChemicalsActionComponent> ent, ref InjectChemicalsActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.NextComedown != null)
            return;

        if (!_blocker.CanInteract(args.Performer, args.Performer))
            return;

        if (!_mobState.IsAlive(args.Performer))
            return;

        args.Handled = true;

        InjectMain(ent, args.Performer);
    }

    private void InjectMain(Entity<InjectChemicalsActionComponent> ent, EntityUid target)
    {
        // PopupClient checks IsFirstTimePredicted on the client and is a no-op on the server, avoiding duplicate popups.
        _popup.PopupClient(Loc.GetString(ent.Comp.Main.Popup), target, target);
        ent.Comp.NextComedown = _timing.CurTime + ent.Comp.ComedownDelay;
        Dirty(ent);
        Inject(target, ent.Comp.Main.Reagents, ent.Comp.Main.Quantity);
    }

    private void Comedown(Entity<InjectChemicalsActionComponent> ent)
    {
        ent.Comp.NextComedown = null;
        Dirty(ent);

        if (_mutation.GetActionMutation(ent)?.Comp?.Target is not {} target)
            return;

        _popup.PopupEntity(Loc.GetString(ent.Comp.Comedown.Popup), target, target);
        Inject(target, ent.Comp.Comedown.Reagents, ent.Comp.Comedown.Quantity);
    }

    private void Inject(EntityUid target, List<ProtoId<ReagentPrototype>> reagents, FixedPoint2 quantity)
    {
        if (quantity <= FixedPoint2.Zero)
            return;

        foreach (var reagent in reagents)
        {
            var solution = new Solution(reagent, quantity);
            _bloodstream.TryAddToBloodstream(target, solution);
        }
    }
}
