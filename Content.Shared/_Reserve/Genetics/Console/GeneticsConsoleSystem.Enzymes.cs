
using Content.Shared.Database;
using Content.Shared.DoAfter;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Console;

public sealed partial class GeneticsConsoleSystem
{
    [Dependency] private UniqueEnzymesSystem _enzymes = default!;

    private void InitializeEnzymes()
    {
        SubscribeLocalEvent<GeneticsConsoleEnzymesComponent, MapInitEvent>(OnEnzymesMapInit);
        SubscribeLocalEvent<GeneticsConsoleEnzymesComponent, ApplyEnzymesDoAfterEvent>(OnApplyEnzymesDoAfter);
        SubscribeLocalEvent<GeneticsConsoleEnzymesComponent,
            DoAfterAttemptEvent<ApplyEnzymesDoAfterEvent>>(OnApplyEnzymesCheck);
        Subs.BuiEvents<GeneticsConsoleEnzymesComponent>(GeneticsConsoleUiKey.Key, subs =>
        {
            subs.Event<GeneticsConsoleSaveEnzymesMessage>(OnSaveEnzymes);
            subs.Event<GeneticsConsoleApplyEnzymesMessage>(OnApplyEnzymes);
        });
    }

    private void OnEnzymesMapInit(Entity<GeneticsConsoleEnzymesComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextApply = _timing.CurTime + ent.Comp.ApplyDelay;
        Dirty(ent);
    }

    private void OnSaveEnzymes(Entity<GeneticsConsoleEnzymesComponent> ent, ref GeneticsConsoleSaveEnzymesMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner) ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            _disk.GetDisk(ent.Owner) is not {} disk)
            return;

        var name = Name(mob);
        var enzymes = _enzymes.GetEnzymes(mob);
        if (disk.Comp.Enzymes == enzymes) // do nothing if it's the same as on disk
            return;

        _disk.SetEnzymes(disk, enzymes);

        _adminLog.Add(LogType.Genetics, LogImpact.Low,
            $"{args.Actor:user} saved {name:name}'s enzymes to {disk.Owner:disk} using {ent.Owner:console}");

        _audio.PlayPredicted(ent.Comp.SaveSound, ent, args.Actor);
    }

    private void OnApplyEnzymes(
        Entity<GeneticsConsoleEnzymesComponent> ent,
        ref GeneticsConsoleApplyEnzymesMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner) ||
            _timing.CurTime < ent.Comp.NextApply ||
            _disk.GetDisk(ent.Owner) is not {} disk ||
            disk.Comp.Enzymes is not {} enzymes ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            !_genome.IsScanned(mob)) // need to scan before applying unique enzymes
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.ApplyDuration,
            new ApplyEnzymesDoAfterEvent(GetNetEntity(mob), GetNetEntity(disk.Owner), enzymes),
            eventTarget: ent,
            target: mob,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };
        SetBusy(ent.Owner, doAfterArgs);
        Speak(ent, "applying-enzymes");
    }

    private void OnApplyEnzymesDoAfter(Entity<GeneticsConsoleEnzymesComponent> ent, ref ApplyEnzymesDoAfterEvent args)
    {
        SetBusy(ent.Owner, false);
        if (_net.IsClient)
            return;

        if (args.Cancelled)
        {
            Speak(ent, "apply-enzymes-failed");
            return;
        }

        args.Handled = true;
        var mob = GetEntity(args.Mob);
        if (!CanWorkOn(ent.Owner, mob))
        {
            Speak(ent, "apply-enzymes-failed");
            return;
        }

        var damage = _mutation.GetGeneticDamage(mob) ?? 0;
        if (damage > ent.Comp.MaxGeneticDamage)
        {
            Speak(ent, "genetic-damage");
            return;
        }

        var disk = _disk.GetDisk(ent.Owner);
        if (disk == null ||
            GetNetEntity(disk.Value.Owner) != args.Disk ||
            disk.Value.Comp.Enzymes != args.Enzymes)
        {
            Speak(ent, "apply-enzymes-failed");
            return;
        }

        var now = _timing.CurTime;
        if (now < ent.Comp.NextApply)
            return; // should be impossible

        ent.Comp.NextApply = now + ent.Comp.ApplyDelay;
        Dirty(ent);

        Speak(ent, "applied-enzymes");
        _enzymes.ChangeEnzymes(mob, args.Enzymes);

        _adminLog.Add(LogType.Genetics, LogImpact.High,
            $"{args.User:user} applied enzymes of '{args.Enzymes.Name:name}' to {mob:target} at {ent.Owner:console}");

        _audio.PlayPvs(ent.Comp.ApplySound, ent);
    }

    private void OnApplyEnzymesCheck(
        Entity<GeneticsConsoleEnzymesComponent> ent,
        ref DoAfterAttemptEvent<ApplyEnzymesDoAfterEvent> args)
    {
        var mob = GetEntity(args.Event.Mob);
        if (!CanKeepWorkingOn(ent.Owner, mob))
        {
            args.Cancel();
            return;
        }

        var disk = _disk.GetDisk(ent.Owner);
        if (disk == null ||
            GetNetEntity(disk.Value.Owner) != args.Event.Disk ||
            disk.Value.Comp.Enzymes != args.Event.Enzymes)
        {
            args.Cancel();
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class ApplyEnzymesDoAfterEvent : DoAfterEvent
{
    public NetEntity Mob;
    public NetEntity Disk;
    public UniqueEnzymes Enzymes;

    public ApplyEnzymesDoAfterEvent(NetEntity mob, NetEntity disk, UniqueEnzymes enzymes)
    {
        Mob = mob;
        Disk = disk;
        Enzymes = enzymes;
    }

    public override DoAfterEvent Clone()
        => new ApplyEnzymesDoAfterEvent(Mob, Disk, Enzymes);
}
