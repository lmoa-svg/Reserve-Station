
using Content.Shared.Tag;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Mutations;

/// <summary>
/// Adds or removes tags to the target mob.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TagMutationComponent : Component
{
    [DataField]
    public List<ProtoId<TagPrototype>> Added = new();

    [DataField]
    public List<ProtoId<TagPrototype>> Removed = new();

    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> ActuallyAdded = new();

    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> ActuallyRemoved = new();
}
