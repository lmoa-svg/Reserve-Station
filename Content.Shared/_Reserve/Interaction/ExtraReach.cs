// based on Trauma-Station: Content.Trauma.Shared/Interaction/ExtraReach{Component,System}.cs
using Content.Shared.Body.Part;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Content.Shared._Reserve.Interaction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExtraReachComponent : Component
{
    /// <summary>
    /// How much extra reach this entity grants, in meters.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Bonus = 0f;

    /// <summary>
    /// The body entity this component's bonus has been applied to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AppliedBody;
}

public sealed partial class ExtraReachSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private bool _isChecking;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraReachComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ExtraReachComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ExtraReachComponent, BodyPartAddedEvent>(OnPartAdded);
        SubscribeLocalEvent<ExtraReachComponent, BodyPartRemovedEvent>(OnPartRemoved);
        SubscribeLocalEvent<ExtraReachComponent, EntGotRemovedFromContainerMessage>(OnPartRemovedFromContainer);
        SubscribeLocalEvent<ExtraReachComponent, InRangeOverrideEvent>(OnRangeOverride);
    }

    private void OnStartup(Entity<ExtraReachComponent> ent, ref ComponentStartup args)
    {
        if (_timing.ApplyingState)
            return;

        ApplyToBody(ent);
    }

    private void OnPartAdded(Entity<ExtraReachComponent> ent, ref BodyPartAddedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        ApplyToBody(ent);
    }

    private void OnPartRemoved(Entity<ExtraReachComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        RemoveAppliedBonus(ent);
    }

    private void OnPartRemovedFromContainer(Entity<ExtraReachComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        RemoveAppliedBonus(ent);
    }

    private void OnShutdown(Entity<ExtraReachComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        RemoveAppliedBonus(ent, dirty: false);
    }

    private void ApplyToBody(Entity<ExtraReachComponent> ent)
    {
        if (!TryComp<BodyPartComponent>(ent.Owner, out var part) || part.Body is not { } body)
            return;

        if (ent.Comp.AppliedBody == body)
            return;

        if (ent.Comp.AppliedBody is { } oldBody)
        {
            ModifyReach(oldBody, -ent.Comp.Bonus);
            ent.Comp.AppliedBody = null;
        }

        if (ent.Comp.Bonus <= 0f)
        {
            Dirty(ent);
            return;
        }

        ent.Comp.AppliedBody = body;
        Dirty(ent);
        ModifyReach(body, ent.Comp.Bonus);
    }

    private void RemoveAppliedBonus(Entity<ExtraReachComponent> ent, bool dirty = true)
    {
        if (ent.Comp.AppliedBody is not { } body)
            return;

        ModifyReach(body, -ent.Comp.Bonus);
        ent.Comp.AppliedBody = null;

        if (dirty && !TerminatingOrDeleted(ent) && ent.Comp.LifeStage < ComponentLifeStage.Stopping)
            Dirty(ent);
    }

    private void OnRangeOverride(Entity<ExtraReachComponent> ent, ref InRangeOverrideEvent args)
    {
        // Reserve's InRangeOverrideEvent(User, Target) does not carry the caller's requested range, mask, or predicate.
        // Return early unless there is a positive bonus so normal range/examine checks are never overridden with a zero bonus,
        // and respect earlier override handlers (e.g. StationAI).
        if (_isChecking || args.Handled || ent.Comp.Bonus <= 0f)
            return;

        var userXform = Transform(args.User);
        var targetXform = Transform(args.Target);

        if (userXform.MapID != targetXform.MapID)
            return;

        var maxRange = SharedInteractionSystem.InteractionRange + ent.Comp.Bonus;
        if (maxRange <= 0f)
            return;

        var distance = (_transform.GetWorldPosition(userXform) - _transform.GetWorldPosition(targetXform)).Length();
        if (distance > maxRange)
            return;

        _isChecking = true;
        try
        {
            // Only mark Handled/InRange when unobstructed within the extended interaction range; otherwise let the
            // caller fall back to its own check (e.g. larger examine range or different collision masks).
            if (_interaction.InRangeUnobstructed(
                    (args.User, userXform),
                    (args.Target, targetXform),
                    targetXform.Coordinates,
                    targetXform.LocalRotation,
                    range: maxRange))
            {
                args.Handled = true;
                args.InRange = true;
            }
        }
        finally
        {
            _isChecking = false;
        }
    }

    public void ModifyReach(EntityUid uid, float reach)
    {
        // don't care if the body is being deleted or delta is effectively zero
        if (TerminatingOrDeleted(uid) || Math.Abs(reach) < 0.001f)
            return;

        // never ensure a new component solely to subtract reach from a body that lacks one
        if (reach < 0f && !HasComp<ExtraReachComponent>(uid))
            return;

        var comp = EnsureComp<ExtraReachComponent>(uid);
        comp.Bonus += reach;

        // remove the component if it goes to 0f (or below due to a mismatch)
        if (comp.Bonus <= 0.001f)
        {
            RemComp(uid, comp);
            return;
        }

        Dirty(uid, comp);
    }
}