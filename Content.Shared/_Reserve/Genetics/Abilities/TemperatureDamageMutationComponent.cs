using Robust.Shared.GameStates;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Adds an offset to cold and/or heat damage thresholds.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TemperatureDamageMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class TemperatureDamageMutationComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ColdOffset;

    [DataField, AutoNetworkedField]
    public float HeatOffset;

    [DataField, AutoNetworkedField]
    public float AppliedColdOffset;

    [DataField, AutoNetworkedField]
    public float AppliedHeatOffset;
}
