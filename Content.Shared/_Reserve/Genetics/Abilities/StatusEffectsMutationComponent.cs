using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Adds permanent status effects while this mutation is active.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StatusEffectsMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class StatusEffectsMutationComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> StatusEffects = default!;

    /// <summary>
    /// Status effects that were added by this mutation and were not already present.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> AppliedStatusEffects = new();
}
