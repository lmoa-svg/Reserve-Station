using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Content.Shared._Reserve.Genetics.Mutations;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Adds a random negative mutation to the target entity.
/// Does nothing for mutations which are already present or conflict with existing ones.
/// </summary>
public sealed partial class RandomNegativeMutation : EntityEffectBase<RandomNegativeMutation>
{
    /// <summary>
    /// Multiplier on difficulty for sequencing the mutation.
    /// </summary>
    [DataField]
    public float Difficulty = 1f;
}

public sealed partial class RandomNegativeMutationEffectSystem :
    EntityEffectSystem<MutatableComponent, RandomNegativeMutation>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    protected override void Effect(Entity<MutatableComponent> ent, ref EntityEffectEvent<RandomNegativeMutation> args)
    {
        if (_net.IsClient)
            return;
        if (_mutation.NegativeMutations.Count == 0)
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var mutation = rand.Pick(_mutation.NegativeMutations);
        _mutation.AddMutation(ent.AsNullable(), mutation, user: args.User,
            automatic: false, predicted: false, difficulty: args.Effect.Difficulty);
    }
}
