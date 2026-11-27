using Content.Shared._Reserve.Viewcone;
using Content.Shared.Inventory;

namespace Content.Shared._Reserve.Inventory;

public sealed partial class ReserveInventorySystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, ModifyViewconeAngleEvent>(_inventory.RelayEvent);
        SubscribeLocalEvent<InventoryComponent, CanSpawnFootstepsEvent>(_inventory.RelayEvent);
    }
}
