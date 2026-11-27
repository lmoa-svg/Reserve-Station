using Content.Shared.Inventory;

namespace Content.Shared._Reserve.Viewcone.Components;

/// <summary>
/// Worn clothing with this component suppresses footstep viewcone effects while equipped.
/// </summary>
[RegisterComponent]
public sealed partial class ViewconeSilentFootstepsComponent : Component;

/// <summary>
/// Cancels the viewcone footstep effect when the wearer has silent footstep clothing equipped.
/// </summary>
public sealed class ViewconeSilentFootstepsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ViewconeSilentFootstepsComponent, InventoryRelayedEvent<CanSpawnFootstepsEvent>>(OnAttempt);
    }

    private void OnAttempt(Entity<ViewconeSilentFootstepsComponent> ent, ref InventoryRelayedEvent<CanSpawnFootstepsEvent> args)
    {
        args.Args.Cancelled = true;
    }
}
