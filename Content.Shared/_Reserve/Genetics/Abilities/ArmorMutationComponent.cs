
using Content.Shared.Damage;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Modifies incoming damage for the mutated mob.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ArmorMutationSystem))]
public sealed partial class ArmorMutationComponent : Component
{
    [DataField(required: true)]
    public DamageModifierSet Modifiers = new();
}
