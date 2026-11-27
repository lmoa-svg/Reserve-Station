using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Mutation that modifies bleeding rate and blood refresh rate.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BleedingMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class BleedingMutationComponent : Component
{
    [DataField, AutoNetworkedField]
    public float BleedModifier = 1.5f;

    [DataField, AutoNetworkedField]
    public float RefreshModifier = 6f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 AppliedDelta = FixedPoint2.Zero;
}
