
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Reserve.Genetics.Mutations;

public sealed partial class RemovesMutationSystem : EntitySystem
{
    [Dependency] private MutationSystem _mutation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemovesMutationComponent, MutationAddedEvent>(OnAdded);
    }

    private void OnAdded(Entity<RemovesMutationComponent> ent, ref MutationAddedEvent args)
    {
        foreach (var id in ent.Comp.Removes)
        {
            _mutation.RemoveMutation(args.Target.AsNullable(), id, user: args.User, automatic: args.Automatic, predicted: args.Predicted);
        }
    }
}
