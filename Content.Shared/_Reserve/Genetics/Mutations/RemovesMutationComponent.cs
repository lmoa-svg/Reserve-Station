

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Mutations;

/// <summary>
/// Mutation component that makes it remove some mutations when this is mutated.
/// Does nothing when this gets removed, or if the mutations weren't present.
/// </summary>
[RegisterComponent, Access(typeof(RemovesMutationSystem))]
public sealed partial class RemovesMutationComponent : Component
{
    /// <summary>
    /// The mutations to remove.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<MutationComponent>> Removes = new();
}
