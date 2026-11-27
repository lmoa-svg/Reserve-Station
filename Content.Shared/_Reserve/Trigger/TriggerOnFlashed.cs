// based on Trauma-Station: Content.Trauma.Shared/Trigger/Triggers/TriggerOnFlashed{Component,System}.cs

using Content.Shared.Flash;
using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.Trigger;

/// <summary>
/// Triggers when this entity is directly flashed or area flashed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(TriggerOnFlashedSystem))]
[AutoGenerateComponentState]
public sealed partial class TriggerOnFlashedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Probability of being triggered when flashed.
    /// </summary>
    [DataField]
    public float Prob = 1f;
}

public sealed partial class TriggerOnFlashedSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnFlashedComponent, AfterFlashedEvent>(OnFlashed);
    }

    private void OnFlashed(Entity<TriggerOnFlashedComponent> ent, ref AfterFlashedEvent args)
    {
        if (SharedRandomExtensions.PredictedProb(_timing, ent.Comp.Prob, GetNetEntity(ent)))
            _trigger.Trigger(ent, args.User, ent.Comp.KeyOut);
    }
}