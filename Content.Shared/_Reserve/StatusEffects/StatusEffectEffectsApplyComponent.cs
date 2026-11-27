// based on Trauma-Station: Content.Trauma.Shared/StatusEffects/StatusEffectEffectsApplyComponent.cs

using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._Reserve.StatusEffects;

/// <summary>
/// Status effect that applies entity effects on apply and on removal.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StatusEffectEffectsApplyComponent : Component
{
    /// <summary>
    /// The effects to apply when the status effect is applied.
    /// </summary>
    [DataField]
    public EntityEffect[]? EffectsOnApply;

    /// <summary>
    /// The effects to apply when the status effect is removed.
    /// </summary>
    [DataField]
    public EntityEffect[]? EffectsOnRemoval;
}