using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared._Reserve.Genetics.Mutations;

namespace Content.Shared._Reserve.Genetics.Abilities;

public sealed partial class BleedingMutationSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BleedingMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<BleedingMutationComponent, MutationRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<BleedingMutationComponent, BleedModifierEvent>(OnBleedModifier);
    }

    private void OnAdded(Entity<BleedingMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (ent.Comp.RefreshModifier < 0f || float.IsNaN(ent.Comp.RefreshModifier) || float.IsInfinity(ent.Comp.RefreshModifier))
            return;

        if (!_bloodstreamQuery.TryComp(args.Target, out var blood))
            return;

        var targetAmount = FixedPoint2.Max(FixedPoint2.Zero, blood.BloodRefreshAmount * ent.Comp.RefreshModifier);
        var delta = targetAmount - blood.BloodRefreshAmount;
        ent.Comp.AppliedDelta = delta;
        Dirty(ent);

        _bloodstream.SetRefreshAmount((args.Target, blood), blood.BloodRefreshAmount + delta);
    }

    private void OnRemoved(Entity<BleedingMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (!_bloodstreamQuery.TryComp(args.Target, out var blood))
            return;

        var newAmount = FixedPoint2.Max(FixedPoint2.Zero, blood.BloodRefreshAmount - ent.Comp.AppliedDelta);
        ent.Comp.AppliedDelta = FixedPoint2.Zero;
        Dirty(ent);

        _bloodstream.SetRefreshAmount((args.Target, blood), newAmount);
    }

    private void OnBleedModifier(Entity<BleedingMutationComponent> ent, ref BleedModifierEvent args)
    {
        args.BleedAmount *= MathF.Max(0f, ent.Comp.BleedModifier);
    }
}
