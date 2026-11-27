using Content.Server.Radio.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared._Reserve.Genetics.Console;
using Content.Shared._Reserve.Genetics.Mutations;
using Content.Shared.Research.Components;

namespace Content.Server._Reserve.Genetics.Console;

public sealed partial class GeneticsResearchConsoleSystem : EntitySystem
{
    [Dependency] private MutationSystem _mutation = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private EntityQuery<ResearchClientComponent> _clientQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GeneticsResearchConsoleComponent, MutationSequencedEvent>(OnSequenced);
    }

    private void OnSequenced(Entity<GeneticsResearchConsoleComponent> ent, ref MutationSequencedEvent args)
    {
        if (args.Data.Discovered || // no infinite point farming chud
            _clientQuery.CompOrNull(ent)?.Server is not {} server ||
            !_mutation.AllMutations.TryGetValue(args.Mutation, out var comp))
            return;

        var difficulty = comp.Difficulty;
        var points = difficulty * ent.Comp.PointsPerDifficulty;
        _research.ModifyServerPoints(server, points);
        var name = ProtoMan.Index(args.Mutation).Name;
        var msg = Loc.GetString("genetics-console-radio-message", ("points", points), ("mutation", name));
        _radio.SendRadioMessage(ent, msg, ent.Comp.Channel, ent);
    }
}
