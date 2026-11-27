using Robust.Shared.GameStates;

namespace Content.Shared._Reserve.Viewcone.Components;

/// <summary>
/// Links a tail segment entity to its head entity so the head can see its own tail regardless of viewcone.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ViewconeTailedSegmentComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Head;
}
