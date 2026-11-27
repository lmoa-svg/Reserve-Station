using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Gives the mutated mob intrinsic radio channels.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RadioMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class RadioMutationComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<ProtoId<RadioChannelPrototype>> Channels = new();

    /// <summary>
    /// Active radio channels that were added and not already present.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<RadioChannelPrototype>> AddedActiveChannels = new();

    /// <summary>
    /// Transmitter channels that were added and not already present.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<RadioChannelPrototype>> AddedTransmitterChannels = new();

    [DataField, AutoNetworkedField]
    public bool AddedReceiver;

    [DataField, AutoNetworkedField]
    public bool AddedActive;

    [DataField, AutoNetworkedField]
    public bool AddedTransmitter;
}
