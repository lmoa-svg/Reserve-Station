
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Mutations;

/// <summary>
/// Mutation component to change how much biomass it takes to work with.
/// </summary>
[RegisterComponent, Access(typeof(MutationSystem))]
[EntityCategory("Mutations")]
public sealed partial class MutationCostComponent : Component
{
    /// <summary>
    /// Multiplier on cost to print mutators, activators or cleansers.
    /// </summary>
    [DataField(required: true)]
    public int BiomassMultiplier;
}
