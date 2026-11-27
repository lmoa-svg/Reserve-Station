using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Modifies max zoom when added and automatically zooms out.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MaxZoomMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class MaxZoomMutationComponent : Component
{
    /// <summary>
    /// What to scale MaxZoom by.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Modifier = 1.25f;

    [DataField, AutoNetworkedField]
    public bool Applied;

    [DataField, AutoNetworkedField]
    public Vector2 AppliedDelta = Vector2.Zero;
}
