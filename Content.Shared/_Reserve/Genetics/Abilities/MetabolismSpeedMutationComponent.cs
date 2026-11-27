using Robust.Shared.GameStates;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Mutation component that increases mob's flat metabolism rate when active.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MetabolismSpeedMutationComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float Bonus;

    [DataField]
    public Dictionary<EntityUid, float> AppliedModifiers = new();
}
