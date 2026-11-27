
using Content.Shared.Actions.Components;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Mutations;

/// <summary>
/// Grants the mutation user an action.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ActionMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class ActionMutationComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<ActionComponent> Action;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
