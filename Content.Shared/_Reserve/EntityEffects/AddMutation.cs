
using Content.Shared.EntityEffects;
using Content.Shared._Reserve.Genetics.Mutations;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.EntityEffects;

/// <summary>
/// Adds a mutation to the target entity.
/// </summary>
public sealed partial class AddMutation : EntityEffectBase<AddMutation>
{
    /// <summary>
    /// The mutation to add.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<MutationComponent> Mutation;

    /// <summary>
    /// Hide popups for the target if true.
    /// </summary>
    [DataField]
    public bool Automatic;

    /// <summary>
    /// Multiplier on difficulty for sequencing the mutation.
    /// </summary>
    [DataField]
    public float Difficulty = 1f;
}

public sealed partial class AddMutationEffectSystem : EntityEffectSystem<MutatableComponent, AddMutation>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private MutationSystem _mutation = default!;

    protected override void Effect(Entity<MutatableComponent> ent, ref EntityEffectEvent<AddMutation> args)
    {
        if (_net.IsClient)
            return;

        var e = args.Effect;
        _mutation.AddMutation(ent.AsNullable(), e.Mutation, user: args.User,
            automatic: e.Automatic, predicted: false, difficulty: e.Difficulty);
    }
}
