
using Content.Shared.Actions;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Action component for use with <see cref="TelepathyActionEvent"/>.
/// PDA messaging but with your mind...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MutationTelepathyActionComponent : Component
{
    [DataField]
    public int MaxLength = 30; // no essays

    [ViewVariables]
    public EntityUid? Target;
}

public sealed partial class TelepathyActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public enum TelepathyUiKey : byte
{
    Key
}

/// <summary>
/// Message sent by the BUI with the chosen text to send to the target.
/// </summary>
[Serializable, NetSerializable]
public sealed class TelepathyChosenMessage(string message) : BoundUserInterfaceMessage
{
    public readonly string Message = message;
}
