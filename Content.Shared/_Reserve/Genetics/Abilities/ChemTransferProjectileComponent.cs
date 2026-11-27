
using Robust.Shared.GameStates;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Component for a projectile that adds the chemspike transfer action while embedded in a mob.
/// Stores the mutation that fired it so embedding can link the spike back to that mutation.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ChemSpikeMutationSystem))]
public sealed partial class ChemTransferProjectileComponent : Component
{
    /// <summary>
    /// The chemspike mutation entity that fired this projectile.
    /// </summary>
    [DataField]
    public EntityUid? Mutation;
}
