
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared._Reserve.Genetics.Mutations;
using Content.Shared._Reserve.Genetics.Tools;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Console;

public sealed partial class GeneticsConsoleSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private GeneticsDiskSystem _disk = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private MutationSystem _mutation = default!;
    [Dependency] private MutatorSystem _mutator = default!;
    [Dependency] private ScannedGenomeSystem _genome = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedMaterialStorageSystem _material = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityQuery<MaterialStorageComponent> _materialQuery = default!;

    private CompName _costName;

    public override void Initialize()
    {
        base.Initialize();

        _costName = Factory.CompName<MutationCostComponent>();

        Subs.BuiEvents<GeneticsConsoleComponent>(GeneticsConsoleUiKey.Key, subs =>
        {
            subs.Event<GeneticsConsoleScrambleMessage>(OnScramble);
            subs.Event<GeneticsConsoleSetBaseMessage>(OnSetBase);
            subs.Event<GeneticsConsoleSequenceMessage>(OnSequence);
            subs.Event<GeneticsConsoleResetSequenceMessage>(OnResetSequence);
            subs.Event<GeneticsConsoleWriteMutationMessage>(OnWriteMutation);
            subs.Event<GeneticsConsoleCombineMessage>(OnCombine);
            subs.Event<GeneticsConsolePrintMessage>(OnPrint);
        });

        InitializeEnzymes();
        InitializeHandheld();
        InitializePrintout();
        InitializeScanner();
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GeneticsConsoleComponent> ent, ref MapInitEvent args)
    {
        // prevent reconstruct cheesing
        ent.Comp.NextScramble = _timing.CurTime + ent.Comp.ScrambleCooldown;
        DirtyField(ent.AsNullable(), nameof(GeneticsConsoleComponent.NextScramble));

        ent.Comp.NextPrint = _timing.CurTime + ent.Comp.PrintDelay;
        DirtyField(ent.AsNullable(), nameof(GeneticsConsoleComponent.NextPrint));

        _material.UpdateMaterialWhitelist(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnGetMaterialWhitelist(Entity<GeneticsConsoleComponent> ent, ref GetMaterialWhitelistEvent args)
    {
        args.Whitelist.Add(ent.Comp.Biomass);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<GeneticsConsoleComponent> ent, ref ExaminedEvent args)
    {
        if (!_materialQuery.TryComp(ent, out var storage))
            return;

        var biomass = _material.GetMaterialAmount(ent.Owner, ent.Comp.Biomass, storage);
        args.PushMarkup(Loc.GetString("genetics-console-examined", ("biomass", biomass)));
    }

    private void OnScramble(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleScrambleMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner) ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            !_genome.IsScanned(mob) || // can't scramble unscanned mobs
            _mutation.GetMutatable(mob) is not {} mutatable)
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextScramble)
            return;

        _adminLog.Add(LogType.Genetics, LogImpact.High,
            $"Scrambled genome of {mob:target} by {args.Actor:user} using console {ent.Owner:console}");

        _damage.ChangeDamage(mob, ent.Comp.ScrambleDamage);

        ent.Comp.NextScramble = now + ent.Comp.ScrambleCooldown;
        DirtyField(ent.AsNullable(), nameof(GeneticsConsoleComponent.NextScramble));

        // reset dormant but unactivated mutations and reroll them
        _mutation.ClearUnusedDormant(mutatable);
        _mutation.Scramble(mutatable);
        UpdateUI(ent.Owner);
    }

    private void OnSetBase(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleSetBaseMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner) ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            _genome.GetSequence(mob, args.Sequence) is not {} sequence ||
            args.Index >= sequence.Bases.Length ||
            args.Index >= sequence.OriginalBases.Length ||
            sequence.OriginalBases[(int) args.Index] != 'X' ||
            _mutation.GetRoundData(sequence.Mutation) is not {} data ||
            data.Discovered)
            return;

        // chud language can't just set a char directly
        var builder = new StringBuilder(sequence.Bases);
        var i = (int) args.Index;
        builder[i] = CycleBase(sequence.Bases[i], args.Cycle);
        sequence.Bases = builder.ToString();
        UpdateUI(ent.Owner);
    }

    private void OnSequence(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleSequenceMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner) ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            _genome.GetSequence(mob, args.Index) is not {} sequence ||
            _mutation.GetRoundData(sequence.Mutation)?.Discovered == true)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.SequenceDelay,
            new SequenceDoAfterEvent(GetNetEntity(mob), args.Index),
            eventTarget: ent,
            target: mob,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };
        SetBusy(ent.Owner, doAfterArgs);
        Speak(ent, "sequencing");
    }

    private void OnResetSequence(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleResetSequenceMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner) ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            _genome.GetSequence(mob, args.Index) is not {} sequence ||
            sequence.Bases == sequence.OriginalBases) // was already reset
            return;

        // incase some shitter runs in and erases all your progress on your monkey idk
        _adminLog.Add(LogType.Genetics, LogImpact.Low,
            $"{mob:target} sequence {args.Index} was reset by {args.Actor:user} using console {ent.Owner:console}");

        sequence.Bases = sequence.OriginalBases;
        UpdateUI(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnSequenceDoAfter(Entity<GeneticsConsoleComponent> ent, ref SequenceDoAfterEvent args)
    {
        SetBusy(ent.Owner, false);
        if (args.Cancelled)
        {
            Speak(ent, "sequence-failed");
            return;
        }

        args.Handled = true;
        var mob = GetEntity(args.Mob);
        if (!CanWorkOn(ent.Owner, mob))
        {
            Speak(ent, "sequence-failed");
            return;
        }

        var damage = _mutation.GetGeneticDamage(mob) ?? 0;
        if (damage > ent.Comp.MaxGeneticDamage)
        {
            Speak(ent, "genetic-damage");
            return;
        }

        if (_net.IsClient)
            return;

        Speak(ent, SequenceMutation(ent, mob, args.Index, args.User)
            ? "sequenced"
            : "sequence-failed");
    }

    [SubscribeLocalEvent]
    private void OnSequenceCheck(
        Entity<GeneticsConsoleComponent> ent,
        ref DoAfterAttemptEvent<SequenceDoAfterEvent> args)
    {
        var mob = GetEntity(args.Event.Mob);
        if (!CanKeepWorkingOn(ent.Owner, mob))
            args.Cancel();
    }

    private void OnWriteMutation(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleWriteMutationMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner))
            return;

        // check delay
        var now = _timing.CurTime;
        if (now < ent.Comp.NextWrite ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            _genome.GetSequence(mob, args.Index) is not {} sequence)
            return;

        var mutation = sequence.Mutation;
        if (_mutation.GetRoundData(mutation)?.Discovered != true ||
            _disk.GetDisk(ent.Owner) is not {} disk ||
            disk.Comp.Mutation == mutation)
            return;

        ent.Comp.NextWrite = now + ent.Comp.WriteDelay;
        DirtyField(ent.AsNullable(), nameof(GeneticsConsoleComponent.NextWrite));

        _adminLog.Add(
            LogType.Genetics,
            LogImpact.Low,
            $"{mutation} from {mob:target} written to {disk.Owner:disk} by {args.Actor:user} at {ent.Owner:console}");
        _audio.PlayPvs(ent.Comp.WriteSound, ent);
        _disk.SetMutation(disk, mutation);
    }

    private void OnCombine(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsoleCombineMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner) ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            _genome.GetSequence(mob, args.Index) is not {} sequence ||
            _disk.GetDisk(ent.Owner)?.Comp.Mutation == null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.Actor,
            ent.Comp.CombineDelay,
            new CombineDoAfterEvent(GetNetEntity(mob), args.Index),
            eventTarget: ent,
            target: mob,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };
        SetBusy(ent.Owner, doAfterArgs);
        Speak(ent, "combining");
    }

    [SubscribeLocalEvent]
    private void OnCombineDoAfter(Entity<GeneticsConsoleComponent> ent, ref CombineDoAfterEvent args)
    {
        SetBusy(ent.Owner, false);
        if (_net.IsClient)
            return;

        if (args.Cancelled)
        {
            Speak(ent, "combine-failed");
            return;
        }

        args.Handled = true;
        var mob = GetEntity(args.Mob);
        if (!CanWorkOn(ent.Owner, mob))
        {
            Speak(ent, "combine-failed");
            return;
        }

        var damage = _mutation.GetGeneticDamage(mob) ?? 0;
        if (damage > ent.Comp.MaxGeneticDamage)
        {
            Speak(ent, "genetic-damage");
            return;
        }

        // should never happen, no message
        if (_disk.GetDisk(ent.Owner)?.Comp.Mutation is not {} diskMutation ||
            _genome.GetSequence(mob, args.Index) is not {} sequence ||
            _mutation.GetMutatable(mob) is not {} mutatable)
            return;

        var mutation = sequence.Mutation;
        if (_mutation.CombineMutations(mutation, diskMutation) is not {} result)
        {
            Speak(ent, "combine-none");
            return;
        }

        // already present or couldn't add it
        if (!_mutation.AddMutation(mutatable.AsNullable(), result, user: args.User))
        {
            Speak(ent, "combine-present");
            return;
        }

        _damage.ChangeDamage(mob, ent.Comp.CombineDamage);

        Speak(ent, "combined");

        _adminLog.Add(LogType.Genetics, LogImpact.Medium,
            $"{result} combined from {mutation} and {diskMutation} by {args.User:user} using console {ent.Owner}");

        // it isn't discovered so you have to figure out what it is before it's too late...
        _genome.TryAddSequence(mob, result);
        UpdateUI(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnCombineCheck(Entity<GeneticsConsoleComponent> ent, ref DoAfterAttemptEvent<CombineDoAfterEvent> args)
    {
        var mob = GetEntity(args.Event.Mob);
        if (!CanKeepWorkingOn(ent.Owner, mob) || _disk.GetDisk(ent.Owner) == null)
            args.Cancel();
    }

    private void OnPrint(Entity<GeneticsConsoleComponent> ent, ref GeneticsConsolePrintMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner))
            return;

        var now = _timing.CurTime;
        if (args.Print >= (uint) ent.Comp.Prints.Count)
            return;

        var i = (int) args.Print;
        if (now < ent.Comp.NextPrint ||
            _disk.GetDisk(ent.Owner) is not {} disk ||
            disk.Comp.Mutation is not {} mutation ||
            !ProtoMan.Resolve(mutation, out var mutationProto))
            return;

        var user = args.Actor;
        var cost = ent.Comp.Prints[i].Cost;
        if (mutationProto.TryComp<MutationCostComponent>(_costName, out var costComp))
            cost *= costComp.BiomassMultiplier;

        if (!TryUseBiomass(ent, cost))
        {
            _popup.PopupClient(Loc.GetString("genetics-console-missing-biomass"), ent, user);
            return;
        }

        ent.Comp.NextPrint = now + ent.Comp.PrintDelay;
        DirtyField(ent.AsNullable(), nameof(GeneticsConsoleComponent.NextPrint));

        var proto = ent.Comp.Prints[i].Proto;
        var item = PredictedSpawnAtPosition(proto, Transform(ent).Coordinates);
        _mutator.AddMutation(item, mutation);
        _audio.PlayPredicted(ent.Comp.PrintSound, ent, user);

        _adminLog.Add(LogType.Genetics, LogImpact.Medium,
            $"Printed {item:item} with {mutation} by {user:user} using console {ent.Owner:console}");
    }

    private void Speak(EntityUid uid, string suffix)
    {
        var msg = Loc.GetString("genetics-console-chat-" + suffix);
        var type = InGameICChatType.Speak;
        _chat.TrySendInGameICMessage(uid, msg, type, hideChat: false, hideLog: true);
    }

    #region Public API

    public static char CycleBase(char b, GeneticsCycle cycle)
        => (b, cycle) switch
        {
            (_, GeneticsCycle.Reset) => 'X',
            ('A', GeneticsCycle.Next) => 'C',
            ('C', GeneticsCycle.Next) => 'G',
            ('G', GeneticsCycle.Next) => 'T',
            ('T', GeneticsCycle.Next) => 'X',
            ('X', GeneticsCycle.Next) => 'A',
            ('A', GeneticsCycle.Last) => 'X',
            ('C', GeneticsCycle.Last) => 'A',
            ('G', GeneticsCycle.Last) => 'C',
            ('T', GeneticsCycle.Last) => 'G',
            ('X', GeneticsCycle.Last) => 'T',
            _ => b // how
        };

    public bool TryUseBiomass(Entity<GeneticsConsoleComponent> ent, int cost)
    {
        if (!_materialQuery.TryComp(ent, out var storage))
            return cost <= 0;

        return _material.TryChangeMaterialAmount(ent.Owner, ent.Comp.Biomass, -cost, storage);
    }

    /// <summary>
    /// Tries to sequences a mutation, either activating it in the mob or damaging it.
    /// </summary>
    public bool SequenceMutation(
        Entity<GeneticsConsoleComponent> ent,
        EntityUid mob,
        uint index,
        EntityUid? user = null)
    {
        if (!CanWorkOn(ent.Owner, mob) ||
            _genome.GetSequence(mob, index) is not {} sequence)
            return false;

        var mutation = sequence.Mutation;
        if (_mutation.GetRoundData(mutation) is not {} data)
            return false;

        if (data.Discovered) // no
            return false;

        if (sequence.Bases != data.Bases)
        {
            var you = Loc.GetString("genetics-console-damages-you");
            var others = Loc.GetString("genetics-console-damages-others");
            _audio.PlayPvs(ent.Comp.SequenceFailSound, ent);
            _popup.PopupEntity(you, ent, mob, PopupType.LargeCaution);
            _popup.PopupEntity(
                others,
                ent,
                Filter.Pvs(ent, entityManager: EntityManager).RemovePlayerByAttachedEntity(mob),
                true,
                PopupType.LargeCaution);
            _damage.ChangeDamage(mob, ent.Comp.SequenceFailDamage);
            if (user != null)
            {
                _adminLog.Add(
                    LogType.Genetics,
                    LogImpact.Medium,
                    $"Failed {mutation} on {mob:target} (damaged) by {user.Value:user} using {ent.Owner:console}");
            }
            else
            {
                _adminLog.Add(
                    LogType.Genetics,
                    LogImpact.Medium,
                    $"Failed sequencing {mutation} on {mob:target} with damage using {ent.Owner:console}");
            }
            return false;
        }

        var ev = new MutationSequencedEvent(mutation, data);
        RaiseLocalEvent(ent, ref ev);

        _audio.PlayPvs(ent.Comp.SequenceSound, ent);
        data.Discovered = true;
        // not predicted because of round data
        _mutation.AddMutation(mob, sequence.Mutation, user: user, predicted: false);
        if (user != null)
        {
            _adminLog.Add(
                LogType.Genetics,
                LogImpact.High,
                $"Discovered and added {mutation} to {mob:target} by {user.Value:user} using {ent.Owner:console}");
        }
        else
        {
            _adminLog.Add(
                LogType.Genetics,
                LogImpact.High,
                $"Discovered and added {mutation} to {mob:target} using {ent.Owner:console}");
        }
        UpdateUI(ent.Owner); // it's now discovered
        return true;
    }

    #endregion Public API
}

[Serializable, NetSerializable]
public sealed partial class SequenceDoAfterEvent : DoAfterEvent
{
    public NetEntity Mob;
    public uint Index;

    public SequenceDoAfterEvent(NetEntity mob, uint index)
    {
        Mob = mob;
        Index = index;
    }

    public override DoAfterEvent Clone()
        => new SequenceDoAfterEvent(Mob, Index);
}

[Serializable, NetSerializable]
public sealed partial class CombineDoAfterEvent : DoAfterEvent
{
    public NetEntity Mob;
    public uint Index;

    public CombineDoAfterEvent(NetEntity mob, uint index)
    {
        Mob = mob;
        Index = index;
    }

    public override DoAfterEvent Clone()
        => new CombineDoAfterEvent(Mob, Index);
}
