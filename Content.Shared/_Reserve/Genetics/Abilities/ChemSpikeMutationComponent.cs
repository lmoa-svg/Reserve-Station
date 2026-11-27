using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Mutation component for chemspike.
/// Stores the transfer chemicals action.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ChemSpikeMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class ChemSpikeMutationComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> Action;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// The mob to transfer chemicals to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// The chemspike entity that was shot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Projectile;

    /// <summary>
    /// Limit on how many chemicals to flush from the user's bloodstream.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxQuantity = 100;

    /// <summary>
    /// Maximum range to transfer chemicals.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 15f;

    /// <summary>
    /// The spike projectile fired at a target to link up with it.
    /// It needs <see cref="ChemTransferProjectileComponent"/> to actually create the link.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId ProjectileProto;

    /// <summary>
    /// How fast the spike is fired, in tiles per second.
    /// </summary>
    [DataField]
    public float ProjectileSpeed = 20f;
}

/// <summary>
/// Raised when the mutation's action is used on a target, firing a chemspike at it.
/// </summary>
public sealed partial class ChemSpikeActionEvent : EntityTargetActionEvent;
