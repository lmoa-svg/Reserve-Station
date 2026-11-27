
using Content.Shared.EntityEffects;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Runs entity effects when this mutation is added or removed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EffectsMutationSystem))]
public sealed partial class EffectsMutationComponent : Component
{
    /// <summary>
    /// The effects ran on the target when this mutation is added.
    /// </summary>
    [DataField]
    public EntityEffect[] Added = [];

    /// <summary>
    /// The effects ran on the target when this mutation is removed.
    /// </summary>
    [DataField]
    public EntityEffect[] Removed = [];

    /// <summary>
    /// If true, doesn't run effects for automatic mutation adding/removing (polymorph).
    /// </summary>
    [DataField]
    public bool IgnoreAutomatic;
}
