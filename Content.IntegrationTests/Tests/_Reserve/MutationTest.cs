using System.Collections.Generic;
using System.Linq;
using Content.Goobstation.Shared.EntityEffects;
using Content.Server.Polymorph.Systems;
using Content.Shared._Reserve.Genetics.Abilities;
using Content.Shared._Reserve.Genetics.Mutations;
using Content.Shared.Polymorph;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Reserve;

[TestFixture]
[TestOf(typeof(MutationSystem))]
public sealed class MutationTest
{
    private static readonly EntProtoId TestMob = "MobHuman";
    private static readonly EntProtoId<MutationComponent> TestMutation = "MutationDwarfism";
    private static readonly ProtoId<PolymorphPrototype> TestPolymorph = "MutationMonkey";

    /// <summary>
    /// Makes sure no errors happen when adding, updating and removing every mutation.
    /// Each mutation gets its own mob which is spawned on the same map.
    /// </summary>
    [Test]
    public async Task AddRemoveAllMutations()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var sEntMan = server.EntMan;
        var sProtoMan = server.ProtoMan;
        var mutation = sEntMan.System<MutationSystem>();

        var factory = sEntMan.ComponentFactory;
        // Polymorphing mutations replace the mob entity, so check them separately.
        var blacklisted = factory.CompName<PolymorphMutationComponent>();
        var effectsName = factory.CompName<EffectsMutationComponent>();
        var mobs = new List<EntityUid>();
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var id in mutation.AllMutations.Keys)
                {
                    if (!sProtoMan.Resolve(id, out var proto) ||
                        proto.HasComp(blacklisted) ||
                        (proto.TryComp<EffectsMutationComponent>(effectsName, out var effects) &&
                         effects.Added.Any(e => e is SpeciesChange or RandomSpeciesChange)))
                    {
                        continue;
                    }

                    var mob = sEntMan.SpawnAtPosition(TestMob, map.GridCoords);
                    Assert.That(mutation.AddMutation(mob, id), $"Failed to add {id} to {sEntMan.ToPrettyString(mob)}");
                    Assert.That(mutation.HasMutation(mob, id), $"Added {id} but it was not present in {sEntMan.ToPrettyString(mob)}");
                    mobs.Add(mob);
                }
            });
        });

        await pair.RunSeconds(10);

        await server.WaitAssertion(() =>
        {
            foreach (var mob in mobs)
            {
                mutation.ClearMutations(mob);
                sEntMan.DeleteEntity(mob);
            }
        });

        await pair.RunSeconds(2);
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Checks that mutations are correctly transferred when polymorphing into another entity.
    /// </summary>
    [Test]
    public async Task MutationsPolymorphTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var sEntMan = server.EntMan;
        var mutation = sEntMan.System<MutationSystem>();
        var polymorph = sEntMan.System<PolymorphSystem>();
        var genome = sEntMan.System<ScannedGenomeSystem>();

        await server.WaitAssertion(() =>
        {
            var dorf = sEntMan.SpawnAtPosition(TestMob, map.GridCoords);

            // scan him and compare sequences later
            genome.ScanGenome(dorf);

            // make him short
            Assert.That(mutation.AddMutation(dorf, TestMutation),
                $"Failed to give {sEntMan.ToPrettyString(dorf)} {TestMutation}!");
            Assert.That(mutation.HasMutation(dorf, TestMutation),
                $"{TestMutation} was not present in {sEntMan.ToPrettyString(dorf)}!");
            var started = GetSequenceIds(sEntMan, dorf);
            Assert.That(started.Contains(TestMutation),
                $"{TestMutation} did not get added to already scanned genomes of {sEntMan.ToPrettyString(dorf)}!");

            // return to monke
            if (polymorph.PolymorphEntity(dorf, TestPolymorph) is not {} monkey)
            {
                Assert.Fail($"Failed to polymorph {sEntMan.ToPrettyString(dorf)} into {TestPolymorph}!");
                return;
            }

            // the monkey must have taken all mutations
            Assert.That(mutation.HasMutation(monkey, TestMutation),
                $"{TestMutation} was not moved to {sEntMan.ToPrettyString(monkey)}!");
            Assert.That(!mutation.HasMutation(dorf, TestMutation),
                $"{TestMutation} was not moved from {sEntMan.ToPrettyString(dorf)}!");

            // and still have everything scanned
            var ended = GetSequenceIds(sEntMan, monkey);
            Assert.That(ended.Contains(TestMutation),
                $"{TestMutation} was not moved to scanned genomes of {sEntMan.ToPrettyString(monkey)}!");
            Assert.That(ended, Is.EquivalentTo(started),
                "Lost some scanned genome sequences when turning into a monkey!");

            // return from monke
            Assert.That(polymorph.Revert(monkey), Is.EqualTo(dorf),
                $"Failed to revert polymorph from {sEntMan.ToPrettyString(monkey)} back to {sEntMan.ToPrettyString(dorf)}!");

            // dwarf should have his mutations back
            Assert.That(mutation.HasMutation(dorf, TestMutation),
                $"{TestMutation} was not moved back to {sEntMan.ToPrettyString(dorf)}!");

            ended = GetSequenceIds(sEntMan, dorf);
            Assert.That(ended, Is.EquivalentTo(started),
                "Lost some scanned genome sequences when turning back from a monkey!");

            sEntMan.DeleteEntity(dorf);
        });

        await pair.CleanReturnAsync();
    }

    private static List<string> GetSequenceIds(IEntityManager entMan, EntityUid uid)
    {
        var comp = entMan.GetComponent<ScannedGenomeComponent>(uid);
        var ids = new List<string>(comp.Sequences.Count);
        foreach (var sequence in comp.Sequences)
        {
            ids.Add(sequence.Mutation);
        }
        return ids;
    }
}
