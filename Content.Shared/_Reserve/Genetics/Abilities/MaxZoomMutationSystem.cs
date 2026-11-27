using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared._Reserve.Genetics.Mutations;

namespace Content.Shared._Reserve.Genetics.Abilities;

public sealed partial class MaxZoomMutationSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly EntityQuery<ContentEyeComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaxZoomMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<MaxZoomMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<MaxZoomMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (ent.Comp.Modifier <= 0f || float.IsNaN(ent.Comp.Modifier) || float.IsInfinity(ent.Comp.Modifier))
            return;

        if (!_query.TryComp(args.Target, out var eye))
            return;

        var oldZoom = eye.MaxZoom;
        var newZoom = oldZoom * ent.Comp.Modifier;
        var delta = newZoom - oldZoom;

        ent.Comp.AppliedDelta = delta;
        ent.Comp.Applied = true;
        Dirty(ent);

        _eye.SetMaxZoom(args.Target, newZoom, eye);
    }

    private void OnRemoved(Entity<MaxZoomMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (!ent.Comp.Applied)
            return;

        if (_query.TryComp(args.Target, out var eye))
        {
            var newZoom = Vector2.Max(Vector2.Zero, eye.MaxZoom - ent.Comp.AppliedDelta);
            _eye.SetMaxZoom(args.Target, newZoom, eye);
        }

        ent.Comp.Applied = false;
        ent.Comp.AppliedDelta = Vector2.Zero;
        Dirty(ent);
    }
}
