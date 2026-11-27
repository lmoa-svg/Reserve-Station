
using System.Text;
using Content.Shared._Reserve.Genetics.Mutations;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Console;

public sealed partial class GeneticsConsoleSystem
{
    [Dependency] private LabelSystem _label = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private void InitializePrintout()
    {
        Subs.BuiEvents<GeneticsPrintoutComponent>(GeneticsConsoleUiKey.Key, subs =>
        {
            subs.Event<GeneticsPrintScanMessage>(OnPrintScan);
        });
    }

    private void OnPrintScan(Entity<GeneticsPrintoutComponent> ent, ref GeneticsPrintScanMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner) ||
            GetWorkableMob(ent.Owner) is not {} mob ||
            !_genome.IsScanned(mob) ||
            !ResetPrintout(ent))
            return;

        var paper = PredictedSpawnAtPosition(ent.Comp.Paper, Transform(ent).Coordinates);

        var label = string.Empty;
        var text = string.Empty;
        if (args.Index is { } index &&
            _genome.GetSequence(mob, index) is { } sequence &&
            _mutation.GetRoundData(sequence.Mutation) is { } data)
        {
            label = data.Number.ToString();
            text = GetSequenceText(sequence, data);
        }
        else
        {
            label = Identity.Name(mob, EntityManager);
            text = GetScanText(mob, label);
        }
        _paper.SetContent(paper, text);
        _label.Label(paper, label);

        _hands.TryPickupAnyHand(args.Actor, paper);

        _adminLog.Add(
            LogType.Genetics,
            LogImpact.Low,
            $"{args.Actor:user} printed scan of {mob:target} to {paper:paper} using console {ent.Owner:console}");
    }

    private bool ResetPrintout(Entity<GeneticsPrintoutComponent> ent)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextPrint)
            return false;

        ent.Comp.NextPrint = now + ent.Comp.PrintDelay;
        Dirty(ent);
        return true;
    }

    private string GetScanText(EntityUid mob, string name)
    {
        var sequences = new List<SequenceState>();
        _genome.AddSequenceStates(mob, sequences);
        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("genetics-printout-title"));
        builder.AppendLine(Loc.GetString("genetics-printout-subject", ("name", name)));
        builder.AppendLine(Loc.GetString("genetics-printout-sequences", ("count", sequences.Count)));

        foreach (var s in sequences)
        {
            var rarity = s.Rarity.RarityChar();
            builder.AppendLine(Loc.GetString("genetics-printout-sequence", ("rarity", rarity), ("number", s.Number)));
        }

        return builder.ToString();
    }

    private string GetSequenceText(Sequence sequence, MutationData data)
    {
        if (sequence.Bases.Length < MutationData.BaseCount)
            return sequence.Bases;

        var rarity = _mutation.GetRarity(sequence.Mutation);
        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("genetics-printout-title"));
        builder.AppendLine(Loc.GetString("genetics-printout-sequence-title", ("number", data.Number)));
        builder.AppendLine(Loc.GetString("genetics-printout-sequence-rarity", ("rarity", rarity)));
        // format it similar to the UI, 2 rows split on 4 bases per group
        var n = MutationData.PairCount;
        for (int o = 0; o <= n; o += n)
        {
            builder.Append("| ");
            for (int i = 0; i < n; i += 4)
            {
                var first = o + i;
                var last = first + 4;
                builder.Append(sequence.Bases[first..last]);
                builder.Append(' ');
            }
            builder.AppendLine("|");
        }
        return builder.ToString();
    }
}
