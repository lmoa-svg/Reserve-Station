
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Console;

public sealed partial class GeneticsConsoleSystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    private void InitializeHandheld()
    {
        SubscribeLocalEvent<HandheldGeneticsScannerComponent, MapInitEvent>(OnHandheldInit);
        SubscribeLocalEvent<HandheldGeneticsScannerComponent, AfterInteractEvent>(OnHandheldInteract);
        SubscribeLocalEvent<HandheldGeneticsScannerComponent, LinkDoAfterEvent>(OnLinkDoAfter);

        SubscribeLocalEvent<LinkedToGeneticScannerComponent, ComponentShutdown>(OnLinkedShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<LinkedToGeneticScannerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var xform = Transform(uid);
            var map = xform.MapUid;
            var pos = _transform.GetWorldPosition(xform);
            comp.Scanners.RemoveAll(scanner =>
            {
                if (TerminatingOrDeleted(scanner))
                    return true;

                var scannerXform = Transform(scanner);
                if (scannerXform.MapUid != map)
                {
                    SetScannedMob(scanner, null);
                    return true;
                }

                var scannerPos = _transform.GetWorldPosition(scannerXform);
                var dist2 = (pos - scannerPos).LengthSquared();
                if (dist2 < comp.RangeSquared) // still in range keep it
                    return false;

                SetScannedMob(scanner, null);
                return true;
            });

            if (comp.Scanners.Count == 0)
                RemCompDeferred(uid, comp);
        }
    }

    private void OnHandheldInit(Entity<HandheldGeneticsScannerComponent> ent, ref MapInitEvent args)
    {
        // it's the scanner itself
        SetScanner(ent.Owner, ent.Owner);
    }

    private void OnHandheldInteract(Entity<HandheldGeneticsScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not {} target || !_mutation.CanMutate(target))
            return;

        var user = args.User;
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.LinkTime,
            new LinkDoAfterEvent(),
            eventTarget: ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DistanceThreshold = SharedInteractionSystem.InteractionRange
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        args.Handled = true;

        var userIdentity = Identity.Entity(user, EntityManager);
        var targetIdentity = Identity.Entity(target, EntityManager);
        var you = Loc.GetString("genetics-console-linking-you", ("scanner", ent), ("user", userIdentity));
        var others = Loc.GetString("genetics-console-linking-others",
            ("scanner", ent),
            ("user", userIdentity),
            ("target", targetIdentity));

        if (user == target)
        {
            _popup.PopupPredicted(you, others, target, user);
        }
        else
        {
            _popup.PopupEntity(you, target, target);
            _popup.PopupPredicted(others, target, user);
        }
    }

    private void OnLinkDoAfter(Entity<HandheldGeneticsScannerComponent> ent, ref LinkDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not {} target)
            return;

        var user = args.User;
        if (!_interaction.InRangeUnobstructed(user, target) ||
            !_interaction.InRangeUnobstructed(ent.Owner, target))
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        if (GetScannedMob(ent.Owner) is {} oldMob)
            Unlink(oldMob, ent.Owner);

        _popup.PopupEntity(Loc.GetString("genetics-console-linked"), user, user);
        SetScannedMob(ent.Owner, target);

        var linked = EnsureComp<LinkedToGeneticScannerComponent>(target);
        if (!linked.Scanners.Contains(ent.Owner))
        {
            linked.Scanners.Add(ent.Owner);
            Dirty(target, linked);
        }

        _adminLog.Add(LogType.Genetics, LogImpact.Low, $"{user:user} linked {target:target} to {ent.Owner:console}");
        _ui.TryOpenUi(ent.Owner, GeneticsConsoleUiKey.Key, user);
    }

    private void OnLinkedShutdown(Entity<LinkedToGeneticScannerComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient)
            return;

        foreach (var scanner in ent.Comp.Scanners)
        {
            SetScannedMob(scanner, null);
        }
        ent.Comp.Scanners.Clear();
    }

    private void Unlink(Entity<LinkedToGeneticScannerComponent?> ent, EntityUid scanner)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Scanners.RemoveAll(uid => uid == scanner);
        if (ent.Comp.Scanners.Count == 0)
            RemComp(ent, ent.Comp);
        else
            Dirty(ent, ent.Comp);
    }
}

[Serializable, NetSerializable]
public sealed partial class LinkDoAfterEvent : SimpleDoAfterEvent;
