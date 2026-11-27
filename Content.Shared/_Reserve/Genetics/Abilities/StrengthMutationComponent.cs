using Robust.Shared.GameStates;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Component for the strength mutation.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StrengthMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class StrengthMutationComponent : Component
{
    /// <summary>
    /// Modifier for melee damage dealt.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MeleeModifier = 1.2f;
}
