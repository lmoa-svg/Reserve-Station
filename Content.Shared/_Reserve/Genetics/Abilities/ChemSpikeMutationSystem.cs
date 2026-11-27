using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._Reserve.Genetics.Mutations;
using Robust.Shared.Network;

namespace Content.Shared._Reserve.Genetics.Abilities;

/// <summary>
/// Handles most things chemspike related.
/// </summary>
public sealed partial class ChemSpikeMutationSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBloodstreamSystem _blood = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemSpikeMutationComponent, ComponentShutdown>(OnShutdown);

        // Action events are raised on the action's container, which is the mutation, or on the user.
        // They are never raised on the action entity, so the mutation is what has to handle them.
        SubscribeLocalEvent<ChemSpikeMutationComponent, ChemSpikeActionEvent>(OnFire);
        SubscribeLocalEvent<ChemSpikeMutationComponent, ChemTransferActionEvent>(OnTransfer);

        SubscribeLocalEvent<ChemTransferProjectileComponent, EmbedEvent>(OnProjectileEmbed);
        SubscribeLocalEvent<ChemTransferProjectileComponent, EmbedDetachEvent>(OnProjectileDetach);
        SubscribeLocalEvent<ChemTransferProjectileComponent, ComponentShutdown>(OnProjectileShutdown);
    }

    private void OnShutdown(Entity<ChemSpikeMutationComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.ActionEntity);
    }

    /// <summary>
    /// Fires a spike at the target, which links the mutation up with them once it embeds.
    /// </summary>
    private void OnFire(Entity<ChemSpikeMutationComponent> ent, ref ChemSpikeActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        // Only the server spawns the spike, otherwise there would be one per prediction.
        if (_net.IsClient)
            return;

        if (!_blocker.CanInteract(args.Performer, args.Performer))
            return;

        var userXform = Transform(args.Performer);
        var targetXform = Transform(args.Target);
        if (userXform.MapID != targetXform.MapID || !_transform.InRange(args.Performer, args.Target, ent.Comp.Range))
            return;

        var from = _transform.GetMapCoordinates(args.Performer, userXform);
        var direction = _transform.GetMapCoordinates(args.Target, targetXform).Position - from.Position;
        if (direction.LengthSquared() < 0.01f)
            return;

        var spike = Spawn(ent.Comp.ProjectileProto, from);
        if (!TryComp<ChemTransferProjectileComponent>(spike, out var transfer))
        {
            Log.Error($"Chemspike projectile {ent.Comp.ProjectileProto} has no {nameof(ChemTransferProjectileComponent)}!");
            QueueDel(spike);
            return;
        }

        // Remember which mutation fired it so embedding it can find its way back here.
        transfer.Mutation = ent.Owner;
        Dirty(spike, transfer);

        _gun.ShootProjectile(spike, direction, Vector2.Zero, args.Performer, args.Performer, ent.Comp.ProjectileSpeed);
    }

    private void OnTransfer(Entity<ChemSpikeMutationComponent> ent, ref ChemTransferActionEvent args)
    {
        // Client shouldn't predict this since the target might have left PVS range.
        if (_net.IsClient ||
            args.Handled ||
            args.Action.Comp.Container != ent.Owner || // the action has to be this mutation's
            !HasComp<ChemTransferActionComponent>(args.Action.Owner) ||
            ent.Comp.Target is not { } target)
            return;

        if (!_blocker.CanInteract(args.Performer, args.Performer))
            return;

        var xformUser = Transform(args.Performer);
        var xformTarget = Transform(target);
        if (xformUser.MapID != xformTarget.MapID || !_transform.InRange(args.Performer, target, ent.Comp.Range))
            return;

        if (_blood.FlushChemicals(args.Performer, ent.Comp.MaxQuantity) is { } removed)
            _blood.TryAddToBloodstream(target, removed);

        args.Handled = true;

        // Don't let it be used again, even if it didn't succeed.
        if (ent.Comp.Projectile is { } projectile)
            _projectile.EmbedDetach(projectile, null);
        SetMutationTarget(ent.Owner, null, null, ent.Comp);
    }

    private void OnProjectileEmbed(Entity<ChemTransferProjectileComponent> ent, ref EmbedEvent args)
    {
        if (HasComp<BloodstreamComponent>(args.Embedded))
            SetProjectileTarget(ent.Owner, args.Embedded);
    }

    private void OnProjectileDetach(Entity<ChemTransferProjectileComponent> ent, ref EmbedDetachEvent args)
    {
        SetProjectileTarget(ent.Owner, null);
    }

    private void OnProjectileShutdown(Entity<ChemTransferProjectileComponent> ent, ref ComponentShutdown args)
    {
        SetProjectileTarget(ent.Owner, null);
    }

    /// <summary>
    /// Links the spike back to the mutation that fired it, so it can transfer chemicals to the mob it embedded in.
    /// </summary>
    private void SetProjectileTarget(EntityUid uid, EntityUid? target)
    {
        if (_net.IsClient)
            return; // links are server authoritative, clients get them through the mutation component

        if (!TryComp<ChemTransferProjectileComponent>(uid, out var projectile) ||
            projectile.Mutation is not { } mutation ||
            !TryComp<ChemSpikeMutationComponent>(mutation, out var comp))
            return;

        // Detaching an old spike must not clear a link made by a newer one.
        if (target == null && comp.Projectile != uid)
            return;

        SetMutationTarget(mutation, target, target == null ? null : uid, comp);
    }

    private void SetMutationTarget(EntityUid uid, EntityUid? target, EntityUid? proj = null, ChemSpikeMutationComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) ||
            (comp.Target == target && comp.Projectile == proj) ||
            _mutation.GetMutationTarget(uid) is not { } user)
            return;

        comp.Target = target;
        comp.Projectile = proj;
        Dirty(uid, comp);

        if (target != null)
            _actions.AddAction(user, ref comp.ActionEntity, comp.Action, container: uid);
        else
            _actions.RemoveAction(comp.ActionEntity);

        var key = target != null ? "set" : "reset";
        var msg = Loc.GetString("MutationChemSpike-target-" + key);
        _popup.PopupEntity(msg, user, user);
    }
}
