using Content.Shared._Reserve.Viewcone;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._Reserve.Hands;

public sealed partial class ReserveHandsRelaySystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, ModifyViewconeAngleEvent>(_hands.RefRelayEvent);
    }
}
